using Viper.Areas.Students.Models;

namespace Viper.test.Students;

/// <summary>
/// Tests for CareerSelectionExportRequest.ApplyRowKeys: how the grid's row keys narrow and order
/// the rows an export writes.
/// </summary>
public class CareerSelectionExportRequestTests
{
    private static List<StudentCareerReportDto> Rows(params string[] keys) =>
        keys.Select(k => new StudentCareerReportDto { RowKey = k }).ToList();

    private static List<string> KeysOf(List<StudentCareerReportDto> rows) =>
        rows.Select(r => r.RowKey).ToList();

    [Fact]
    public void ApplyRowKeys_NoRequest_ReturnsEveryRowInItsOrder()
    {
        var result = CareerSelectionExportRequest.ApplyRowKeys(Rows("1", "2", "3"), null);

        Assert.Equal(["1", "2", "3"], KeysOf(result));
    }

    [Fact]
    public void ApplyRowKeys_NullKeys_ReturnsEveryRowInItsOrder()
    {
        var result = CareerSelectionExportRequest.ApplyRowKeys(Rows("1", "2", "3"),
            new CareerSelectionExportRequest { RowKeys = null });

        Assert.Equal(["1", "2", "3"], KeysOf(result));
    }

    [Fact]
    public void ApplyRowKeys_EmptyKeys_ReturnsNoRows()
    {
        // The grid's search matched nothing, which is not the same as no filter at all.
        var result = CareerSelectionExportRequest.ApplyRowKeys(Rows("1", "2", "3"),
            new CareerSelectionExportRequest { RowKeys = [] });

        Assert.Empty(result);
    }

    [Fact]
    public void ApplyRowKeys_FollowsTheKeysOrder()
    {
        var result = CareerSelectionExportRequest.ApplyRowKeys(Rows("1", "2", "3"),
            new CareerSelectionExportRequest { RowKeys = ["3", "1", "2"] });

        Assert.Equal(["3", "1", "2"], KeysOf(result));
    }

    [Fact]
    public void ApplyRowKeys_KeysOutsideTheRows_AreIgnored()
    {
        // A key for a student the caller may not see matches nothing and adds nothing.
        var result = CareerSelectionExportRequest.ApplyRowKeys(Rows("1", "2"),
            new CareerSelectionExportRequest { RowKeys = ["99", "2", "STU00009"] });

        Assert.Equal(["2"], KeysOf(result));
    }

    [Fact]
    public void ApplyRowKeys_RepeatedKey_KeepsItsFirstPositionAndOneRow()
    {
        var result = CareerSelectionExportRequest.ApplyRowKeys(Rows("1", "2"),
            new CareerSelectionExportRequest { RowKeys = ["2", "1", "2"] });

        Assert.Equal(["2", "1"], KeysOf(result));
    }

    [Fact]
    public void ApplyRowKeys_NullKeyInTheList_IsSkipped()
    {
        var result = CareerSelectionExportRequest.ApplyRowKeys(Rows("1", "2"),
            new CareerSelectionExportRequest { RowKeys = [null!, "2"] });

        Assert.Equal(["2"], KeysOf(result));
    }

    [Fact]
    public void ApplyRowKeys_MatchesKeysExactly()
    {
        // Unmapped students are keyed by MothraId, so case must not merge two different keys.
        var result = CareerSelectionExportRequest.ApplyRowKeys(Rows("STU00001"),
            new CareerSelectionExportRequest { RowKeys = ["stu00001"] });

        Assert.Empty(result);
    }
}
