using System;
using System.Collections.Generic;
using System.Text;

namespace Threax.K8sDeploy.Config
{
    public class Volume
    {
        public String Source { get; set; }

        public String Dest { get; set; }

        /// <summary>
        /// The type of the volume mount. Default: Directory
        /// </summary>
        public VolumeType Type { get; set; } = VolumeType.Directory;
    }

    public enum VolumeType
    {
        Directory = 0,
        File = 1
    }
}
