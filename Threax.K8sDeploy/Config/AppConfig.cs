using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Threax.K8sDeploy.Config
{
    class AppConfig
    {
        public AppConfig(String sourceFile)
        {
            this.SourceFile = sourceFile;
        }

        public String SourceFile { get; private set; }

        public String Name { get; set; }

        public String Domain { get; set; } = "dev.threax.com";

        public String RepoUrl { get; set; }

        public long? User { get; set; } = 10000;

        public long? Group { get; set; } = 10000;

        public String SrcBasePath { get; set; } = "src";

        /// <summary>
        /// The branch of the repo to use. Default: master.
        /// </summary>
        public String Branch { get; set; } = "master";

        public String AppDataBasePath { get; set; } = "data";

        public String Dockerfile { get; set; }

        public String BaseTag { get; set; } = "k8sdeploy";

        public bool AlwaysPull { get; set; } = true;

        public String DeploymentFile { get; set; } = "AppDeployment.yaml";

        public String InitCommand { get; set; }

        public Dictionary<String, Volume> Volumes { get; set; }

        /// <summary>
        /// Set this to true to auto mount the app settings config. Default: true.
        /// </summary>
        public bool AutoMountAppSettings { get; set; } = true;

        /// <summary>
        /// The mount path for the appsettings file. Default: /app/appsettings.Production.json.
        /// </summary>
        public String AppSettingsMountPath { get; set; } = "/app/appsettings.Production.json";

        /// <summary>
        /// The sub path for the appsettings file. Default: appsettings.Production.json.
        /// </summary>
        public String AppSettingsSubPath { get; set; } = "appsettings.Production.json";

        /// <summary>
        /// Validate that this config is correct. Throws an exception if there is an error.
        /// </summary>
        public void Validate()
        {
            if(Name == null)
            {
                throw new InvalidOperationException($"{nameof(Name)} cannot be null. Please provide a value.");
            }

            if(BaseTag == null)
            {
                throw new InvalidOperationException($"{nameof(BaseTag)} cannot be null. Please provide a value.");
            }
        }

        /// <summary>
        /// Build the path to clone code into.
        /// </summary>
        /// <returns></returns>
        public String GetSourcePath()
        {
            return Path.GetFullPath(Path.Combine(SrcBasePath, Name));
        }

        public String GetAppDataPath(String path)
        {
            return Path.GetFullPath(Path.Combine(AppDataBasePath, path));
        }

        public String GetBuildTag()
        {
            return $"{BaseTag}-{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}";
        }

        public String GetCurrentTag()
        {
            return $"{BaseTag}-current";
        }
    }
}
