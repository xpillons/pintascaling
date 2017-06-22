using Pinta.AutoScaling.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pinta.AutoScaling.Models;
using Pinta.DAL;

namespace Pinta.AutoScaling.LoadWatchers
{
    public class DocumentDBWatcher : ILoadWatcher
    {
        private ClusterLoadRepository _repo;
        public DocumentDBWatcher(string endpoint, string authKey, string databaseId)
        {
            _repo = new ClusterLoadRepository(endpoint, authKey, databaseId);
            _repo.InitializeAsync().Wait();
        }

        public ConfigPool GetPools()
        {
            Task<ConfigPool> tPool = _repo.GetConfigPoolAsync();
            return tPool.Result;
        }

        public ClusterLoad GetVMLoad(string poolName)
        {
            var load = new ClusterLoad();
            Task<IEnumerable<JobInstance>> tJobs;
            Task<IEnumerable<VMInstance>> tVms;

            tJobs = _repo.GetJobInstancesAsync(poolName);
            load.Jobs = tJobs.Result;

            tVms = _repo.GetVMInstancesAsync(poolName);
            load.Nodes = tVms.Result;
            
            return load;
        }
    }
}
