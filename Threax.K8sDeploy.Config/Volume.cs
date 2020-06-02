using System;
using System.Collections.Generic;
using System.Text;

namespace Threax.K8sDeploy.Config
{
    public class Volume
    {
        public String Source { get; set; }

        public String Destination { get; set; }

        /// <summary>
        /// The type of the volume mount. Default: Directory
        /// </summary>
        public PathType Type { get; set; } = PathType.Directory;
    }
}
