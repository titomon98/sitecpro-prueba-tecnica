using MesaSitec.Dominio.Enums;

namespace MesaSitec.Dominio.Servicios;

//Aqui se filtran las solicitudes para que se puedan ver solo los recursos propios
//No deben ser visibles las solicitudes de los demas. 
//Tambien se validan otras operaciones para ver si el rol tiene permitido hacerla
public static class PoliticaPermisos
{
    public static bool DebeVerSoloPropias(Rol rol) => rol == Rol.Solicitante;
    public static bool PuedeVerDetalle(Rol rol, bool esPropia) => rol switch
    {
        Rol.Admin => true,
        Rol.Agente => true,
        Rol.Solicitante => esPropia,
        _ => false
    };
    public static bool PuedeCrear(Rol rol) => true;
    public static bool PuedeEditar(Rol rol, bool esPropia, EstadoSolicitud estado) => rol switch
    {
        Rol.Admin => true,
        Rol.Agente => true,
        Rol.Solicitante => esPropia && estado == EstadoSolicitud.Nueva,
        _ => false
    };

    public static bool PuedeEjecutarAccion(Rol rol, AccionSolicitud accion, bool esPropia) => accion switch
    {
        AccionSolicitud.Asignar or AccionSolicitud.Iniciar or AccionSolicitud.Resolver or AccionSolicitud.Reabrir
            => rol is Rol.Admin or Rol.Agente,

        AccionSolicitud.Cerrar
            => rol is Rol.Admin or Rol.Agente || (rol == Rol.Solicitante && esPropia),

        AccionSolicitud.Cancelar
            => rol == Rol.Admin,

        _ => false
    };
}