using System;
using System.Collections.Generic;
using System.Text;

namespace Threax.K8sDeploy.Config
{
    public class Secret
    {
        public String SecretName { get; set; }

        /// <summary>
        /// Use this to get the secret. Either the user defined secret name or one derived from the app name and a secret key.
        /// </summary>
        /// <param name="appName">The name of the app.</param>
        /// <param name="key">A key or unique name for the secret.</param>
        /// <returns></returns>
        public String GetSecretName(String appName, String key)
        {
            return SecretName ?? $"k8sconfig-secret-{appName.ToLowerInvariant()}-{key.ToLowerInvariant()}";
        }

        public String Source { get; set; }

        public String Dest { get; set; }

        /// <summary>
        /// The type of the secret mount. Default: Directory
        /// </summary>
        public PathType Type { get; set; } = PathType.Directory;
    }
}
