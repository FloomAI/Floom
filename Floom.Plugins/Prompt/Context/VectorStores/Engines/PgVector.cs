using Floom.Logs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Npgsql;
using Pgvector;

namespace Floom.Plugins.Prompt.Context.VectorStores.Engines;

public class PgVector : VectorStoreProvider
{
    readonly ILogger _logger;

    public PgVector()
    {
        _logger = FloomLoggerFactory.CreateLogger(GetType());
    }

    private string GetConnectionString()
    {
        var userName = ConnectionArgs.Username;
        var password = ConnectionArgs.Password;
        var address = ConnectionArgs.Endpoint;
        
        var connectionString = $"Host={address};Database=postgres;Username={userName};Password={password};";
        return connectionString;
    }
    
    public override async Task<IActionResult> HealthCheck()
    {
        try
        {
            await using var conn = new NpgsqlConnection(GetConnectionString());
            await conn.OpenAsync();

            // If the connection is successfully opened, the database is accessible
            if (conn.State == System.Data.ConnectionState.Open)
            {
                await conn.CloseAsync();
                _logger.LogInformation("Connection to the PgVector database was successful.");
                return new OkObjectResult(new { Message = $"PgVector Connection Healthy" });
            }
            else
            {
                await conn.CloseAsync();
                _logger.LogError("Failed to open the PgVector database connection.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"An error occurred while trying to connect to the database: {ex.Message}");
        }    
        
        return new BadRequestObjectResult(new
            { Message = $"PgVector Connection Failed", ErrorCode = VectorStoreErrors.ConnectionFailed });
    }

    public override async Task Prepare()
    {
        var pipelineId = CollectionName;
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(GetConnectionString());
        dataSourceBuilder.UseVector();
        await using var dataSource = dataSourceBuilder.Build();
        var connection = dataSource.OpenConnection();
        var deleteCommandText = "DELETE FROM embeddings_table WHERE pipelineId = @pipelineId;";
        await using (var deleteCommand = new NpgsqlCommand(deleteCommandText, connection))
        {
            deleteCommand.Parameters.AddWithValue("@pipelineId", pipelineId);
            await deleteCommand.ExecuteNonQueryAsync();
        }
    }

    public override async Task CreateAndInsertVectors(List<string> chunks, List<List<float>> embeddingsVectors)
    {
        _logger.LogInformation("CreateAndInsertVectors");
        var pipelineId = CollectionName;

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(GetConnectionString());
        dataSourceBuilder.UseVector();
        await using var dataSource = dataSourceBuilder.Build();

        var connection = dataSource.OpenConnection();
        _logger.LogInformation("CreateAndInsertVectors : Opening Connection");

        // Start a transaction
        _logger.LogInformation("CreateAndInsertVectors : Start a transaction");

        try
        {
            _logger.LogInformation("CreateAndInsertVectors : Remove all existing items for the given pipelineId");

            // Remove all existing items for the given pipelineId
            var deleteCommandText = "DELETE FROM embeddings_table WHERE pipelineId = @pipelineId;";
            await using (var deleteCommand = new NpgsqlCommand(deleteCommandText, connection))
            {
                deleteCommand.Parameters.AddWithValue("@pipelineId", pipelineId);
                await deleteCommand.ExecuteNonQueryAsync();
            }

            _logger.LogInformation("CreateAndInsertVectors : Insert new vectors");

            // Insert new vectors
            for (int i = 0; i < chunks.Count; i++)
            {
                var chunk = chunks[i];
                var vector = embeddingsVectors[i];
                
                await using (var cmd = new NpgsqlCommand("INSERT INTO embeddings_table (pipelineId, chunkId, text, vector) VALUES ($1, $2, $3, $4)", connection))
                {
                    cmd.Parameters.AddWithValue(pipelineId);
                    var chunkId = $"page{i + 1}";
                    cmd.Parameters.AddWithValue(chunkId);
                    cmd.Parameters.AddWithValue(chunk);
                    cmd.Parameters.AddWithValue(new Vector(vector.ToArray()));
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"An error occurred during PgVector transaction: {ex.Message}");
        }
        _logger.LogInformation("CreateAndInsertVectors : Completed");
    }
    
    public override async Task<List<VectorSearchResult>> Search(List<float> vectors, uint topResults = 5)
    {
        var pipelineId = CollectionName;
        _logger.LogInformation("Search");
        var results = new List<VectorSearchResult>();

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(GetConnectionString());
        dataSourceBuilder.UseVector();
        await using var dataSource = dataSourceBuilder.Build();

        var connection = dataSource.OpenConnection();
        
        await using (var cmd = new NpgsqlCommand("SELECT chunkId, text, vector FROM embeddings_table WHERE pipelineId = $1 ORDER BY vector <-> $2 LIMIT $3", connection))
        {
            cmd.Parameters.AddWithValue(pipelineId);
            var embedding = new Vector(vectors.ToArray());
            cmd.Parameters.AddWithValue(embedding);
            cmd.Parameters.AddWithValue((int)topResults);
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var vectorSearchResult = new VectorSearchResult
                    {
                        id = reader.GetString(0),
                        text = reader.GetString(1)
                    };
                    var vector = (Vector)reader.GetValue(2);
                    vectorSearchResult.values = new List<float>(vector.ToArray());
                    results.Add(vectorSearchResult);
                }
            }
        }

        _logger.LogInformation($"Found {results.Count} vectors for pipelineId {pipelineId}.");
        return results;
    }
}