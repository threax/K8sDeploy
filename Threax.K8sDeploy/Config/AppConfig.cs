using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Threax.K8sDeploy.Config
{
    class AppConfig
    {
        public String Name { get; set; }

        public String RepoUrl { get; set; }

        public String SrcBasePath { get; set; } = "src";

        /// <summary>
        /// Validate that this config is correct. Throws an exception if there is an error.
        /// </summary>
        public void Validate()
        {
            if(Name == null)
            {
                throw new InvalidOperationException("K8sDeploy.Name cannot be null. Please provide a value.");
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
    }
}
