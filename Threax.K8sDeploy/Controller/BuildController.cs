using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Threax.K8sDeploy.Config;
using Threax.K8sDeploy.Services;

namespace Threax.K8sDeploy.Controller
{
    class BuildController : IController
    {
        private AppConfig appConfig;
        private ILogger logger;
        private IProcessRunner processRunner;

        public BuildController(AppConfig appConfig, ILogger<BuildController> logger, IProcessRunner processRunner)
        {
            this.appConfig = appConfig;
            this.logger = logger;
            this.processRunner = processRunner;
        }

        public Task Run()
        {
            var clonePath = appConfig.GetSourcePath();
            var dockerFile = Path.GetFullPath(Path.Combine(clonePath, appConfig.Dockerfile));
            var image = appConfig.Name;
            var tag = $"{image}:{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}";
            var currentTag = appConfig.CurrentTag ?? throw new InvalidOperationException($"Please provide {nameof(appConfig.CurrentTag)} when using build.");

            var args = $"build {clonePath} -f {dockerFile} -t {tag} -t {image}:{currentTag}";

            if (appConfig.AlwaysPull)
            {
                args += " --pull";
            }

            processRunner.RunProcessWithOutput(new ProcessStartInfo("docker", args));

            return Task.CompletedTask;
        }
    }
}
