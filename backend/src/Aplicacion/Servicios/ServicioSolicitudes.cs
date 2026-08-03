using MesaSitec.Aplicacion.Abstracciones;
using MesaSitec.Aplicacion.Comun;
using MesaSitec.Aplicacion.DTOs;
using MesaSitec.Dominio.Entidades;
using MesaSitec.Dominio.Enums;
using MesaSitec.Dominio.Errores;
using MesaSitec.Dominio.Servicios;
using Microsoft.EntityFrameworkCore;

namespace MesaSitec.Aplicacion.Servicios;

public class ServicioSolicitudes
{
    private readonly IAppDbContext _db;
    private readonly IContextoUsuario _ctx;
    private readonly IProveedorFecha _reloj;

    public ServicioSolicitudes(IAppDbContext db, IContextoUsuario ctx, IProveedorFecha reloj)
    {
        _db = db;
        _ctx = ctx;
        _reloj = reloj;
    }

    private static readonly EstadoSolicitud[] EstadosTerminales =
        { EstadoSolicitud.Resuelta, EstadoSolicitud.Cerrada, EstadoSolicitud.Cancelada };

    public async Task<PaginaDto<SolicitudListaDto>> ListarAsync(ConsultaSolicitudes q)
    {
        if (q.Page < 1)
            throw new ParametroInvalidoException("El parametro 'page' debe ser mayor o igual a 1.");
        if (q.PageSize < 1 || q.PageSize > 100)
            throw new ParametroInvalidoException("El parametro 'pageSize' debe estar entre 1 y 100.");

        var ahora = _reloj.AhoraUtc;
        IQueryable<Solicitud> query = _db.Solicitudes
            .Where(s => s.TenantId == _ctx.TenantId);

        // Un Solicitante solo ve las que el creo
        if (PoliticaPermisos.DebeVerSoloPropias(_ctx.Rol))
            query = query.Where(s => s.SolicitanteId == _ctx.UsuarioId);

        var estado = ParserEnums.EstadoFiltro(q.Estado);
        if (estado is not null)
            query = query.Where(s => s.Estado == estado);

        var prioridad = ParserEnums.PrioridadFiltro(q.Prioridad);
        if (prioridad is not null)
            query = query.Where(s => s.Prioridad == prioridad);

        if (q.CategoriaId is Guid catId)
            query = query.Where(s => s.CategoriaId == catId);

        if (q.AgenteId is Guid agId)
            query = query.Where(s => s.AgenteId == agId);

        if (!string.IsNullOrWhiteSpace(q.Q))
        {
            var termino = q.Q.Trim().ToLower();
            query = query.Where(s =>
                s.Titulo.ToLower().Contains(termino) ||
                s.Descripcion.ToLower().Contains(termino) ||
                s.Codigo.ToLower().Contains(termino));
        }

        if (q.Vencidas is bool vencidas)
        {
            if (vencidas)
                query = query.Where(s => s.FechaLimiteSla < ahora && !EstadosTerminales.Contains(s.Estado));
            else
                query = query.Where(s => !(s.FechaLimiteSla < ahora && !EstadosTerminales.Contains(s.Estado)));
        }

        query = AplicarOrden(query, q.Sort);

        var total = await query.CountAsync();
        var totalPaginas = total == 0 ? 0 : (int)Math.Ceiling(total / (double)q.PageSize);

        var pagina = await query
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .Include(s => s.Categoria)
            .Include(s => s.Agente)
            .ToListAsync();

        var items = pagina.Select(s => new SolicitudListaDto(
            s.Id,
            s.Codigo,
            s.Titulo,
            s.Estado,
            s.Prioridad,
            new ReferenciaDto(s.Categoria!.Id, s.Categoria.Nombre),
            s.Agente is null ? null : new ReferenciaDto(s.Agente.Id, s.Agente.Nombre),
            s.FechaCreacion,
            s.FechaLimiteSla,
            s.EstaVencida(ahora)
        )).ToList();

        return new PaginaDto<SolicitudListaDto>(items, q.Page, q.PageSize, total, totalPaginas);
    }
    private static IQueryable<Solicitud> AplicarOrden(IQueryable<Solicitud> query, string? sort)
    {
        sort = string.IsNullOrWhiteSpace(sort) ? "-fechaCreacion" : sort.Trim();

        return sort switch
        {
            "fechaCreacion"  => query.OrderBy(s => s.FechaCreacion).ThenBy(s => s.Id),
            "-fechaCreacion" => query.OrderByDescending(s => s.FechaCreacion).ThenBy(s => s.Id),
            "prioridad"      => query.OrderBy(s => s.Prioridad == Prioridad.Baja ? 0 : s.Prioridad == Prioridad.Media ? 1 : s.Prioridad == Prioridad.Alta ? 2 : 3).ThenBy(s => s.Id),
            "-prioridad"     => query.OrderByDescending(s => s.Prioridad == Prioridad.Baja ? 0 : s.Prioridad == Prioridad.Media ? 1 : s.Prioridad == Prioridad.Alta ? 2 : 3).ThenBy(s => s.Id),
            "codigo"         => query.OrderBy(s => s.Codigo).ThenBy(s => s.Id),
            _ => throw new ParametroInvalidoException(
                $"El parametro 'sort' con valor '{sort}' no es valido.")
        };
    }
    public async Task<SolicitudDetalleDto> CrearAsync(CrearSolicitudRequest req)
    {
        // Los tres roles pueden crear
        if (!PoliticaPermisos.PuedeCrear(_ctx.Rol))
            throw new OperacionNoPermitidaException();

        var validador = new ValidadorCampos();
        validador.ValidarLongitud("titulo", req.Titulo, 5, 120);
        validador.ValidarLongitud("descripcion", req.Descripcion, 10, 4000);
        validador.LanzarSiHayErrores();

        // La categoria debe existir, estar activa y ser del mismo tenant
        var categoria = await ObtenerCategoriaValidaAsync(req.CategoriaId);

        var ahora = _reloj.AhoraUtc;

        var solicitud = new Solicitud
        {
            Id = Guid.NewGuid(),
            TenantId = _ctx.TenantId,
            Codigo = await GenerarCodigoAsync(ahora.Year),
            Titulo = req.Titulo.Trim(),
            Descripcion = req.Descripcion.Trim(),
            CategoriaId = categoria.Id,
            Prioridad = req.Prioridad,
            Estado = EstadoSolicitud.Nueva,
            SolicitanteId = _ctx.UsuarioId,
            AgenteId = null,
            FechaCreacion = ahora,
            FechaLimiteSla = CalculadoraSLA.CalcularFechaLimite(ahora, categoria.SlaHoras, req.Prioridad)
        };

        _db.Solicitudes.Add(solicitud);
        await _db.SaveChangesAsync();

        return await ObtenerDetalleAsync(solicitud.Id);
    }
    public async Task<SolicitudDetalleDto> ObtenerDetalleAsync(Guid id)
    {
        var solicitud = await CargarDelTenantAsync(id);

        var esPropia = solicitud.SolicitanteId == _ctx.UsuarioId;
        if (!PoliticaPermisos.PuedeVerDetalle(_ctx.Rol, esPropia))
            // Se responde 404 (no 403) para no confirmar la existencia de un recurso ajeno.
            throw new RecursoNoEncontradoException();

        return MapearDetalle(solicitud, esPropia);
    }
    public async Task<SolicitudDetalleDto> EditarAsync(Guid id, EditarSolicitudRequest req)
    {
        var solicitud = await CargarDelTenantAsync(id);
        var esPropia = solicitud.SolicitanteId == _ctx.UsuarioId;

        if (!PoliticaPermisos.PuedeEditar(_ctx.Rol, esPropia, solicitud.Estado))
            throw new OperacionNoPermitidaException();

        var validador = new ValidadorCampos();
        validador.ValidarLongitud("titulo", req.Titulo, 5, 120);
        validador.ValidarLongitud("descripcion", req.Descripcion, 10, 4000);
        validador.LanzarSiHayErrores();

        var categoria = await ObtenerCategoriaValidaAsync(req.CategoriaId);

        solicitud.Titulo = req.Titulo.Trim();
        solicitud.Descripcion = req.Descripcion.Trim();
        solicitud.CategoriaId = categoria.Id;
        solicitud.Prioridad = req.Prioridad;

        if (!EstadosTerminales.Contains(solicitud.Estado))
        {
            solicitud.FechaLimiteSla = CalculadoraSLA.CalcularFechaLimite(
                solicitud.FechaCreacion, categoria.SlaHoras, solicitud.Prioridad);
        }

        await _db.SaveChangesAsync();
        return await ObtenerDetalleAsync(solicitud.Id);
    }
    public async Task<SolicitudDetalleDto> EjecutarTransicionAsync(Guid id, TransicionRequest req)
    {
        var solicitud = await CargarDelTenantAsync(id);
        var esPropia = solicitud.SolicitanteId == _ctx.UsuarioId;

        var accion = ParserEnums.Accion(req.Accion);
        if (!PoliticaPermisos.PuedeEjecutarAccion(_ctx.Rol, accion, esPropia))
            throw new OperacionNoPermitidaException();

        var nuevoEstado = MaquinaEstados.Siguiente(solicitud.Estado, accion);

        switch (accion)
        {
            case AccionSolicitud.Asignar:
                await ValidarYAsignarAgenteAsync(solicitud, req.AgenteId);
                break;

            case AccionSolicitud.Resolver:
                ExigirMotivo(req.Motivo, 20);
                solicitud.MotivoResolucion = req.Motivo!.Trim();
                solicitud.FechaResolucion = _reloj.AhoraUtc;
                break;

            case AccionSolicitud.Cancelar:
                ExigirMotivo(req.Motivo, 10);
                solicitud.MotivoCancelacion = req.Motivo!.Trim();
                break;

            case AccionSolicitud.Reabrir:
                solicitud.FechaResolucion = null;
                solicitud.MotivoResolucion = null;
                break;

            // iniciar y cerrar no requieren datos adicionales
        }

        solicitud.Estado = nuevoEstado;
        await _db.SaveChangesAsync();

        return await ObtenerDetalleAsync(solicitud.Id);
    }

    private async Task<Solicitud> CargarDelTenantAsync(Guid id)
    {
        var solicitud = await _db.Solicitudes
            .Include(s => s.Categoria)
            .Include(s => s.Agente)
            .Include(s => s.Solicitante)
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == _ctx.TenantId);

        return solicitud ?? throw new RecursoNoEncontradoException();
    }

    private async Task<Categoria> ObtenerCategoriaValidaAsync(Guid categoriaId)
    {
        var categoria = await _db.Categorias
            .FirstOrDefaultAsync(c => c.Id == categoriaId && c.TenantId == _ctx.TenantId && c.Activo);

        if (categoria is null)
        {
            var errores = new Dictionary<string, string[]>
            {
                ["categoriaId"] = new[] { "La categoria no existe, esta inactiva o no pertenece a su organizacion." }
            };
            throw new ValidacionException(errores);
        }
        return categoria;
    }
    private async Task<string> GenerarCodigoAsync(int anio)
    {
        var prefijo = $"SOL-{anio}-";
        // Correlativo independiente por tenant y por anio.
        var cantidad = await _db.Solicitudes
            .CountAsync(s => s.TenantId == _ctx.TenantId && s.Codigo.StartsWith(prefijo));
        var siguiente = cantidad + 1;
        return $"{prefijo}{siguiente:D5}";
    }

    private async Task ValidarYAsignarAgenteAsync(Solicitud solicitud, Guid? agenteId)
    {
        if (agenteId is not Guid id)
        throw new AgenteInvalidoException("Debe indicar el 'agenteId' para asignar.");

        var agente = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == _ctx.TenantId);

        if (agente is null || !agente.Activo || (agente.Rol != Rol.Agente && agente.Rol != Rol.Admin))
        throw new AgenteInvalidoException();

        solicitud.AgenteId = agente.Id;
    }

    private static void ExigirMotivo(string? motivo, int minimo)
    {
        if ((motivo?.Trim().Length ?? 0) < minimo)
        throw new MotivoRequeridoException($"El motivo es obligatorio y debe tener al menos {minimo} caracteres.");
    }

    private SolicitudDetalleDto MapearDetalle(Solicitud s, bool esPropia)
    {
        var acciones = MaquinaEstados.AccionesDisponibles(s.Estado)
        .Where(a => PoliticaPermisos.PuedeEjecutarAccion(_ctx.Rol, a, esPropia))
        .Select(a => a.ToString().ToLowerInvariant())
        .ToList();

        return new SolicitudDetalleDto(
            s.Id,
            s.Codigo,
            s.Titulo,
            s.Descripcion,
            s.Estado,
            s.Prioridad,
            new ReferenciaDto(s.Categoria!.Id, s.Categoria.Nombre),
            new ReferenciaDto(s.Solicitante!.Id, s.Solicitante.Nombre),
            s.Agente is null ? null : new ReferenciaDto(s.Agente.Id, s.Agente.Nombre),
            s.FechaCreacion,
            s.FechaLimiteSla,
            s.FechaResolucion,
            s.MotivoResolucion,
            s.MotivoCancelacion,
            s.EstaVencida(_reloj.AhoraUtc),
            acciones);
    }
}