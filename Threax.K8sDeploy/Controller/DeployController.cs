using k8s.Models;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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

            if (latestDateTag == null)
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
                { "host", $"{appConfig.Name}.{appConfig.Domain}" },
                { "image", latestDateTag },
                { "appdataPath", appdataPath },
                { "user", appConfig.User },
                { "group", appConfig.Group },
                { "initCommand", appConfig.InitCommand }
            };

            //if (!File.Exists(deploymentFile))
            //{
            //    var builtInFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), deploymentFile);

            //    if (!File.Exists(builtInFile))
            //    {
            //        throw new FileNotFoundException($"Cannot find {deploymentFile} searched '{Path.GetFullPath(deploymentFile)}' and '{builtInFile}'");
            //    }
            //}

            //var inputYaml = File.ReadAllText(deploymentFile);
            //var outputYaml = tokenReplacer.ReplaceTokens(inputYaml, parameters);
            //File.WriteAllText(outputPath, outputYaml);

            var outputPath = Path.GetDirectoryName(deploymentFile);
            ApplyObjects(outputPath, new List<Object>()
            {
                CreateDeployment(appConfig.Name, latestDateTag, appdataPath, appConfig.User, appConfig.Group),
                CreateService(appConfig.Name),
                CreateIngress(appConfig.Name, $"{appConfig.Name}.{appConfig.Domain}")
            });

            return Task.CompletedTask;
        }

        private void ApplyObjects(String outputPath, IEnumerable<Object> data)
        {
            var serializer = Newtonsoft.Json.JsonSerializer.Create(new Newtonsoft.Json.JsonSerializerSettings()
            {
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
            });

            foreach (var item in data)
            {
                var outputFile = Path.Combine(outputPath, $"apply-{Guid.NewGuid()}.json");
                using (var writer = new StreamWriter(File.Open(outputFile, FileMode.Create, FileAccess.ReadWrite, FileShare.None)))
                {
                    serializer.Serialize(writer, item, item.GetType());
                    writer.Flush();
                }

                processRunner.RunProcessWithOutput(new ProcessStartInfo("kubectl", $"apply -f \"{outputFile}\""));

                File.Delete(outputFile);
            }
        }

        private V1BetaIngress CreateIngress(String name, String host)
        {
            return new V1BetaIngress()
            {
                apiVersion = "networking.k8s.io/v1beta1",
                kind = "Ingress",
                metadata = new V1BetaIngressMetadata() {
                    name = name,
                },
                spec = new V1BetaIngressSpec() {
                    tls = new List<V1BetaIngressTls>() {
                        new V1BetaIngressTls() {
                            hosts = new List<string>(){ host },
                            secretName = $"{appConfig.Domain}-tls"
                        }
                    },
                    rules = new List<V1BetaIngressRule>() {
                        new V1BetaIngressRule() {
                            host = host,
                            http = new V1BetaIngressHttp() {
                                paths = new List<V1BetaIngressPath>() {
                                    new V1BetaIngressPath() {
                                        backend = new V1BetaIngressBackend() {
                                            serviceName = name,
                                            servicePort = 80
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }

        private V1Service CreateService(String name)
        {
            return new V1Service()
            {
                ApiVersion = "v1",
                Kind = "Service",
                Metadata = new V1ObjectMeta() {
                    Name = name
                },
                Spec = new V1ServiceSpec() {
                    Selector = new Dictionary<String, String>() {
                        { "app", name }
                    },
                    Ports = new List<V1ServicePort>() {
                        new V1ServicePort() {
                            Port = 80,
                            TargetPort = 5000
                        }
                    }
                }
            };
        }

        private V1Deployment CreateDeployment(String name, String image, String appdataPath, long? user, long? group)
        {
            //Try to build an object
            return new V1Deployment()
            {
                ApiVersion = "apps/v1",
                Kind = "Deployment",
                Metadata = new V1ObjectMeta()
                {
                    Name = name
                },
                Spec = new V1DeploymentSpec()
                {
                    Replicas = 1,
                    Selector = new V1LabelSelector()
                    {
                        MatchLabels = new Dictionary<String, String>() {
                            { "app", name }
                        }
                    },
                    Template = new V1PodTemplateSpec()
                    {
                        Metadata = new V1ObjectMeta()
                        {
                            Labels = new Dictionary<String, String>() {
                                { "app", name }
                            }
                        },
                        Spec = new V1PodSpec()
                        {
                            Containers = new List<V1Container>() {
                                new V1Container() {
                                    Name = "app",
                                    Image = image,
                                    Env = new List<V1EnvVar>() {
                                        new V1EnvVar() {
                                            Name = "ASPNETCORE_URLS",
                                            Value = "http://*:5000"
                                        }
                                    },
                                    ImagePullPolicy = "IfNotPresent",
                                    Ports = new List<V1ContainerPort>() {
                                        new V1ContainerPort() {
                                            ContainerPort = 5000
                                        }
                                    },
                                    VolumeMounts = new List<V1VolumeMount>() {
                                        new V1VolumeMount() {
                                            MountPath = "/appdata",
                                            Name = "appdata-volume"
                                        }
                                    }
                                }
                            },
                            InitContainers = new List<V1Container>() {
                                new V1Container() {
                                    Name = "app-init",
                                    Command = new List<String>() { "sh", "-c", appConfig.InitCommand },
                                    Image = image,
                                    Env = new List<V1EnvVar>() {
                                        new V1EnvVar() {
                                            Name = "ASPNETCORE_URLS",
                                            Value = "http://*:5000"
                                        }
                                    },
                                    ImagePullPolicy = "IfNotPresent",
                                    Ports = new List<V1ContainerPort>() {
                                        new V1ContainerPort() {
                                            ContainerPort = 5000
                                        }
                                    },
                                    VolumeMounts = new List<V1VolumeMount>() {
                                        new V1VolumeMount() {
                                            MountPath = "/appdata",
                                            Name = "appdata-volume"
                                        }
                                    },
                                }
                            },
                            Volumes = new List<V1Volume>() {
                                new V1Volume() {
                                    Name = "appdata-volume",
                                    HostPath = new V1HostPathVolumeSource() {
                                        Path = appdataPath,
                                        Type = "Directory"
                                    }
                                }
                            },
                            SecurityContext = new V1PodSecurityContext()
                            {
                                RunAsUser = user,
                                RunAsGroup = group,
                                RunAsNonRoot = true
                            }
                        }
                    },
                },
            };
        }
    }
}
