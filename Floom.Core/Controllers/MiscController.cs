using Floom.Repository;
using Floom.Repository.MongoDb;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Floom.Controllers;

[ApiController]
[Route("/v{version:apiVersion}/[controller]")]
public class MiscController : ControllerBase
{
    private IMongoClient _mongoClient;
        
    public MiscController(IMongoClient mongoClient)
    {
        _mongoClient = mongoClient;
    }

    [HttpGet("Health")]
    public async Task<IActionResult> Health()
    {
        var dbInitializer = new MongoDbInitializer(_mongoClient);
        var mongoResult = await dbInitializer.TestConnection();
        if (!mongoResult)
        {
            return StatusCode(500, "Mongo Unhealthy");
        }
        return Ok("Healthy");
    }
}