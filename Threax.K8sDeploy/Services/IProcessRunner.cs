using System.Diagnostics;

namespace Threax.K8sDeploy.Services
{
    interface IProcessRunner
    {
        void RunProcessWithOutput(ProcessStartInfo startInfo);
    }
}