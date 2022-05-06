using System.Reflection;
using IdentityProtectionMonitoring.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration(configuration =>
    {
        configuration.AddEnvironmentVariables("Config.");
    })
    .ConfigureServices(services =>
    {
        services.AddSingleton<IGraphClientService, GraphClientService>();
    })
    .Build();

host.Run();