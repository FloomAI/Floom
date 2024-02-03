using Floom.Auth;
using Floom.Pipeline;
using Floom.Pipeline.Entities;
using Floom.Pipeline.Entities.Dtos;
using Microsoft.AspNetCore.Mvc;
using DataType = Floom.Pipeline.Entities.Dtos.DataType;

namespace Floom.Controllers
{
    [ApiController]
    [Route("/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    public class PipelinesController : ControllerBase
    {
        private readonly ILogger<PipelinesController> _logger;
        private readonly IPipelinesService _service;

        public PipelinesController(
            ILogger<PipelinesController> logger,
            IPipelinesService service)

        {
            _logger = logger;
            _service = service;
        }
        
        [HttpPost("Commit")]
        public async Task<IActionResult?> Commit(PipelineDto pipelineDto)
        {
            _logger.LogInformation("Pipelines/Commit Invoked");
            await _service.Commit(pipelineDto);
            return new OkResult();
        }

        public Task<IActionResult> RunMockPipeline()
        {
            var mockFloomResponse = new FloomResponse
            {
                messageId = "mockMessageId123",
                chatId = "mockChatId456",
                values = new List<ResponseValue>
                {
                    new ResponseValue
                    {
                        type = DataType.String,
                        format = "text/plain",
                        value = "Hello, this is a mock string!",
                        b64 = Convert.ToBase64String(
                            System.Text.Encoding.UTF8.GetBytes("Hello, this is a mock string!")),
                        url = "http://example.com/mockurl"
                    },
                    // Add more ResponseValue items as needed
                },
                processingTime = 12345,
                tokenUsage = new FloomResponseTokenUsage
                {
                    // Populate with mock data
                }
            };

            return Task.FromResult<IActionResult>(Ok(mockFloomResponse));
        }

        //JSON
        //Think of chat, not only message. Keep Session history somewhere based on "Session ID".
        //Think of multimodel (text, image, audio, video INPUT/OUTPUT)
        //Think of static prompt / dynamic one + variables (SDK/API)[
        [HttpPost("Run")]
        [Consumes("application/json")]
        public async Task<IActionResult> Run(
            FloomRequest floomRequest
        )
        {
            var response = await _service.Run(floomRequest);
            return Ok(response);
        }
    }
}