using McMaster.Extensions.CommandLineUtils;
using Microsoft.EntityFrameworkCore;
using OasisHubs.Site.Data;
using Serilog;

namespace OasisHubs.Site.Cli;

[Command(Name = "create", Description = "create database")]
public class CreateCommand : CommandBase {
    private readonly IDbContextFactory<OasisHubsDbContext> _dbContextFactory;
    private readonly ILogger<CreateCommand> _logger;

    public CreateCommand(IDbContextFactory<OasisHubsDbContext> dbContextFactory, ILogger<CreateCommand> logger) {
        this._dbContextFactory = dbContextFactory;
        this._logger = logger;

    }

    private async Task OnExecuteAsync() {

        Log.Information("Creating database ...");

        await using var context = await this._dbContextFactory.CreateDbContextAsync();
        await context.Database.EnsureCreatedAsync();

        Log.Information("Database Created");
    }
}
