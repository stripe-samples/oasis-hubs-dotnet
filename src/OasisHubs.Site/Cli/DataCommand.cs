using McMaster.Extensions.CommandLineUtils;
using McMaster.Extensions.CommandLineUtils.Abstractions;

namespace OasisHubs.Site.Cli;

[Command(Name = "data", Description = "Data management commands")]
[Subcommand(typeof(CreateCommand), typeof(SeedCommand), typeof(DropCommand))]
public class DataCommand : CommandBase {
    private readonly CommandLineApplication _cmdApp;
    private readonly CommandLineContext _commandLineContext;

    public DataCommand(CommandLineApplication cmdApp, CommandLineContext commandLineContext) {
        this._cmdApp = cmdApp;
        this._commandLineContext = commandLineContext;
    }
    private void OnExecute() {
        _cmdApp.ShowHelp();
    }
}
