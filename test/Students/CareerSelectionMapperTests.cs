using Viper.Areas.Students.Models;
using Viper.Areas.Students.Models.Entities;

namespace Viper.test.Students;

/// <summary>
/// Tests for CareerSelectionMapper: how a submitted form becomes the stored row, in particular
/// which free text survives and what an unfilled field is stored as.
/// </summary>
public class CareerSelectionMapperTests
{
    [Fact]
    public void ApplyStudentInfoToEntity_StoresTheSelectedOptionIds()
    {
        var entity = NewEntity();

        CareerSelectionMapper.ApplyStudentInfoToEntity(new StudentCareerInfoDto
        {
            Direction = Option("Academia", 1),
            PrimaryFocus = Option("Equine", 2),
            SecondaryFocus = Option("Bovine", 3),
            PostGrad = Option("Residency", 4),
            ShortTermPlans = "Internship",
            LongTermPlans = "Practice ownership",
        }, entity);

        Assert.Equal(1, entity.Career);
        Assert.Equal(2, entity.FirstSpecies);
        Assert.Equal(3, entity.SecondSpecies);
        Assert.Equal(4, entity.PostGrad);
        Assert.Equal("Internship", entity.ShortTermStatement);
        Assert.Equal("Practice ownership", entity.LongTermStatement);
    }

    [Fact]
    public void ApplyStudentInfoToEntity_NothingSelected_ClearsTheOptionIds()
    {
        var entity = NewEntity();
        entity.Career = 1;
        entity.FirstSpecies = 2;

        CareerSelectionMapper.ApplyStudentInfoToEntity(new StudentCareerInfoDto(), entity);

        Assert.Null(entity.Career);
        Assert.Null(entity.FirstSpecies);
    }

    [Fact]
    public void ApplyStudentInfoToEntity_CatchAll_KeepsItsFreeText()
    {
        var entity = NewEntity();

        CareerSelectionMapper.ApplyStudentInfoToEntity(new StudentCareerInfoDto
        {
            Direction = Option("Other", 9, isOther: true),
            DirectionOther = "  Wildlife rehabilitation  ",
        }, entity);

        Assert.Equal(9, entity.Career);
        Assert.Equal("Wildlife rehabilitation", entity.CareerOther);
    }

    [Fact]
    public void ApplyStudentInfoToEntity_OrdinaryOption_DropsFreeTextFromAPreviousOtherChoice()
    {
        // The stale text must not reappear if the student switches back to "Other" later.
        var entity = NewEntity();
        entity.CareerOther = "Wildlife rehabilitation";

        CareerSelectionMapper.ApplyStudentInfoToEntity(new StudentCareerInfoDto
        {
            Direction = Option("Academia", 1),
            DirectionOther = "Wildlife rehabilitation",
        }, entity);

        Assert.Equal(string.Empty, entity.CareerOther);
    }

    [Fact]
    public void ApplyStudentInfoToEntity_UnfilledText_IsStoredAsEmptyNotNull()
    {
        var entity = NewEntity();

        CareerSelectionMapper.ApplyStudentInfoToEntity(new StudentCareerInfoDto(), entity);

        Assert.Equal(string.Empty, entity.CareerOther);
        Assert.Equal(string.Empty, entity.FirstSpeciesOther);
        Assert.Equal(string.Empty, entity.SecondSpeciesOther);
        Assert.Equal(string.Empty, entity.ShortTermStatement);
        Assert.Equal(string.Empty, entity.LongTermStatement);
    }

    [Fact]
    public void ApplyStudentInfoToEntity_TrimsStatements()
    {
        var entity = NewEntity();

        CareerSelectionMapper.ApplyStudentInfoToEntity(new StudentCareerInfoDto
        {
            ShortTermPlans = "  Internship\n",
            LongTermPlans = "\tPractice ownership ",
        }, entity);

        Assert.Equal("Internship", entity.ShortTermStatement);
        Assert.Equal("Practice ownership", entity.LongTermStatement);
    }

    [Fact]
    public void ApplyStudentInfoToEntity_LeavesTheMentorAlone()
    {
        // The mentor is admin-only, so the service sets it; the mapper must not touch it.
        var entity = NewEntity();
        entity.FacultyMothraId = "FAC00001";

        CareerSelectionMapper.ApplyStudentInfoToEntity(new StudentCareerInfoDto { MentorId = 500 }, entity);

        Assert.Equal("FAC00001", entity.FacultyMothraId);
    }

    private static CareerSelection NewEntity() => new() { Pidm = 20000001, DateAdded = DateTime.Now };

    private static CareerDropdownOption Option(string label, int value, bool isOther = false) =>
        new() { Label = label, Value = value, IsOther = isOther };
}
