using JsonTranslator;
using JsonTranslator.Services;
using Serilog;
using Serilog.Events;

if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Log")))
{
    try
    {
        Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Log"));
    }
    catch
    {
        Console.WriteLine("Could not create log directory");
    }
}

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Debug)
    .Enrich.FromLogContext()
    .WriteTo.File(Path.Combine(Directory.GetCurrentDirectory(), "Log", "log.txt"), rollingInterval: RollingInterval.Hour)
    .CreateLogger();

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddLogging(logging =>
        {
            logging.AddSerilog();
        });
        services.AddHostedService<Worker>();
        services.AddTransient<IFileService, FileService>();
        services.AddTransient<IFtpService, FtpService>();
        services.AddTransient<IApiService, ApiService>();
        services.AddTransient<ISourcesService, SourcesService>();
    })
    .Build();

await host.RunAsync();