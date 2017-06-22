using Pinta.AutoScaling.Interfaces;
using Pinta.AutoScaling.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Pinta.AutoScaler
{
    public delegate void TraceEventHandler(object sender, string message);


    public class ScaleSetScaler
    {
        public event TraceEventHandler TraceEvent;

        private ILoadWatcher _loadWatcher;
        private string _clientId, _clientSecret, _tenantId, _subscriptionId;

        private List<string> _scalesetList;

        public ScaleSetScaler(string clientId, string clientSecret, string tenantId, string subscriptionId)
        {
            _clientId = clientId;
            _clientSecret = clientSecret;
            _tenantId = tenantId;
            _subscriptionId = subscriptionId;
            _scalesetList = new List<string>();
        }


        public void SetLoadWatcher(ILoadWatcher loadWatcher)
        {
            _loadWatcher = loadWatcher;
        }

        public void AddScaleset(string resourceGroup, string scalesetName)
        {
            _scalesetList.Add($"{resourceGroup},{scalesetName}");
        }


        private async Task AutoScale(string resourceGroup, string vmssName)
        {
            try
            {
                VMScaleSet vmss;
                int minRequiredNodes=0, maxRequiredNodes=0, sumRequiredNodes=0;
                int maxAvailableVMs, stoppedAvailableVMs, freeRunningVMs;
                int jobQueued;
                int busyNodes;
                int nodesToStart = 0;

                vmss = new VMScaleSet(_tenantId, _clientId, _clientSecret, _subscriptionId, resourceGroup, vmssName);
                vmss.TraceEvent += this.TraceEvent;

                // sample load watcher for current queue lenght
                OnTraceEvent("Querying load watcher");
                var load = _loadWatcher.GetVMLoad(vmssName);

                busyNodes = load.Nodes.Count(r => r.JobStatus == VMJobStatusEnum.Busy);
                jobQueued = load.Jobs.Count(r => r.Status == JobStatusEnum.Queued);

                OnTraceEvent($"Busy nodes is {busyNodes:N2}");
                OnTraceEvent($"Job queued is {jobQueued:N2}");

                OnTraceEvent($"Getting currect vmss vm states...");
                var vmStatusList = await vmss.GetVMSSVMStatesAsync(load.Nodes);
                stoppedAvailableVMs = vmStatusList.Count(r => r.Status == VMInstanceStatusEnum.Stopped);
                freeRunningVMs = vmStatusList.Count(r => r.Status == VMInstanceStatusEnum.Running && r.JobStatus == VMJobStatusEnum.Free);
                OnTraceEvent($"Stopped Available VMs is {stoppedAvailableVMs}");
                OnTraceEvent($"Free Running VM is {freeRunningVMs}");

                maxAvailableVMs = stoppedAvailableVMs + freeRunningVMs;

                // If jobs are queued, check how many nodes we need to start
                if (jobQueued > 0)
                {
                    minRequiredNodes = load.Jobs.Where(r => r.Status == JobStatusEnum.Queued).Min(r => r.Nodes);
                    sumRequiredNodes = load.Jobs.Where(r => r.Status == JobStatusEnum.Queued).Sum(s => s.Nodes);
                    maxRequiredNodes = load.Jobs.Where(r => r.Status == JobStatusEnum.Queued).Max(s => s.Nodes);

                    OnTraceEvent($"Min, Max, Sum nodes required is {minRequiredNodes},{maxRequiredNodes},{sumRequiredNodes}");

                    if (sumRequiredNodes <= maxAvailableVMs)
                        nodesToStart = sumRequiredNodes - freeRunningVMs;
                    else if (maxRequiredNodes <= maxAvailableVMs)
                        nodesToStart = maxRequiredNodes - freeRunningVMs;
                    else if (minRequiredNodes <= maxAvailableVMs)
                        nodesToStart = minRequiredNodes - freeRunningVMs;
                    OnTraceEvent($"There are {nodesToStart} nodes that needs to be started");
                }

                if (nodesToStart > 0)
                    await vmss.StartInstanceAsync(vmStatusList, nodesToStart, vmStatusList.Count());
                else
                {
                    // retrieve the vm list with no jobs running
                    var stoppableVMs = vmStatusList.Where(r => r.JobStatus != VMJobStatusEnum.Busy && r.Status == VMInstanceStatusEnum.Running);
                    int nodesToStop = stoppableVMs.Count();
                    if (nodesToStop > 0)
                    {
                        OnTraceEvent($"There are {nodesToStop} nodes that needs to be stopped");
                        await vmss.StopInstanceAsync(vmStatusList.Where(r => r.JobStatus != VMJobStatusEnum.Busy), nodesToStop, vmStatusList.Count());
                    }
                }

            }
            catch (Exception ex)
            {
                OnTraceEvent(ex.ToString());
            }
        }

        public async Task AutoScale()
        {
            foreach (var item in _scalesetList)
            {
                var words = item.Split(',');
                string rg = words[0];
                string vmssName = words[1];
                OnTraceEvent($"Autoscaling scaleset {vmssName} in resource group {rg}");
                await AutoScale(rg, vmssName);
            }
        }


        private void OnTraceEvent(string message)
        {
            if (this.TraceEvent != null)
            {
                this.TraceEvent(this, message);
            }
        }

    }
}
