using System;
using System.Collections.Generic;
using System.Text;

namespace k8s.Models
{

    public class V1BetaIngress
    {
        public string apiVersion { get; set; }
        public string kind { get; set; }
        public V1BetaIngressMetadata metadata { get; set; }
        public V1BetaIngressSpec spec { get; set; }
    }

    public class V1BetaIngressMetadata
    {
        public string name { get; set; }
    }

    public class V1BetaIngressSpec
    {
        public List<V1BetaIngressRule> rules { get; set; }
    }

    public class V1BetaIngressRule
    {
        public string host { get; set; }
        public V1BetaIngressHttp http { get; set; }
    }

    public class V1BetaIngressHttp
    {
        public List<V1BetaIngressPath> paths { get; set; }
    }

    public class V1BetaIngressPath
    {
        public V1BetaIngressBackend backend { get; set; }
    }

    public class V1BetaIngressBackend
    {
        public string serviceName { get; set; }
        public int servicePort { get; set; }
    }
}
