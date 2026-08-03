using MesaSitec.Aplicacion.Abstracciones;
using MesaSitec.Dominio.Entidades;
using MesaSitec.Dominio.Enums;
using MesaSitec.Dominio.Servicios;
using Microsoft.EntityFrameworkCore;

namespace MesaSitec.Infraestructura.Persistencia.Seed;

//Funciona solo si la base esta vacia
public static class SeedData
{
    public static async Task InicializarAsync(MesaSitecDbContext db, IHasherContrasenia hasher, DateTime fechaBase)
    {
        // Si ya hay tenants, asumimos que la base ya fue sembrada y no hacemos nada.
        if (await db.Tenants.AnyAsync())
            return;

        const string ContrasenaComun = "Sitec.2026";
        var hash = hasher.Hashear(ContrasenaComun); //No se guardan como texto plano

        var norte = new Tenant { Id = Guid.NewGuid(), Nombre = "Cooperativa Norte", Activo = true };
        var sur = new Tenant { Id = Guid.NewGuid(), Nombre = "Bufete Sur", Activo = true };
        db.Tenants.AddRange(norte, sur);

        // Usuarios
        Usuario NuevoUsuario(Guid tenantId, string email, string nombre, Rol rol) => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = email,
            Nombre = nombre,
            Rol = rol,
            PasswordHash = hash,
            Activo = true
        };

        //Cooperativa Norte
        var adminNorte = NuevoUsuario(norte.Id, "admin@norte.test", "Admin Norte", Rol.Admin);
        var agente1 = NuevoUsuario(norte.Id, "agente1@norte.test", "Agente Uno", Rol.Agente);
        var agente2 = NuevoUsuario(norte.Id, "agente2@norte.test", "Agente Dos", Rol.Agente);
        var user1Norte = NuevoUsuario(norte.Id, "user1@norte.test", "Usuario Uno Norte", Rol.Solicitante);
        var user2Norte = NuevoUsuario(norte.Id, "user2@norte.test", "Usuario Dos Norte", Rol.Solicitante);

        //Bufete Sur
        var adminSur = NuevoUsuario(sur.Id, "admin@sur.test", "Admin Sur", Rol.Admin);
        var user1Sur = NuevoUsuario(sur.Id, "user1@sur.test", "Usuario Uno Sur", Rol.Solicitante);

        db.Usuarios.AddRange(adminNorte, agente1, agente2, user1Norte, user2Norte, adminSur, user1Sur);

    //Categorias de tenants
        Dictionary<string, Categoria> CrearCategorias(Guid tenantId)
        {
            var defs = new (string nombre, int sla)[]
            {
                ("Incidente", 8),
                ("Requerimiento", 40),
                ("Consulta", 24),
                ("Falla critica", 4),
            };
            var dict = new Dictionary<string, Categoria>();
            foreach (var (nombre, sla) in defs)
            {
                var cat = new Categoria { Id = Guid.NewGuid(), TenantId = tenantId, Nombre = nombre, SlaHoras = sla, Activo = true };
                dict[nombre] = cat;
                db.Categorias.Add(cat);
            }
            return dict;
        }

        var catNorte = CrearCategorias(norte.Id);
        var catSur = CrearCategorias(sur.Id);

        //correlativos
        var correlativoNorte = 0;
        var correlativoSur = 0;

        Solicitud NuevaSolicitud(
            Tenant tenant, ref int correlativo, string titulo, string descripcion,
            Categoria categoria, Prioridad prioridad, EstadoSolicitud estado,
            Usuario solicitante, Usuario? agente, double offsetCreacionHoras,
            string? motivoResolucion = null, double? offsetResolucionHoras = null,
            string? motivoCancelacion = null)
        {
            correlativo++;
            var fechaCreacion = fechaBase.AddHours(offsetCreacionHoras);
            var codigo = $"SOL-{fechaCreacion.Year}-{correlativo:D5}";
            var sla = CalculadoraSLA.CalcularFechaLimite(fechaCreacion, categoria.SlaHoras, prioridad);

            return new Solicitud
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Codigo = codigo,
                Titulo = titulo,
                Descripcion = descripcion,
                CategoriaId = categoria.Id,
                Prioridad = prioridad,
                Estado = estado,
                SolicitanteId = solicitante.Id,
                AgenteId = agente?.Id,
                FechaCreacion = fechaCreacion,
                FechaLimiteSla = sla,
                MotivoResolucion = motivoResolucion,
                FechaResolucion = offsetResolucionHoras.HasValue ? fechaBase.AddHours(offsetResolucionHoras.Value) : null,
                MotivoCancelacion = motivoCancelacion
            };
        }

        var solicitudes = new List<Solicitud>();

        solicitudes.Add(NuevaSolicitud(norte, ref correlativoNorte, "No puedo acceder al portal",
            "Al ingresar mis credenciales el sistema me devuelve a la pantalla de login una y otra vez.",
            catNorte["Incidente"], Prioridad.Alta, EstadoSolicitud.Nueva, user1Norte, null, -240));
        solicitudes.Add(NuevaSolicitud(norte, ref correlativoNorte, "Error 500 al generar reporte mensual",
            "El modulo de reportes lanza un error interno cuando selecciono el periodo del mes anterior.",
            catNorte["Falla critica"], Prioridad.Critica, EstadoSolicitud.Asignada, user2Norte, agente1, -120));
        solicitudes.Add(NuevaSolicitud(norte, ref correlativoNorte, "La impresora de facturacion no responde",
            "Desde ayer la impresora del area de facturacion aparece fuera de linea y no imprime.",
            catNorte["Incidente"], Prioridad.Media, EstadoSolicitud.EnProceso, user1Norte, agente2, -100));
        solicitudes.Add(NuevaSolicitud(norte, ref correlativoNorte, "Solicito acceso al modulo de inventario",
            "Necesito permisos de lectura sobre el modulo de inventario para revisar existencias.",
            catNorte["Requerimiento"], Prioridad.Alta, EstadoSolicitud.Asignada, user2Norte, agente1, -200));
        solicitudes.Add(NuevaSolicitud(norte, ref correlativoNorte, "Lentitud general del sistema",
            "El sistema tarda mas de un minuto en cargar cualquier pantalla desde esta manana.",
            catNorte["Falla critica"], Prioridad.Critica, EstadoSolicitud.EnProceso, user1Norte, agente2, -80));

        solicitudes.Add(NuevaSolicitud(norte, ref correlativoNorte, "Correo corporativo rechaza adjuntos grandes",
            "Al enviar adjuntos de mas de 10 MB el correo devuelve un mensaje de rechazo.",
            catNorte["Incidente"], Prioridad.Media, EstadoSolicitud.Resuelta, user1Norte, agente1, -60,
            motivoResolucion: "Se amplio el limite de adjuntos a 25 MB y se valido con el usuario.", offsetResolucionHoras: -52));
        solicitudes.Add(NuevaSolicitud(norte, ref correlativoNorte, "Restablecer contrasena de usuario bloqueado",
            "El usuario de contabilidad quedo bloqueado tras varios intentos fallidos de acceso.",
            catNorte["Requerimiento"], Prioridad.Baja, EstadoSolicitud.Resuelta, user2Norte, agente2, -70,
            motivoResolucion: "Se restablecio la contrasena del usuario y se confirmo el acceso correcto.", offsetResolucionHoras: -40));
        solicitudes.Add(NuevaSolicitud(norte, ref correlativoNorte, "Duda sobre exportacion a Excel",
            "Quisiera saber como exportar el listado de clientes a un archivo de Excel.",
            catNorte["Consulta"], Prioridad.Baja, EstadoSolicitud.Resuelta, user1Norte, agente1, -55,
            motivoResolucion: "Se explico el flujo de exportacion desde el menu Reportes y quedo resuelto.", offsetResolucionHoras: -50));

        solicitudes.Add(NuevaSolicitud(norte, ref correlativoNorte, "Solicitud duplicada de acceso",
            "Pido acceso al modulo de compras (creo que ya habia pedido esto antes).",
            catNorte["Requerimiento"], Prioridad.Baja, EstadoSolicitud.Cancelada, user2Norte, null, -48,
            motivoCancelacion: "Duplicada de una solicitud anterior del mismo usuario."));
        solicitudes.Add(NuevaSolicitud(norte, ref correlativoNorte, "Prueba de ticket",
            "Este es un ticket de prueba que puede ignorarse por completo.",
            catNorte["Consulta"], Prioridad.Baja, EstadoSolicitud.Cancelada, user1Norte, null, -30,
            motivoCancelacion: "Ticket de prueba creado por error, se cancela."));

        solicitudes.Add(NuevaSolicitud(norte, ref correlativoNorte, "Actualizacion de datos de contacto",
            "Solicito actualizar mi numero de telefono y correo en el directorio interno.",
            catNorte["Requerimiento"], Prioridad.Media, EstadoSolicitud.Cerrada, user1Norte, agente1, -90,
            motivoResolucion: "Se actualizaron los datos de contacto en el directorio y se cerro el caso.", offsetResolucionHoras: -80));
        solicitudes.Add(NuevaSolicitud(norte, ref correlativoNorte, "Instalacion de software de oficina",
            "Necesito que instalen la suite de ofimatica en mi equipo nuevo.",
            catNorte["Requerimiento"], Prioridad.Baja, EstadoSolicitud.Cerrada, user2Norte, agente2, -110,
            motivoResolucion: "Se instalo la suite y el usuario confirmo su funcionamiento; se cierra.", offsetResolucionHoras: -95));

        solicitudes.Add(NuevaSolicitud(norte, ref correlativoNorte, "Pantalla azul intermitente",
            "El equipo de recepcion muestra pantalla azul varias veces al dia sin patron claro.",
            catNorte["Incidente"], Prioridad.Alta, EstadoSolicitud.Nueva, user1Norte, null, -2));
        solicitudes.Add(NuevaSolicitud(norte, ref correlativoNorte, "Configurar firma de correo",
            "Quiero configurar una firma institucional en mi cliente de correo.",
            catNorte["Consulta"], Prioridad.Baja, EstadoSolicitud.Nueva, user2Norte, null, -1));
        solicitudes.Add(NuevaSolicitud(norte, ref correlativoNorte, "VPN se desconecta cada 10 minutos",
            "La conexion VPN se cae aproximadamente cada diez minutos y debo reconectar.",
            catNorte["Incidente"], Prioridad.Media, EstadoSolicitud.Asignada, user1Norte, agente1, -6));
        solicitudes.Add(NuevaSolicitud(norte, ref correlativoNorte, "Solicito monitor adicional",
            "Requiero un segundo monitor para el area de disenio.",
            catNorte["Requerimiento"], Prioridad.Baja, EstadoSolicitud.Asignada, user2Norte, agente2, -5));
        solicitudes.Add(NuevaSolicitud(norte, ref correlativoNorte, "Base de datos de clientes desactualizada",
            "El listado de clientes muestra informacion de hace varios meses; hay que sincronizar.",
            catNorte["Falla critica"], Prioridad.Alta, EstadoSolicitud.EnProceso, user1Norte, agente1, -4));
        solicitudes.Add(NuevaSolicitud(norte, ref correlativoNorte, "Permisos para carpeta compartida",
            "Necesito acceso de escritura a la carpeta compartida del area contable.",
            catNorte["Requerimiento"], Prioridad.Media, EstadoSolicitud.EnProceso, user2Norte, agente2, -3));
        solicitudes.Add(NuevaSolicitud(norte, ref correlativoNorte, "Consulta sobre respaldo de archivos",
            "Quisiera saber con que frecuencia se respaldan mis archivos del servidor.",
            catNorte["Consulta"], Prioridad.Baja, EstadoSolicitud.Nueva, user1Norte, null, 1));
        solicitudes.Add(NuevaSolicitud(norte, ref correlativoNorte, "Teclado con teclas que no responden",
            "Varias teclas de mi teclado dejaron de funcionar; solicito reemplazo.",
            catNorte["Incidente"], Prioridad.Baja, EstadoSolicitud.Nueva, user2Norte, null, 2));
        solicitudes.Add(NuevaSolicitud(norte, ref correlativoNorte, "Migracion de correo a nuevo dominio",
            "Solicito la migracion de mi buzon al nuevo dominio corporativo antes de fin de mes.",
            catNorte["Requerimiento"], Prioridad.Alta, EstadoSolicitud.Asignada, user1Norte, agente1, 3));
        solicitudes.Add(NuevaSolicitud(norte, ref correlativoNorte, "Sistema no permite adjuntar imagenes",
            "Al crear un caso no puedo adjuntar imagenes en formato PNG; muestra un error.",
            catNorte["Incidente"], Prioridad.Media, EstadoSolicitud.EnProceso, user2Norte, agente2, 4));
        solicitudes.Add(NuevaSolicitud(norte, ref correlativoNorte, "Reabrir caso de acceso al inventario",
            "El acceso al inventario que se habia resuelto volvio a fallar; se reabre el caso.",
            catNorte["Requerimiento"], Prioridad.Media, EstadoSolicitud.EnProceso, user2Norte, agente1, 5));
        solicitudes.Add(NuevaSolicitud(norte, ref correlativoNorte, "Duda sobre horario de mantenimiento",
            "Quiero confirmar el horario de la ventana de mantenimiento del proximo fin de semana.",
            catNorte["Consulta"], Prioridad.Baja, EstadoSolicitud.Resuelta, user1Norte, agente2, -20,
            motivoResolucion: "Se informo la ventana de mantenimiento del sabado de 22:00 a 02:00.", offsetResolucionHoras: -18));
        solicitudes.Add(NuevaSolicitud(norte, ref correlativoNorte, "Falla critica en pasarela de pagos",
            "La pasarela de pagos rechaza todas las transacciones con tarjeta desde hace una hora.",
            catNorte["Falla critica"], Prioridad.Critica, EstadoSolicitud.Asignada, user1Norte, agente1, 6));

        solicitudes.Add(NuevaSolicitud(sur, ref correlativoSur, "No carga el expediente digital",
            "Al abrir un expediente el sistema queda cargando indefinidamente sin mostrar contenido.",
            catSur["Incidente"], Prioridad.Alta, EstadoSolicitud.Nueva, user1Sur, null, -50));
        solicitudes.Add(NuevaSolicitud(sur, ref correlativoSur, "Solicito plantilla de contrato",
            "Necesito la plantilla actualizada de contrato de arrendamiento para un cliente.",
            catSur["Requerimiento"], Prioridad.Media, EstadoSolicitud.Asignada, user1Sur, adminSur, -40));
        solicitudes.Add(NuevaSolicitud(sur, ref correlativoSur, "Consulta sobre firma electronica",
            "Quisiera saber si la firma electronica de los documentos tiene validez legal en el sistema.",
            catSur["Consulta"], Prioridad.Baja, EstadoSolicitud.Resuelta, user1Sur, adminSur, -60,
            motivoResolucion: "Se confirmo que la firma electronica cumple los requisitos legales vigentes.", offsetResolucionHoras: -55));
        solicitudes.Add(NuevaSolicitud(sur, ref correlativoSur, "Error al exportar demanda a PDF",
            "El boton de exportar a PDF de una demanda genera un archivo vacio.",
            catSur["Falla critica"], Prioridad.Critica, EstadoSolicitud.EnProceso, user1Sur, adminSur, -30));
        solicitudes.Add(NuevaSolicitud(sur, ref correlativoSur, "Actualizar datos del despacho",
            "Solicito actualizar la direccion y telefono del despacho en el pie de los documentos.",
            catSur["Requerimiento"], Prioridad.Baja, EstadoSolicitud.Nueva, user1Sur, null, -2));
        solicitudes.Add(NuevaSolicitud(sur, ref correlativoSur, "Caso cancelado por cambio de prioridad",
            "Se solicito una mejora que finalmente el despacho decidio no implementar.",
            catSur["Requerimiento"], Prioridad.Baja, EstadoSolicitud.Cancelada, user1Sur, null, -25,
            motivoCancelacion: "El despacho decidio no continuar con la solicitud."));
        solicitudes.Add(NuevaSolicitud(sur, ref correlativoSur, "Capacitacion sobre el modulo de agenda",
            "Solicito una breve capacitacion sobre el uso del modulo de agenda de audiencias.",
            catSur["Consulta"], Prioridad.Media, EstadoSolicitud.Cerrada, user1Sur, adminSur, -70,
            motivoResolucion: "Se realizo la capacitacion y el usuario quedo conforme; se cierra.", offsetResolucionHoras: -65));
        solicitudes.Add(NuevaSolicitud(sur, ref correlativoSur, "Lentitud al buscar jurisprudencia",
            "La busqueda de jurisprudencia tarda demasiado y a veces no devuelve resultados.",
            catSur["Incidente"], Prioridad.Media, EstadoSolicitud.Nueva, user1Sur, null, -1));

        db.Solicitudes.AddRange(solicitudes);

        await db.SaveChangesAsync();
    }
}