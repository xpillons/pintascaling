using System;
using System.Configuration;
using Pinta.AutoScaler;
using Pinta.AutoScaling.Interfaces;
using Pinta.AutoScaling.LoadWatchers;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs;

namespace Pinta.AutoScaling
{
    public class ScalerFunction
    {
        public static TraceWriter Log;

        public static void Run(TimerInfo myTimer, TraceWriter log)
        {
            Log = log;
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");

            ILoadWatcher loadWatcher = new DocumentDBWatcher(ConfigurationManager.AppSettings["endpoint"],
                                                             ConfigurationManager.AppSettings["authKey"],
                                                             ConfigurationManager.AppSettings["database"]);

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
                    log.Info($"Adding {pool}");
                    var words = pool.Split(',');
                    autoScaler.AddScaleset(words[0], words[1]);
                }

                autoScaler.SetLoadWatcher(loadWatcher);
                autoScaler.TraceEvent += AutoScaler_TraceEvent;

                autoScaler.AutoScale().Wait();
            }
            else
            {
                log.Info($"No pool configured");
            }
        }

        private static void AutoScaler_TraceEvent(object sender, string message)
        {
            Log.Info(message);
        }
    }
}