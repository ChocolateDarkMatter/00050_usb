using Serilog;
using Serilog.Formatting.Compact;
using UsbOverIp.BackendService;
using UsbOverIp.BackendService.Api;
using UsbOverIp.BackendService.Discovery;
using UsbOverIp.BackendService.Services;
using UsbOverIp.Shared.Contracts;
using UsbOverIp.Shared.Models;

// Ensure working directory is set to app directory for service mode
Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .Build())
    .CreateLogger();

try
{
    Log.Information("Starting UsbOverIp Backend Service");

    var builder = WebApplication.CreateBuilder(args);

    // Add Windows Service support
    builder.Host.UseWindowsService();

    // Configure Serilog
    builder.Services.AddSerilog();

    // Configure ServerConfiguration
    builder.Services.Configure<ServerConfiguration>(
        builder.Configuration.GetSection("ServerConfiguration"));

    // Ensure ServerId is generated if not set
    var serverConfig = builder.Configuration.GetSection("ServerConfiguration").Get<ServerConfiguration>();
    if (serverConfig != null && serverConfig.ServerId == Guid.Empty)
    {
        serverConfig.ServerId = Guid.NewGuid();
        Log.Information("Generated new ServerId: {ServerId}", serverConfig.ServerId);
    }

    // Register services
    builder.Services.AddSingleton<IUsbipdClient, UsbipdClient>();
    builder.Services.AddSingleton<IDeviceManager, DeviceManager>();

    // Add hosted services
    builder.Services.AddHostedService<Worker>();
    builder.Services.AddHostedService<DiscoveryBroadcaster>();

    var app = builder.Build();

    // Map API endpoints
    app.MapApiEndpoints();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Make Program class public for integration tests
public partial class Program { }
