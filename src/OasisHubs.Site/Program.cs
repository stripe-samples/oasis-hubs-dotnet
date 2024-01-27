using OasisHubs.Site;
using OasisHubs.Site.Cli;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

Log.Logger = new LoggerConfiguration()
   .MinimumLevel.Information()
   .WriteTo.Console(
      theme: AnsiConsoleTheme.Code,
      outputTemplate:
      "[{Level:w}]: {Timestamp:dd-MM-yyyy:HH:mm:ss} {MachineName} {EnvironmentName} {SourceContext} {Message}{NewLine}{Exception}")
   .CreateBootstrapLogger();

try {
   await Host.CreateDefaultBuilder()
      .UseDefaultServiceProvider( _ => {})
      .ConfigureServices((context, services) => {
         services.AddCoreServices(context.Configuration);
      })
      .UseSerilog((context, services, configuration) => configuration
         .ReadFrom.Configuration(context.Configuration))
      .RunCommandLineApplicationAsync<RootCommand>(args);
}
catch (Exception ex) {
   Log.Fatal(ex, "Application terminated unexpectedly");
}
finally {
   Log.CloseAndFlush();
}
