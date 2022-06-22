using JsonTranslator;
using JsonTranslator.Services;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.File(@"C:\temp\workerservice\"+DateTime.UtcNow.Day+"-"+DateTime.UtcNow.Month+"-"+DateTime.UtcNow.Year+".txt")
    .CreateLogger();

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<IFtpService, FtpService>();
        services.AddSingleton<IApiService, ApiService>();
    })
    .Build();

await host.RunAsync();