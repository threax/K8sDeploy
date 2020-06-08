using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Threax.K8sDeploy.Config;

namespace Threax.K8sDeploy.Services
{
    class ConfigFileProvider : IConfigFileProvider
    {
        private readonly K8sDeployConfig appConfig;

        public ConfigFileProvider(K8sDeployConfig appConfig)
        {
            this.appConfig = appConfig;
        }

        public String GetConfigText()
        {
            var path = appConfig.SourceFile;

            using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }
    }
}
