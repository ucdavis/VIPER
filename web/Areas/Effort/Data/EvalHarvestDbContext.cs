using Microsoft.EntityFrameworkCore;
using Viper.Areas.Effort.Models.Entities;

namespace Viper.Areas.Effort.Data;

/// <summary>
/// Entity Framework DbContext for the evalHarvest database.
/// All tables are in the default (dbo) schema.
/// </summary>
public class EvalHarvestDbContext : DbContext
{
    public EvalHarvestDbContext(DbContextOptions<EvalHarvestDbContext> options) : base(options)
    {
    }

    public virtual DbSet<EhCourse> Courses { get; set; }
    public virtual DbSet<EhQuestion> Questions { get; set; }
    public virtual DbSet<EhQuant> Quants { get; set; }
    public virtual DbSet<EhPerson> People { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (HttpHelper.Settings != null)
        {
            optionsBuilder.UseSqlServer(HttpHelper.Settings["ConnectionStrings:EvalHarvest"]);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // EhCourse (dbo.eh_Courses)
        modelBuilder.Entity<EhCourse>(entity =>
        {
            entity.HasKey(e => new { e.Crn, e.TermCode, e.FacilitatorEvalId });
            entity.ToTable("eh_Courses");

            entity.Property(e => e.Crn).HasColumnName("course_CRN");
            entity.Property(e => e.TermCode).HasColumnName("course_termCode");
            entity.Property(e => e.FacilitatorEvalId).HasColumnName("course_facilitator_evalid");
            entity.Property(e => e.SubjCode).HasColumnName("course_subj_code").HasMaxLength(3);
            entity.Property(e => e.CrseNumb).HasColumnName("course_crse_numb").HasMaxLength(5);
            entity.Property(e => e.Enrollment).HasColumnName("course_enrollment");
            entity.Property(e => e.HomeDept).HasColumnName("course_home_dept").HasMaxLength(4);
            entity.Property(e => e.Title).HasColumnName("course_title").HasMaxLength(50);
            entity.Property(e => e.Sequence).HasColumnName("course_sequence").HasMaxLength(3);
            entity.Property(e => e.Respondents).HasColumnName("course_respondants");
            entity.Property(e => e.IsAdHoc).HasColumnName("course_adhoc").HasDefaultValue(false);
            entity.Property(e => e.CourseType).HasColumnName("course_type").HasMaxLength(20);
        });

        // EhQuestion (dbo.eh_Questions)
        modelBuilder.Entity<EhQuestion>(entity =>
        {
            entity.HasKey(e => e.QuestId);
            entity.ToTable("eh_Questions");

            entity.Property(e => e.QuestId).HasColumnName("quest_ID")
                .ValueGeneratedOnAdd();
            entity.Property(e => e.Text).HasColumnName("quest_text")
                .HasColumnType("text");
            entity.Property(e => e.Type).HasColumnName("quest_type").HasMaxLength(100);
            entity.Property(e => e.Order).HasColumnName("quest_order");
            entity.Property(e => e.Crn).HasColumnName("quest_CRN");
            entity.Property(e => e.TermCode).HasColumnName("quest_TermCode");
            entity.Property(e => e.IsOverall).HasColumnName("quest_overall")
                .HasDefaultValue(false);
            entity.Property(e => e.EvaluateeType).HasColumnName("quest_EvaluateeType").HasMaxLength(1);
            entity.Property(e => e.FacilitatorEvalId).HasColumnName("quest_facilitator_evalid");
        });

        // EhQuant (dbo.eh_Quant)
        modelBuilder.Entity<EhQuant>(entity =>
        {
            entity.HasKey(e => e.QuantId);
            entity.ToTable("eh_Quant");

            entity.Property(e => e.QuantId).HasColumnName("quant_ID")
                .ValueGeneratedOnAdd();
            entity.Property(e => e.QuestionIdFk).HasColumnName("quant_QuestionID_FK");
            entity.Property(e => e.MailId).HasColumnName("quant_mailid").HasMaxLength(50);
            entity.Property(e => e.NoOpN).HasColumnName("quant_noop_n");
            entity.Property(e => e.NoOpP).HasColumnName("quant_noop_p");
            entity.Property(e => e.Count1N).HasColumnName("quant_1_n");
            entity.Property(e => e.Count2N).HasColumnName("quant_2_n");
            entity.Property(e => e.Count3N).HasColumnName("quant_3_n");
            entity.Property(e => e.Count4N).HasColumnName("quant_4_n");
            entity.Property(e => e.Count5N).HasColumnName("quant_5_n");
            entity.Property(e => e.Count1P).HasColumnName("quant_1_p");
            entity.Property(e => e.Count2P).HasColumnName("quant_2_p");
            entity.Property(e => e.Count3P).HasColumnName("quant_3_p");
            entity.Property(e => e.Count4P).HasColumnName("quant_4_p");
            entity.Property(e => e.Count5P).HasColumnName("quant_5_p");
            entity.Property(e => e.Mean).HasColumnName("quant_mean");
            entity.Property(e => e.Sd).HasColumnName("quant_sd");
            entity.Property(e => e.Enrolled).HasColumnName("quant_enrolled");
            entity.Property(e => e.Respondents).HasColumnName("quant_respondants");
            entity.Property(e => e.EvaluateeType).HasColumnName("quant_EvaluateeType").HasMaxLength(1);

            entity.HasOne(e => e.Question)
                .WithMany(q => q.Quants)
                .HasForeignKey(e => e.QuestionIdFk);
        });

        // EhPerson (dbo.eh_People)
        modelBuilder.Entity<EhPerson>(entity =>
        {
            entity.HasKey(e => new { e.MailId, e.TermCode });
            entity.ToTable("eh_People");

            entity.Property(e => e.MailId).HasColumnName("people_mailid").HasMaxLength(30);
            entity.Property(e => e.TermCode).HasColumnName("people_termCode");
            entity.Property(e => e.MothraId).HasColumnName("people_mothraID").HasMaxLength(10);
            entity.Property(e => e.LoginId).HasColumnName("people_loginID").HasMaxLength(8);
            entity.Property(e => e.FirstName).HasColumnName("people_first_name").HasMaxLength(50);
            entity.Property(e => e.LastName).HasColumnName("people_last_name").HasMaxLength(60);
            entity.Property(e => e.TeachingDept).HasColumnName("people_teachingDept").HasMaxLength(3);
        });
    }
}
