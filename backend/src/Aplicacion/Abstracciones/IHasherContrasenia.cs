namespace MesaSitec.Aplicacion.Abstracciones;
public interface IHasherContrasenia { 
    string Hashear(string plana); 
    bool Verificar(string plana, string hash); 
}