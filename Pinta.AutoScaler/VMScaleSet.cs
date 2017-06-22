using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pinta.AutoScaling.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Pinta.AutoScaler
{
    public class VMScaleSet
    {
        public event TraceEventHandler TraceEvent;

        private string _tenantId;
        private string _clientId;
        private string _clientSecret;
        private string _subscriptionId;
        private string _resourceGroup;
        private string _vmssName;
        private const string _azureArmApiBaseUrl = "https://management.azure.com/";
        private const string _vmssApiVersion = "2016-03-30";

        public VMScaleSet(string tenantId, string clientId, string clientSecret, string subscriptionId, string resourceGroup, string vssName)
        {
            _tenantId = tenantId;
            _clientId = clientId;
            _clientSecret = clientSecret;
            _subscriptionId = subscriptionId;
            _resourceGroup = resourceGroup;
            _vmssName = vssName;
        }

        private async Task<AuthenticationResult> GetAuthorizationHeaderAsync(string tenantId, string clientId, string clientSecret)
        {
            var context = new AuthenticationContext("https://login.windows.net/" + tenantId);

            var creds = new ClientCredential(clientId, clientSecret);

            return await context.AcquireTokenAsync("https://management.core.windows.net/", creds);
        }

        public async Task<string> SetVMSSInstanceAsync(string[] instanceIds, ScaleDirection scaleDirection)
        {
            try
            {
                AuthenticationResult authenticationResult = await GetAuthorizationHeaderAsync(_tenantId, _clientId, _clientSecret);
                var token = authenticationResult.CreateAuthorizationHeader();

                using (HttpClient client = new HttpClient())
                {
                    client.BaseAddress = new Uri(_azureArmApiBaseUrl);
                    client.DefaultRequestHeaders.Add("Authorization", token);
                    client.DefaultRequestHeaders
                          .Accept
                          .Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    string vmssVerb = (scaleDirection == ScaleDirection.Out ? ScalesetAction.start.ToString("G") : ScalesetAction.deallocate.ToString("G"));

                    var url = $"subscriptions/{_subscriptionId}/resourceGroups/{_resourceGroup}/providers/Microsoft.Compute/virtualMachineScaleSets/{_vmssName}/{vmssVerb}?api-version={_vmssApiVersion}";
                    var payload = $"{{instanceIds:{JsonConvert.SerializeObject(instanceIds)}}}";
                    HttpRequestMessage message = new HttpRequestMessage();
                    message.Content = new StringContent(payload, Encoding.UTF8, "application/json");

                    using (HttpResponseMessage response = await client.PostAsync(url, message.Content))
                    using (HttpContent content = response.Content)
                    {
                        if (response.StatusCode != System.Net.HttpStatusCode.OK)
                        {
                            OnTraceEvent(response.StatusCode.ToString());
                        }
                        return await content.ReadAsStringAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                OnTraceEvent(ex.ToString());
                return await Task.FromResult<string>(ex.ToString());
            }
        }

        public async Task<string> SetVMSSCapacityAsync(dynamic sku)
        {
            try
            {
                AuthenticationResult authenticationResult = await GetAuthorizationHeaderAsync(_tenantId, _clientId, _clientSecret);
                var token = authenticationResult.CreateAuthorizationHeader();

                using (HttpClient client = new HttpClient())
                {
                    client.BaseAddress = new Uri(_azureArmApiBaseUrl);
                    client.DefaultRequestHeaders.Add("Authorization", token);
                    client.DefaultRequestHeaders
                          .Accept
                          .Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var url = $"subscriptions/{_subscriptionId}/resourceGroups/{_resourceGroup}/providers/Microsoft.Compute/virtualMachineScaleSets/{_vmssName}?api-version={_vmssApiVersion}";
                    var payload = $"{{sku:{JsonConvert.SerializeObject(sku)}}}";
                    HttpRequestMessage message = new HttpRequestMessage(new HttpMethod("PATCH"), url);

                    message.Content = new StringContent(payload, Encoding.UTF8, "application/json");

                    using (HttpResponseMessage response = await client.SendAsync(message))
                    using (HttpContent content = response.Content)
                    {
                        if (response.StatusCode != System.Net.HttpStatusCode.OK)
                        {
                            OnTraceEvent(response.StatusCode.ToString());
                        }
                        return await content.ReadAsStringAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                OnTraceEvent(ex.ToString());
                return await Task.FromResult<string>(ex.ToString());
            }
        }

        public async Task<IEnumerable<VMInstance>> GetVMSSVMStatesAsync(IEnumerable<VMInstance> vmJobStatusList)
        {
            var vmList = new List<VMInstance>();

            try
            {
                // VMs list
                var vmListUrl = $"subscriptions/{_subscriptionId}/resourceGroups/{_resourceGroup}/providers/Microsoft.Compute/virtualMachineScaleSets/{_vmssName}/virtualMachines?api-version={_vmssApiVersion}";
                dynamic dynVmList = await GetResponse(vmListUrl);

                if (dynVmList.value != null)
                {
                    foreach (var item in dynVmList.value)
                    {
                        var vm = new VMInstance();
                        vm.ScaleSetId = item.instanceId;
                        vm.Name = item.properties.osProfile.computerName;
                        vm.PoolName = _vmssName;
                        var vmJobStatus = vmJobStatusList.FirstOrDefault(r => r.Name == vm.Name);
                        if (vmJobStatus != null)
                            vm.JobStatus = vmJobStatus.JobStatus;
                        else
                            vm.JobStatus = VMJobStatusEnum.Unknown;

                        vmList.Add(vm);
                    }
                }
                // VMs status
                var vmStatusUrl = $"subscriptions/{_subscriptionId}/resourceGroups/{_resourceGroup}/providers/Microsoft.Compute/virtualMachineScaleSets/{_vmssName}/virtualMachines?$expand=instanceView&$select=instanceView&api-version={_vmssApiVersion}";

                dynamic dynStatus = await GetResponse(vmStatusUrl);
                if (dynStatus.value != null)
                {
                    foreach (var item in dynStatus.value)
                    {
                        string instanceId = item.instanceId;
                        var vm = vmList.FirstOrDefault(v => v.ScaleSetId == instanceId);

                        foreach (var status in item.properties.instanceView.statuses)
                        {
                            string statusCode = status.code;
                            if (statusCode.StartsWith("PowerState"))
                            {
                                string displayStatus = status.displayStatus;
                                if (displayStatus.Contains("running"))
                                    vm.Status = VMInstanceStatusEnum.Running;
                                else if (displayStatus.Contains("deallocated"))
                                    vm.Status = VMInstanceStatusEnum.Stopped;
                            }
                        }
                    }
                }
                return vmList;
            }
            catch (Exception ex)
            {
                OnTraceEvent(ex.ToString());
                return vmList;
            }

        }

        private async Task<dynamic> GetResponse(string url)
        {
            try
            {
                AuthenticationResult authenticationResult = await GetAuthorizationHeaderAsync(_tenantId, _clientId, _clientSecret);
                var token = authenticationResult.CreateAuthorizationHeader();

                using (HttpClient client = new HttpClient())
                {
                    client.BaseAddress = new Uri(_azureArmApiBaseUrl);
                    client.DefaultRequestHeaders.Add("Authorization", token);
                    client.DefaultRequestHeaders
                          .Accept
                          .Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    using (HttpResponseMessage response = await client.GetAsync(url))
                    using (HttpContent content = response.Content)
                    {
                        string json = await content.ReadAsStringAsync();
                        dynamic dyn = JObject.Parse(json);
                        return dyn;
                    }
                }
            }
            catch (Exception ex)
            {
                OnTraceEvent(ex.ToString());
                return await Task.FromResult<string>(ex.ToString());
            }
        }

        public async Task<dynamic> GetVMSSCapacityAsync()
        {
            try
            {
                AuthenticationResult authenticationResult = await GetAuthorizationHeaderAsync(_tenantId, _clientId, _clientSecret);
                var token = authenticationResult.CreateAuthorizationHeader();

                using (HttpClient client = new HttpClient())
                {
                    client.BaseAddress = new Uri(_azureArmApiBaseUrl);
                    client.DefaultRequestHeaders.Add("Authorization", token);
                    client.DefaultRequestHeaders
                          .Accept
                          .Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var url = $"subscriptions/{_subscriptionId}/resourceGroups/{_resourceGroup}/providers/Microsoft.Compute/virtualMachineScaleSets/{_vmssName}?api-version={_vmssApiVersion}";

                    using (HttpResponseMessage response = await client.GetAsync(url))
                    using (HttpContent content = response.Content)
                    {
                        string json = await content.ReadAsStringAsync();
                        dynamic dyn = JObject.Parse(json);
                        return dyn.sku;
                    }
                }
            }
            catch (Exception ex)
            {
                OnTraceEvent(ex.ToString());
                return await Task.FromResult<string>(ex.ToString());
            }
        }

        public async Task<string> StartInstanceAsync(IEnumerable<VMInstance> vms, int count, int capacity)
        {
            OnTraceEvent($"starting {count} vms");
            var instandeIds = GetInstanceIds(vms, count, capacity, VMInstanceStatusEnum.Stopped);
            return await SetVMSSInstanceAsync(instandeIds, ScaleDirection.Out);
        }

        public async Task<string> StopInstanceAsync(IEnumerable<VMInstance> vms, int count, int capacity)
        {
            OnTraceEvent($"stopping {count} vms");
            var instandeIds = GetInstanceIds(vms, count, capacity, VMInstanceStatusEnum.Running);
            return await SetVMSSInstanceAsync(instandeIds, ScaleDirection.In);
        }

        private string[] GetInstanceIds(IEnumerable<VMInstance> vms, int count, int capacity, VMInstanceStatusEnum status)
        {
            string[] instandeIds = new string[] { };

            // get the list of stopped VMs
            var vmList = vms.Where(r => r.Status == status).Take(count);
            if (vmList.Count() == capacity)
            {
                instandeIds = new string[] { "*" };
            }
            else
            {
                var ids = vmList.Select(r => r.ScaleSetId);
                instandeIds = ids.ToArray();
            }

            return instandeIds;
        }

        public async Task<string> ScaleAsync(dynamic Sku, ScaleDirection scaleDirection)
        {
            int current = Sku.capacity;

            if (scaleDirection == ScaleDirection.Out)
            {
                Sku.capacity += 1;
            }
            else
            {
                Sku.capacity -= 1;
            }

            OnTraceEvent($"setting currect vmss capacity from {current} to {JsonConvert.SerializeObject(Sku.capacity)}");
            return await SetVMSSCapacityAsync(Sku);
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
