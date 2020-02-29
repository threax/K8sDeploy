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
        private ITokenReplacer tokenReplacer;

        public DeployController(AppConfig appConfig, ILogger<DeployController> logger, IProcessRunner processRunner, ITokenReplacer tokenReplacer)
        {
            this.appConfig = appConfig;
            this.logger = logger;
            this.processRunner = processRunner;
            this.tokenReplacer = tokenReplacer;
        }

        public Task Run()
        {
            var image = appConfig.Name;
            var currentTag = appConfig.GetCurrentTag();
            var deploymentFile = Path.GetFullPath(appConfig.DeploymentFile ?? throw new InvalidOperationException("You must provide a deployment file to use deploy."));

            //Get the tags from docker
            var args = $"inspect --format=\"{{{{json .RepoTags}}}}\" {image}:{currentTag}";
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

            logger.LogInformation($"Redeploying '{image}' with tag '{latestDateTag}'.");

            //Ensure app data path exists (will need to handle permissions here too eventually).
            var appdataPath = appConfig.GetAppDataPath();
            if (!Directory.Exists(appdataPath))
            {
                Directory.CreateDirectory(appdataPath);
            }

            //Hack up windows path
            appdataPath = "/" + appdataPath.Replace("\\", "/").Remove(1, 1);

            //Create deployment yaml from source file
            var parameters = new Dictionary<String, Object>()
            {
                { "name", appConfig.Name },
                { "image", latestDateTag },
                { "appdataPath", appdataPath },
                { "user", appConfig.User },
                { "group", appConfig.Group }
            };
            var inputYaml = File.ReadAllText(deploymentFile);
            var outputYaml = tokenReplacer.ReplaceTokens(inputYaml, parameters);
            var outputPath = Path.Combine(Path.GetDirectoryName(deploymentFile), $"Out-{Guid.NewGuid()}.yaml");
            File.WriteAllText(outputPath, outputYaml);

            //Apply with kubectl
            processRunner.RunProcessWithOutput(new ProcessStartInfo("kubectl", $"apply -f \"{outputPath}\""));

            File.Delete(outputPath);

            return Task.CompletedTask;
        }
    }
}
