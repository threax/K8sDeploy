using System;
using k8s;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Threax.Extensions.Configuration.SchemaBinder;
using Threax.K8sDeploy.Config;
using Threax.K8sDeploy.Controller;
using Threax.K8sDeploy.Services;

namespace Threax.K8sDeploy
{
    class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<HostedService>();

                    services.AddScoped<SchemaConfigurationBinder>(s => new SchemaConfigurationBinder(s.GetRequiredService<IConfiguration>()));

                    services.AddScoped<IProcessRunner, ProcessRunner>();
                    services.AddScoped<IConfigFileProvider, ConfigFileProvider>();

                    services.AddScoped<IKubernetes>(s =>
                    {
                        var config = KubernetesClientConfiguration.BuildDefaultConfig();
                        IKubernetes client = new Kubernetes(config);
                        return client;
                    });

                    services.AddScoped<AppConfig>(s =>
                    {
                        var config = s.GetRequiredService<SchemaConfigurationBinder>();
                        var appConfig = new AppConfig();
                        config.Bind("K8sDeploy", appConfig);
                        appConfig.Validate();
                        return appConfig;
                    });

                    var controllerType = typeof(HelpController);
                    //Determine which controller to use.
                    if(args.Length > 0)
                    {
                        var command = args[0];
                        controllerType = ControllerFinder.GetControllerType(command);
                    }
                    services.AddScoped(typeof(IController), controllerType);
                });
    }
}
