using Floom.Data.Entities;
using Floom.Entities;
using Floom.Entities.Prompt;
using Floom.Entities.Response;

namespace Floom.Pipeline.Entities
{
    public class OldPipelineModel : BaseModel
    {
        public string Schema { get; set; }
        public List<Floom.Entities.Model.Model>? Models { get; set; }
        public PromptModel? Prompt { get; set; }
        public ResponseModel? Response { get; set; }
        public bool ChatHistory { get; set; }
        public List<DataModel>? Data { get; set; }
    }
}