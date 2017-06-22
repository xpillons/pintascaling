using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pinta.AutoScaling.Models
{
    public class ClusterLoad
    {
        public IEnumerable<VMInstance> Nodes;
        public IEnumerable<JobInstance> Jobs;
    }
    public class VMInstance
    {
        public static string TYPE = "vms";
        public string id { get; set; }
        public string Type { get { return TYPE; } }
        public string PoolName { get; set; }
        public string Name { get; set; }
        public string ScaleSetId { get; set; }
        [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public VMInstanceStatusEnum Status { get; set; }
        [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public VMJobStatusEnum JobStatus { get; set; }
    }

    public class JobInstance
    {
        public static string TYPE = "jobs";
        public string id { get; set; }
        public string Type { get { return TYPE; } }
        public string QueueName { get; set; }
        public string SchedulerId { get; set; }
        public int Nodes { get; set; }
        [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public JobStatusEnum Status { get; set; }
    }

    public class ConfigPool
    {
        public ConfigPool()
        {
            RgVmssName = new List<string>();
        }
        public static string TYPE = "pools";
        public string id { get; set; }
        public string Type { get { return TYPE; } }
        public string SubscriptionId { get; set; }
        public string TenantId { get; set; }
        public string ClientSecret { get; set; }
        public string ClientId { get; set; }
        // list of RG/VMSS formatted RG,VMSS
        public List<string> RgVmssName { get; set; }
    }

    public enum VMInstanceStatusEnum
    {
        Stopped,
        Running,
        Transitioning
    }
    public enum VMJobStatusEnum
    {
        Unknown,
        Free,
        Busy
    }

    public enum JobStatusEnum
    {
        Unknown,
        Queued,
        Hold,
        Running
    }

    public enum ScaleDirection
    {
        Out,
        In
    }

    public enum ScalesetAction
    {
        deallocate,
        start
    }

}
