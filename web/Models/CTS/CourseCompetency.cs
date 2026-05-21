namespace Viper.Models.CTS;

public class CourseCompetency
{
    public int CourseCompetencyId { get; set; }

    public int CourseId { get; set; }

    public int CompetencyId { get; set; }

    public int LevelId { get; set; }

    public int Order { get; set; }

    public virtual Course Course { get; set; } = null!;
}
