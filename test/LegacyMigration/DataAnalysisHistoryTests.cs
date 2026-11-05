using Viper.Scripts.LegacyMigration.Analysis;

namespace Viper.test.LegacyMigration;

/// <summary>
/// Every run keeps a copy of its results under history, so two runs finishing in the same second must not share a file.
/// </summary>
public class DataAnalysisHistoryTests
{
    [Fact]
    public void HistoryFileName_RunsInTheSameSecond_Differ()
    {
        var first = new DateTime(2026, 10, 30, 14, 5, 9, 120, DateTimeKind.Local);

        Assert.Equal("results-2026-10-30-140509-120.json", DataAnalysis.HistoryFileName(first));
        Assert.NotEqual(DataAnalysis.HistoryFileName(first), DataAnalysis.HistoryFileName(first.AddMilliseconds(1)));
    }
}
