//Para generar las migraciones

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MesaSitec.Infraestructura.Persistencia;

public class FabricaDbContextDisenio : IDesignTimeDbContextFactory<MesaSitecDbContext>
{
    public MesaSitecDbContext CreateDbContext(string[] args)
    {
        var opciones = new DbContextOptionsBuilder<MesaSitecDbContext>()
            //Cadena de conexion ficticia solo para generar el esquema de la migracion.
            .UseSqlite("Data Source=mesasitec_disenio.db").Options;

        return new MesaSitecDbContext(opciones);
    }
}