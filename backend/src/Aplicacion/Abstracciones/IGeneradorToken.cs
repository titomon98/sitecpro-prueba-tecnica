using MesaSitec.Dominio.Entidades;
namespace MesaSitec.Aplicacion.Abstracciones;
public interface IGeneradorToken { 
    (string token, int expiraEnSegundos) Generar(Usuario usuario);
}