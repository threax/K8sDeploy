using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Threax.K8sDeploy.Config
{
    class AppConfig
    {
        public String Name { get; set; }

        public String Domain { get; set; } = "dev.threax.com";

        public String RepoUrl { get; set; }

        public long? User { get; set; } = 10000;

        public long? Group { get; set; } = 10000;

        public String SrcBasePath { get; set; } = "src";

        public String AppDataBasePath { get; set; } = "data";

        public String Dockerfile { get; set; }

        public String BaseTag { get; set; } = "k8sdeploy";

        public bool AlwaysPull { get; set; } = true;

        public String DeploymentFile { get; set; } = "AppDeployment.yaml";

        public String InitCommand { get; set; }

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

        /// <summary>
        /// Build the path to clone code into.
        /// </summary>
        /// <returns></returns>
        public String GetAppDataPath()
        {
            return Path.GetFullPath(Path.Combine(AppDataBasePath, Name));
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
