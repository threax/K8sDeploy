using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using Threax.Extensions.Configuration.SchemaBinder;
using Threax.K8sDeploy.Config;

namespace Threax.K8sDeploy.Controller
{
    class UpdateSchemaController : IController
    {
        private readonly AppConfig appConfig;
        private readonly SchemaConfigurationBinder schemaConfigurationBinder;

        public UpdateSchemaController(AppConfig appConfig, SchemaConfigurationBinder schemaConfigurationBinder)
        {
            this.appConfig = appConfig;
            this.schemaConfigurationBinder = schemaConfigurationBinder;
        }

        public async Task Run()
        {
            var config = await this.schemaConfigurationBinder.CreateSchema();

            var output = appConfig.GetConfigPath(appConfig.SchemaOutputPath);

            using (var writer = new StreamWriter(File.Open(output, FileMode.Create, FileAccess.ReadWrite, FileShare.None)))
            {
                writer.Write(config);
            }
        }
    }
}
