using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Threax.K8sDeploy.Config;

namespace Threax.K8sDeploy.Controller
{
    class CloneController : IController
    {
        private AppConfig appConfig;
        private ILogger logger;

        public CloneController(AppConfig appConfig, ILogger<CloneController> logger)
        {
            this.appConfig = appConfig;
            this.logger = logger;
        }

        public Task Run()
        {
            var clonePath = appConfig.GetSourcePath();

            CloneGitRepo(appConfig.RepoUrl, clonePath);

            return Task.CompletedTask;
        }

        private void CloneGitRepo(string repo, string clonePath, string repoUser = null, string repoPass = null)
        {
            if (Directory.Exists(clonePath))
            {
                logger.LogInformation($"Pulling changes to {clonePath}");
                var path = Path.Combine(clonePath, ".git");
                using (var gitRepository = new Repository(path))
                {
                    var signature = new Signature("bot", "bot@bot", DateTime.Now);
                    var result = Commands.Pull(gitRepository, signature, new PullOptions()
                    {
                        FetchOptions = new FetchOptions()
                        {
                            CredentialsProvider = (u, user, cred) => new UsernamePasswordCredentials()
                            {
                                Username = repoUser,
                                Password = repoPass
                            }
                        }
                    });
                }
            }
            else
            {
                logger.LogInformation($"Cloning {repo} to {clonePath}");
                Repository.Clone(repo, clonePath, new CloneOptions()
                {
                    CredentialsProvider = (u, user, cred) => new UsernamePasswordCredentials()
                    {
                        Username = repoUser,
                        Password = repoPass
                    }
                });
            }
        }
    }
}
