using McMaster.Extensions.CommandLineUtils;
using Microsoft.EntityFrameworkCore;
using OasisHubs.Site.Data;
using Serilog;

namespace OasisHubs.Site.Cli;

[Command(Name = "drop", Description = "Drops database")]
public class DropCommand : CommandBase {
    private readonly IDbContextFactory<OasisHubsDbContext> _dbContextFactory;
    private readonly ILogger<DropCommand> _logger;

    public DropCommand(IDbContextFactory<OasisHubsDbContext> dbContextFactory, ILogger<DropCommand> logger) {
        this._dbContextFactory = dbContextFactory;
        this._logger = logger;

    }

    private async Task OnExecuteAsync() {

        var proceed = Prompt.GetYesNo("Are you SURE you want to DELETE this database?",
            defaultAnswer: false, promptColor: ConsoleColor.Red);

        if (!proceed) return;

        Log.Information("Deleting database ...");

        await using var context = await this._dbContextFactory.CreateDbContextAsync();
        await context.Database.EnsureDeletedAsync();

        Log.Information("Database deleted");
        Log.Information("You have to manually purge any Stripe data from within the dashboard!");
    }
}
