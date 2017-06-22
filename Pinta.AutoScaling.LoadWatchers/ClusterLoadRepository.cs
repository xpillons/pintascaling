using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Pinta.AutoScaling.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pinta.DAL
{
    public class ClusterLoadRepository : DocumentDBRepositoryBase
    {
        private static string _collectionId = "ClusterLoad";

        public ClusterLoadRepository(string endpoint, string authKey, string databaseId):
            base(endpoint, authKey, databaseId)
        {            
        }

        public async Task InitializeAsync()
        {
            await Initialize(_collectionId);
        }

        public async Task<IEnumerable<VMInstance>> GetVMInstancesAsync(string poolName)
        {
            IDocumentQuery<VMInstance> query = _client.CreateDocumentQuery<VMInstance>(
                UriFactory.CreateDocumentCollectionUri(_databaseId, _collectionId))
                .Where(r => r.Type == VMInstance.TYPE && r.PoolName == poolName)
                .AsDocumentQuery();

            List<VMInstance> results = new List<VMInstance>();
            while (query.HasMoreResults)
            {
                results.AddRange(await query.ExecuteNextAsync<VMInstance>());
            }

            return results;
        }

        public async Task<IEnumerable<JobInstance>> GetJobInstancesAsync(string poolName)
        {
            IDocumentQuery<JobInstance> query = _client.CreateDocumentQuery<JobInstance>(
                UriFactory.CreateDocumentCollectionUri(_databaseId, _collectionId))
                .Where( r => r.Type==JobInstance.TYPE && r.QueueName==poolName)
                .AsDocumentQuery();

            List<JobInstance> results = new List<JobInstance>();
            while (query.HasMoreResults)
            {
                results.AddRange(await query.ExecuteNextAsync<JobInstance>());
            }

            return results;
        }

        public async Task<ConfigPool> GetConfigPoolAsync()
        {
            IDocumentQuery<ConfigPool> query = _client.CreateDocumentQuery<ConfigPool>(
                UriFactory.CreateDocumentCollectionUri(_databaseId, _collectionId))
                .Where(r => r.Type == ConfigPool.TYPE)
                .AsDocumentQuery();

            List<ConfigPool> results = new List<ConfigPool>();
            while (query.HasMoreResults)
            {
                results.AddRange(await query.ExecuteNextAsync<ConfigPool>());
            }

            return results[0];
        }

        #region JobInstance CRUD operations
        public async Task CreateJobInstanceAsync(JobInstance j)
        {
            var document = await _client.UpsertDocumentAsync(UriFactory.CreateDocumentCollectionUri(_databaseId, _collectionId), j);
        }

        public async Task ReplaceJobInstanceAsync(JobInstance j)
        {
            await _client.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(_databaseId, _collectionId, j.id), j);
        }

        public async Task DeleteJobInstanceAsync(string id)
        {
            await _client.DeleteDocumentAsync(UriFactory.CreateDocumentUri(_databaseId, _collectionId, id));
        }
        #endregion

        #region VMInstance CRUD operations
        public async Task CreateVMInstanceAsync(VMInstance v)
        {
            var document = await _client.UpsertDocumentAsync(UriFactory.CreateDocumentCollectionUri(_databaseId, _collectionId), v);
        }

        public async Task ReplaceVMInstanceAsync(VMInstance v)
        {
            await _client.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(_databaseId, _collectionId, v.id), v);
        }

        public async Task DeleteVMInstanceAsync(string id)
        {
            await _client.DeleteDocumentAsync(UriFactory.CreateDocumentUri(_databaseId, _collectionId, id));
        }
        #endregion

        #region ConfigPool CRUD operations
        public async Task CreateConfigPoolAsync(ConfigPool item)
        {
            var document = await _client.UpsertDocumentAsync(UriFactory.CreateDocumentCollectionUri(_databaseId, _collectionId), item);
        }

        public async Task ReplaceConfigPoolAsync(ConfigPool item)
        {
            await _client.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(_databaseId, _collectionId, item.id), item);
        }

        public async Task DeleteConfigPoolAsync(string id)
        {
            await _client.DeleteDocumentAsync(UriFactory.CreateDocumentUri(_databaseId, _collectionId, id));
        }
        #endregion

    }
}
