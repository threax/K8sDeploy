using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Threax.K8sDeploy.Config;
using Threax.K8sDeploy.Services;

namespace Threax.K8sDeploy.Controller
{
    class DeployController : IController
    {
        private const String Namespace = "default";

        private AppConfig appConfig;
        private ILogger logger;
        private IProcessRunner processRunner;
        private ITokenReplacer tokenReplacer;
        private readonly IKubernetes k8SClient;

        public DeployController(AppConfig appConfig, ILogger<DeployController> logger, IProcessRunner processRunner, ITokenReplacer tokenReplacer, IKubernetes k8sClient)
        {
            this.appConfig = appConfig;
            this.logger = logger;
            this.processRunner = processRunner;
            this.tokenReplacer = tokenReplacer;
            k8SClient = k8sClient;
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

            var deployment = CreateDeployment(appConfig.Name, latestDateTag, appdataPath, appConfig.User, appConfig.Group);
            var service = CreateService(appConfig.Name);
            var ingress = CreateIngress(appConfig.Name, $"{appConfig.Name}.{appConfig.Domain}");

            k8SClient.CreateOrReplaceNamespacedDeployment(deployment, Namespace);
            k8SClient.CreateOrReplaceNamespacedService(service, Namespace);
            k8SClient.CreateOrReplaceNamespacedIngress1(ingress, Namespace);

            return Task.CompletedTask;
        }

        private Networkingv1beta1Ingress CreateIngress(String name, String host)
        {
            return new Networkingv1beta1Ingress()
            {
                ApiVersion = "networking.k8s.io/v1beta1",
                Kind = "Ingress",
                Metadata = new V1ObjectMeta
                {
                    Name = name,
                },
                Spec = new Networkingv1beta1IngressSpec()
                {
                    Tls = new List<Networkingv1beta1IngressTLS>() {
                        new Networkingv1beta1IngressTLS() {
                            Hosts = new List<string>(){ host },
                            SecretName = $"{appConfig.Domain}-tls"
                        }
                    },
                    Rules = new List<Networkingv1beta1IngressRule>() {
                        new Networkingv1beta1IngressRule() {
                            Host = host,
                            Http = new Networkingv1beta1HTTPIngressRuleValue() {
                                Paths = new List<Networkingv1beta1HTTPIngressPath>() {
                                    new Networkingv1beta1HTTPIngressPath() {
                                        Backend = new Networkingv1beta1IngressBackend() {
                                            ServiceName = name,
                                            ServicePort = 80
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
                            //Make InitContainers optional
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
