using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Threax.K8sDeploy.Services
{
    public class ConfigFileProvider : IConfigFileProvider
    {
        public String GetConfigText()
        {
            var path = Path.GetFullPath("appsettings.json");

            using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }
    }
}
