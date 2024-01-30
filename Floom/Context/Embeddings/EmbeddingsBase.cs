using Microsoft.AspNetCore.Mvc;

namespace Floom.Embeddings
{
    public interface EmbeddingsProvider
    {
        public Task<List<List<float>>> GetEmbeddingsAsync(List<string> strings);
        public Task<IActionResult> ValidateModelAsync();
        public string GetModelName();
    }
}