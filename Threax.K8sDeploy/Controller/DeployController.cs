using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Threax.K8sDeploy.Config;
using Threax.K8sDeploy.Services;

namespace Threax.K8sDeploy.Controller
{
    class DeployController : IController
    {
        private AppConfig appConfig;
        private ILogger logger;
        private IProcessRunner processRunner;

        public DeployController(AppConfig appConfig, ILogger<DeployController> logger, IProcessRunner processRunner)
        {
            this.appConfig = appConfig;
            this.logger = logger;
            this.processRunner = processRunner;
        }

        public Task Run()
        {
            var image = appConfig.Name;
            var currentTag = appConfig.GetCurrentTag();

            var args = $"inspect --format=\"{{{{json .RepoTags}}}}\" {image}:{currentTag}";

            //Get the tags from docker
            var startInfo = new ProcessStartInfo("docker", args);
            var json = processRunner.RunProcessWithOutputGetOutput(startInfo);
            var tags = JsonSerializer.Deserialize<List<String>>(json);

            //Remove any tags that weren't set by this software
            tags.Remove($"{image}:{currentTag}");
            var tagFilter = $"{image}:{appConfig.BaseTag}";
            tags = tags.Where(i => i.StartsWith(tagFilter)).ToList(); 
            tags.Sort(); //Docker seems to store these in order, but sort them by their names, the tags are date based and the latest will always be last

            var latestDateTag = tags.LastOrDefault();

            if(latestDateTag == null)
            {
                throw new InvalidOperationException($"Cannot find a tag in the format '{tagFilter}' on image '{image}'.");
            }

            return Task.CompletedTask;
        }
    }
}
