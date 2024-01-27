using McMaster.Extensions.CommandLineUtils;

namespace OasisHubs.Site.Cli;

public abstract class CommandBase { }

[Command(
    FullName = "Oasis Hubs",
    Name = "oasis",
    UnrecognizedArgumentHandling = UnrecognizedArgumentHandling.StopParsingAndCollect)]
[Subcommand(typeof(DataCommand), typeof(ServeCommand))]
public class RootCommand : CommandBase {

   private CommandLineApplication _cmdApp;

   public RootCommand(CommandLineApplication cmdApp) {
      this._cmdApp = cmdApp;
   }
   public virtual void OnExecute() { _cmdApp.ShowHelp(); }
}
