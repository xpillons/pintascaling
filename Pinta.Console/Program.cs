using Pinta.AutoScaler;
using Pinta.AutoScaling.Interfaces;
using Pinta.AutoScaling.LoadWatchers;
using Pinta.AutoScaling.Models;
using Pinta.DAL;
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pinta
{
    class Program
    {
        private static string _endpoint = "https://yourbase.documents.azure.com:443/";
        private static string _authKey = "yourkey";
        private static string _database = "Pinta";

        static void Main(string[] args)
        {
            RunAutoscaler();
        }

        static void RunAutoscaler()
        {
            ILoadWatcher loadWatcher = new DocumentDBWatcher(_endpoint, _authKey, _database);
            var pools = loadWatcher.GetPools();
            if (pools != null)
            {
                ScaleSetScaler autoScaler = new ScaleSetScaler(
                                                    pools.ClientId,
                                                    pools.ClientSecret,
                                                    pools.TenantId,
                                                    pools.SubscriptionId);
                foreach (var pool in pools.RgVmssName)
                {
                    var words = pool.Split(',');
                    autoScaler.AddScaleset(words[0], words[1]);
                }

                autoScaler.SetLoadWatcher(loadWatcher);
                autoScaler.TraceEvent += AutoScaler_TraceEvent;

                while (!Console.KeyAvailable)
                {
                    Task t = autoScaler.AutoScale();
                    t.Wait();

                    double minutes = 5;
                    Trace.WriteLine($"Pausing for {minutes} minutes");
                    Task.Delay((int)(60 * 1000 * minutes)).Wait();
                }
            }
        }
        private static void AutoScaler_TraceEvent(object sender, string message)
        {
            Console.WriteLine(message);
        }

        static void CreateClusterConfig()
        {
            var repo = new ClusterLoadRepository(_endpoint, _authKey, _database);
            repo.InitializeAsync().Wait();

            var config = new ConfigPool();
            config.SubscriptionId = "";
            config.ClientId = "";
            config.ClientSecret = "";
            config.TenantId = "";
            config.RgVmssName.Add("pinta-nodes,pinta00");
            config.RgVmssName.Add("pinta-nodes,a900");

            repo.CreateConfigPoolAsync(config).Wait();
        }
        static void CreateClusterLoadData(string poolName)
        {
            var repo = new ClusterLoadRepository(_endpoint, _authKey, _database);
            repo.InitializeAsync().Wait();
            DeleteJobs(repo, poolName);
            DeleteNodes(repo, poolName);
            CreateJobs(repo, poolName);
            CreateNodes(repo, poolName);
        }

        static void DeleteJobs(ClusterLoadRepository repo, string poolName)
        {
            Task<IEnumerable<JobInstance>> tJobs;

            tJobs = repo.GetJobInstancesAsync(poolName);
            var jobs = tJobs.Result;

            // clean up 
            foreach (var item in jobs)
            {
                repo.DeleteJobInstanceAsync(item.id).Wait();
            }
        }

        static void DeleteNodes(ClusterLoadRepository repo, string poolName)
        {
            Task<IEnumerable<VMInstance>> tVms;

            tVms = repo.GetVMInstancesAsync(poolName);
            var vms = tVms.Result;

            // clean up 
            foreach (var item in vms)
            {
                repo.DeleteVMInstanceAsync(item.id).Wait();
            }
        }

        static void CreateJobs(ClusterLoadRepository repo, string poolName)
        {
            Task<IEnumerable<JobInstance>> tJobs;

            var J1 = new JobInstance() { id = Guid.NewGuid().ToString(), Nodes = 4, QueueName = poolName, SchedulerId = "124", Status = AutoScaling.Models.JobStatusEnum.Queued };
            repo.CreateJobInstanceAsync(J1).Wait();
            var J2 = new JobInstance() { id = Guid.NewGuid().ToString(), Nodes = 4, QueueName = poolName, SchedulerId = "127", Status = AutoScaling.Models.JobStatusEnum.Queued };
            repo.CreateJobInstanceAsync(J2).Wait();
            var J3 = new JobInstance() { id = Guid.NewGuid().ToString(), Nodes = 8, QueueName = poolName, SchedulerId = "130", Status = AutoScaling.Models.JobStatusEnum.Queued };
            repo.CreateJobInstanceAsync(J3).Wait();

            tJobs = repo.GetJobInstancesAsync(poolName);
            var jobs = tJobs.Result;

            foreach (var item in jobs)
            {
                Console.WriteLine($"{item.id} {item.Status}");
            }
        }

        static void CreateNodes(ClusterLoadRepository repo, string poolName)
        {
            Task<IEnumerable<VMInstance>> tVms;

            var vm1 = new VMInstance() { id = Guid.NewGuid().ToString(), Name = "pinta00s2000000", PoolName = poolName, Status = VMInstanceStatusEnum.Running, JobStatus = VMJobStatusEnum.Free };
            repo.CreateVMInstanceAsync(vm1).Wait();
            var vm2 = new VMInstance() { id = Guid.NewGuid().ToString(), Name = "pinta00s2000001", PoolName = poolName, Status = VMInstanceStatusEnum.Running, JobStatus = VMJobStatusEnum.Free };
            repo.CreateVMInstanceAsync(vm2).Wait();
            var vm3 = new VMInstance() { id = Guid.NewGuid().ToString(), Name = "pinta00s2000002", PoolName = poolName, Status = VMInstanceStatusEnum.Running, JobStatus = VMJobStatusEnum.Free };
            repo.CreateVMInstanceAsync(vm3).Wait();
            var vm4 = new VMInstance() { id = Guid.NewGuid().ToString(), Name = "pinta00s2000004", PoolName = poolName, Status = VMInstanceStatusEnum.Running, JobStatus = VMJobStatusEnum.Free };
            repo.CreateVMInstanceAsync(vm4).Wait();

            tVms = repo.GetVMInstancesAsync(poolName);
            var vms = tVms.Result;

            foreach (var item in vms)
            {
                Console.WriteLine($"{item.id} {item.Status}");
            }
        }
    }
}
