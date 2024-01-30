using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Microsoft.Extensions.Logging;
using Floom.Controllers;
using Floom.Entities.Model;
using Floom.Services;
using Microsoft.AspNetCore.Mvc;

namespace FloomTests;

[TestClass]
public class ModelsUnitTest
{
    private Mock<ILogger<ModelsController>> _mockLogger;
    private Mock<IModelsService> _mockModelsService;
    private ModelsController _controller;

    public ModelsUnitTest()
    {
        _mockLogger = new Mock<ILogger<ModelsController>>();
        _mockModelsService = new Mock<IModelsService>();
        var expectedModels = new List<ModelDtoV1>
        {
            new ModelDtoV1()
            {
                id= "open-ai-model2",
            }
        };
        _mockModelsService.Setup(service => service.GetAll()).ReturnsAsync(expectedModels);
        _controller = new ModelsController(_mockLogger.Object, _mockModelsService.Object);
    }

    [TestMethod]
    public async Task TestMethod1()
    {
        // Arrange
        var mockRequest = new ModelDtoV1();
        
        // Act
        var result = await _controller.Apply(mockRequest);

        // Assert
        // Assert the expected behavior. This could be checking the status code, the content of the response, etc.
        Assert.IsNotNull(result);
    }
    
    [TestMethod]
    public async Task TestApplyEndpoint()
    {
        // Arrange
        var client = new HttpClient();
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("http://localhost:80/v1/Models/Apply"),
            Headers =
            {
                { "Api-Key", "kTOWPaeL8gikO6IzISudkDygyaZHjxf4" },
            },
            Content = new StringContent(
                @"{
                ""id"": ""open-ai-model2"",
                ""model"": ""gpt-3.5-turbo-1106"",
                ""vendor"": ""OpenAI"",
                ""apiKey"": ""REMOVED""
            }", Encoding.UTF8, "application/json")
        };

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.IsTrue(response.IsSuccessStatusCode);
        
        var models = await _controller.Get();

        Assert.IsInstanceOfType(models.Result, typeof(OkObjectResult));
        
        foreach (var model in models.Value)
        {
            // Assert.IsNotNull(model.id, "Model should not be null");
        }
    }
}