using System;
using System.Collections.Generic;
using System.Text;

namespace Threax.K8sDeploy.Config
{
    public class Secret
    {
        public String SecretName { get; set; }

        public String Dest { get; set; }

        /// <summary>
        /// The type of the secret mount. Default: Directory
        /// </summary>
        public PathType Type { get; set; } = PathType.Directory;
    }
}
