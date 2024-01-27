using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OasisHubs.Site.Data;

public class OasisHubDesignTimeDbContextFactory : IDesignTimeDbContextFactory<OasisHubsDbContext>
{
    public virtual OasisHubsDbContext CreateDbContext(string[] args)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json")
               .AddJsonFile("appsettings.Development.json")
               .Build();

        var optionsBuilder = new DbContextOptionsBuilder<OasisHubsDbContext>();
        optionsBuilder.UseSqlServer(configuration.GetConnectionString("OasisHubsSQLServer"), opts => opts.EnableRetryOnFailure());

        return new OasisHubsDbContext(optionsBuilder.Options);
    }
}
