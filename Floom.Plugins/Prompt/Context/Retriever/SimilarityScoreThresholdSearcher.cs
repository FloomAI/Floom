using Floom.Plugins.Prompt.Context.VectorStores;

namespace Floom.Plugins.Prompt.Context.Retriever;

public class SimilarityScoreThresholdSearcher
{
    private uint kIncrement = 5;
    private int maxK = 50;
    private double minSimilarityScore = 0.5;

    public async Task<List<VectorSearchResult>> GetRelevantDocuments(VectorStoreProvider vectorStoreProvider, List<float> queryVectors)
    {
        uint currentK = 0;
        var allResults = new List<VectorSearchResult>();

        do
        {
            currentK += kIncrement;
            var results = await vectorStoreProvider.Search(
                queryVectors,
                currentK); // Adjusted to match the parameter names and types

            // Filter results directly by the similarity score
            var filteredResults = results.Where(result => result.score >= minSimilarityScore).ToList();

            if (filteredResults.Count > 0)
            {
                allResults.AddRange(filteredResults);
                // Remove duplicates that might be added in subsequent searches
                allResults = allResults.GroupBy(result => result.text).Select(grp => grp.First()).ToList();
            }

        } while (allResults.Count < maxK && allResults.Count >= currentK);

        // Limit the results to maxK
        return allResults.Take(maxK).ToList();
    }
}
