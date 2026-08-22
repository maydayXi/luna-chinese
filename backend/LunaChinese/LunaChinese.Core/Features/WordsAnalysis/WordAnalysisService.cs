using LunaChinese.Core.Models;

namespace LunaChinese.Core.Features.WordsAnalysis;

public class WordAnalysisService : IWordAnalysisService
{
    private const int BatchSize = 10;

    public Task<BatchWordAnalysisResult> AnalyzeBatchAsync(AnalyzeWordsCommand command, IProgress<WordAnalysisProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}