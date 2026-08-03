using System.Text.Json.Serialization;
using MesaSitec.Api.Autenticacion;
using MesaSitec.Api.Errores;
using MesaSitec.Aplicacion.Abstracciones;
using MesaSitec.Aplicacion.Servicios;
using MesaSitec.Infraestructura.Persistencia;
using MesaSitec.Infraestructura.Persistencia.Seed;
using MesaSitec.Infraestructura.Servicios;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Puerto fijo 5080. Solo en docker se usa ASPNETCORE_URLS, si no fuerza localhost
if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
    builder.WebHost.UseUrls("http://localhost:5080");

var config = builder.Configuration;
var secretoJwt = Environment.GetEnvironmentVariable("JWT_SECRET")
                 ?? config["Jwt:Secret"]
                 ?? "SITECPRO"; // valor de desarrollo por defecto

builder.Services
    .AddControllers()
    .AddJsonOptions(opciones =>
    {
        // Enums como texto
        opciones.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Traduce los errores de validacion de modelo
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(opciones =>
{
    opciones.InvalidModelStateResponseFactory = RespuestaModelState.Construir;
});

// Base de datos SQLite
var cadenaConexion = config.GetConnectionString("Default") ?? "Data Source=mesasitec.db";
builder.Services.AddDbContext<MesaSitecDbContext>(opciones => opciones.UseSqlite(cadenaConexion));
builder.Services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<MesaSitecDbContext>());

// Inyeccion de dependencias de las capas
builder.Services.Configure<OpcionesJwt>(config.GetSection(OpcionesJwt.Seccion));
// Se inyecta el secreto resuelto
builder.Services.PostConfigure<OpcionesJwt>(o => o.Secret = secretoJwt);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IContextoUsuario, ContextoUsuarioHttp>();
builder.Services.AddScoped<IGeneradorToken, GeneradorToken>();
builder.Services.AddScoped<IHasherContrasenia, HasherContrasenaBCrypt>();
builder.Services.AddSingleton<IProveedorFecha, ProveedorFechaSistema>();

builder.Services.AddScoped<ServicioAuth>();
builder.Services.AddScoped<ServicioCategorias>();
builder.Services.AddScoped<ServicioSolicitudes>();
builder.Services.AddScoped<ServicioUsuarios>();

// Manejo global de excepciones
builder.Services.AddExceptionHandler<ManejadorGlobalExcepciones>();
builder.Services.AddProblemDetails();
//Autenticacion JWT
var opcionesJwt = new OpcionesJwt { Secret = secretoJwt };
config.GetSection(OpcionesJwt.Seccion).Bind(opcionesJwt);
opcionesJwt.Secret = secretoJwt;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opciones =>
    {
        // No renombrar los claims entrantes: asi "sub", "rol", etc. se leen tal cual.
        opciones.MapInboundClaims = false;
        opciones.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = opcionesJwt.Issuer,
            ValidateAudience = true,
            ValidAudience = opcionesJwt.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = opcionesJwt.ObtenerClave(),
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        //Cuando falta el token o es invalido/expirado, respondemos 401 con formato propio
        opciones.Events = new JwtBearerEvents
        {
            OnChallenge = async contexto =>
            {
                contexto.HandleResponse(); // evitamos la respuesta por defecto
                await EscritorProblema.EscribirAsync(
                    contexto.HttpContext, 401, "NO_AUTENTICADO", "No autenticado",
                    "El token esta ausente, es invalido o ha expirado.");
            }
        };
    });

builder.Services.AddAuthorization();

//CORS para el frontend en el puerto 5173
const string PoliticaCors = "FrontendLocal";
builder.Services.AddCors(opciones =>
{
    opciones.AddPolicy(PoliticaCors, p => p
        .WithOrigins("http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

// Swagger con esquema de seguridad
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opciones =>
{
    opciones.SwaggerDoc("v1", new OpenApiInfo { Title = "MesaSitec API", Version = "v1" });

    var esquema = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Introduzca el token JWT (sin el prefijo 'Bearer ')."
    };
    opciones.AddSecurityDefinition("Bearer", esquema);
    opciones.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Migracion automatica
await InicializarBaseDeDatosAsync(app);

app.UseExceptionHandler();

// Swagger accesible siempre
app.UseSwagger();
app.UseSwaggerUI(o => o.SwaggerEndpoint("/swagger/v1/swagger.json", "MesaSitec API v1"));

app.UseCors(PoliticaCors);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();


// aplica migraciones y siembra datos desde seed si esta vacio
static async Task InicializarBaseDeDatosAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var sp = scope.ServiceProvider;
    var db = sp.GetRequiredService<MesaSitecDbContext>();

    // Aplica las migraciones pendientes
    await db.Database.MigrateAsync();

    var textoFechaBase = Environment.GetEnvironmentVariable("SEED_FECHA_BASE")
    ?? app.Configuration["SEED_FECHA_BASE"]
    ?? "2026-01-15T08:00:00Z";
    var fechaBase = DateTime.Parse(
        textoFechaBase,
        System.Globalization.CultureInfo.InvariantCulture,
        System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal);

    var hasher = sp.GetRequiredService<IHasherContrasenia>();
    await SeedData.InicializarAsync(db, hasher, fechaBase);
}
public partial class Program { }