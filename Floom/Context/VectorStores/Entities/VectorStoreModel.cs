using Floom.Entities;
using Floom.Entities.VectorStore;
using MongoDB.Bson;

namespace Floom.VectorStores;

public class VectorStoreModel : BaseModel
{
    public VectorStoreVendor.Enum Vendor { get; set; }
    public VectorStoreConnectionArgs ConnectionArgs { get; set; }

    private string GenerateId()
    {
        return (Vendor + "-" + ConnectionArgs.Endpoint + "-" + ConnectionArgs.Username).ToLower();
    }

    public VectorStoreEntity ToEntity()
    {
        return new VectorStoreEntity()
        {
            Id = Id != null ? ObjectId.Parse(Id) : ObjectId.Empty,
            name = name,
            vendor = Vendor,
            endpoint = ConnectionArgs.Endpoint,
            environment = ConnectionArgs.Environment,
            apiKey = ConnectionArgs.ApiKey,
            port = ConnectionArgs.Port,
        };
    }

    public static VectorStoreModel? GetEnvVarVectorStoreConfiguration()
    {
        var vectorStoreModel = new VectorStoreModel();

        var vdb_vendor = Environment.GetEnvironmentVariable("VDB_VENDOR") ?? string.Empty;
        var vdb_apikey = Environment.GetEnvironmentVariable("VDB_APIKEY") ?? string.Empty;
        var vdb_environment = Environment.GetEnvironmentVariable("VDB_ENVIRONMENT") ?? string.Empty;
        var vdb_endpoint = Environment.GetEnvironmentVariable("VDB_ENDPOINT") ?? string.Empty;
        var vdb_port = Environment.GetEnvironmentVariable("VDB_PORT") ?? string.Empty;

        if (string.IsNullOrEmpty(vdb_vendor))
        {
            return null;
        }

        vectorStoreModel = new VectorStoreModel()
        {
            Vendor = VectorStoreVendor.FromString(vdb_vendor),
            ConnectionArgs = new VectorStoreConnectionArgs(vdb_endpoint, vdb_environment, vdb_apikey)
        };

        vectorStoreModel.name = vectorStoreModel.GenerateId();

        if (vdb_port != string.Empty)
        {
            vectorStoreModel.ConnectionArgs.Port = (ushort)Int32.Parse(vdb_port);
        }

        return vectorStoreModel;
    }

    public static VectorStoreModel FromEntity(VectorStoreEntity vectorStoreEntity)
    {
        var vectorStoreModel = new VectorStoreModel()
        {
            Id = vectorStoreEntity.Id == ObjectId.Empty ? null : vectorStoreEntity.Id.ToString(),
            name = vectorStoreEntity.name,
            Vendor = vectorStoreEntity.vendor,
            ConnectionArgs = new VectorStoreConnectionArgs()
            {
                Endpoint = vectorStoreEntity.endpoint,
                Environment = vectorStoreEntity.environment,
                ApiKey = vectorStoreEntity.apiKey,
                Port = (ushort)vectorStoreEntity.port,
            }
        };
        return vectorStoreModel;
    }

    public static VectorStoreModel FromDto(VectorStoreDtoV1 vectorStoreDto)
    {
        var vectorStoreModel = new VectorStoreModel()
        {
            Vendor = vectorStoreDto.vendor,
            ConnectionArgs = new VectorStoreConnectionArgs()
            {
                Endpoint = vectorStoreDto.endpoint,
                Environment = vectorStoreDto.environment,
                ApiKey = vectorStoreDto.apiKey,
                Port = (ushort)vectorStoreDto.port,
            }
        };
        return vectorStoreModel;
    }

    public static VectorStoreModel GetInternalVectorStoreConfiguration()
    {
        var vectorStoreModel = new VectorStoreModel()
        {
            Vendor = VectorStoreVendor.Enum.Milvus,
            ConnectionArgs = new VectorStoreConnectionArgs()
            {
                Endpoint = "127.0.0.1",
                Port = 19530,
                Username = "root",
                Password = "Milvus"
            }
        };
        vectorStoreModel.name = vectorStoreModel.GenerateId();
        return vectorStoreModel;
    }

    public class VectorStoreConnectionArgs
    {
        public string? Endpoint;
        public ushort Port;
        public string? Username;
        public string? Password;
        public string? ApiKey;
        public string? Environment;

        public VectorStoreConnectionArgs()
        {
        }

        public VectorStoreConnectionArgs(string? endpoint, string? environment, string? apiKey)
        {
            Endpoint = endpoint;
            Environment = environment;
            ApiKey = apiKey;
        }

        public VectorStoreConnectionArgs(string? endpoint, string? username, string? password, string? port = "19530")
        {
            Endpoint = endpoint;
            Port = Convert.ToUInt16(port);
            Username = username;
            Password = password;
        }

        public VectorStoreConnectionArgs(string? endpoint, string? username, string? password, string? environment,
            string? port = "19530")
        {
            Endpoint = endpoint;
            Port = Convert.ToUInt16(port);
            Username = username;
            Password = password;
            Environment = environment;
        }
    }
}