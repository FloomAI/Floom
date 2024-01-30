using Floom.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using MongoDB.Driver;

namespace Floom.Helpers
{
    public static class VectorStore
    {
        public static Models.VectorStore GetEnvVarVectorStore()
        {
            Models.VectorStore vectorStore = new Models.VectorStore();

            var vdb_vendor = Environment.GetEnvironmentVariable("VDB_VENDOR") ?? string.Empty;
            var vdb_apikey = Environment.GetEnvironmentVariable("VDB_APIKEY") ?? string.Empty;
            var vdb_environment = Environment.GetEnvironmentVariable("VDB_ENVIRONMENT") ?? string.Empty;
            var vdb_endpoint = Environment.GetEnvironmentVariable("VDB_ENDPOINT") ?? string.Empty;
            var vdb_port = Environment.GetEnvironmentVariable("VDB_PORT") ?? string.Empty;

            if (string.IsNullOrEmpty(vdb_vendor))
            {
                return null;
            }

            vectorStore = new Models.VectorStore()
            {
                vendor = vdb_vendor,
                apiKey = vdb_apikey,
                environment = vdb_environment,
                endpoint = vdb_endpoint,
            };

            if (vdb_port != null && vdb_port != string.Empty)
            {
                vectorStore.port = Int32.Parse(vdb_port);
            }

            return vectorStore;
        }
    }
}
