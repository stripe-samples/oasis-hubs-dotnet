using McMaster.Extensions.CommandLineUtils;
using McMaster.Extensions.CommandLineUtils.Abstractions;

namespace OasisHubs.Site.Cli;

[Command(Name = "serve", Description = "Run Oasis web application")]
public class ServeCommand : CommandBase {

    private CommandLineContext _ctx;

    public ServeCommand(CommandLineContext ctx) {
        this._ctx = ctx;
    }
    public virtual Task OnExecuteAsync() =>
        WebApplication.CreateBuilder(_ctx.Arguments)
            .ConfigureServices()
            .ConfigurePipeline()
            .RunAsync();

}
