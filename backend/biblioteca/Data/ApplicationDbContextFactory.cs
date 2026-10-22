using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using biblioteca.Configuration;

namespace biblioteca.Data;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        EnvLoader.Load();
        var connectionString = EnvLoader.GetConnectionString();

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseMySQL(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
