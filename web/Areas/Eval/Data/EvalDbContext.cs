using Microsoft.EntityFrameworkCore;
using Viper.Areas.Eval.Models.Entities;

namespace Viper.Areas.Eval.Data;

/// <summary>
/// Entity Framework DbContext for the Eval database.
/// </summary>
public class EvalDbContext : DbContext
{
    public EvalDbContext(DbContextOptions<EvalDbContext> options) : base(options)
    {
    }

    public virtual DbSet<Evaluation> Evaluations { get; set; }
    public virtual DbSet<Template> Templates { get; set; }
    public virtual DbSet<Term> Terms { get; set; }
    public virtual DbSet<Question> Questions { get; set; }
    public virtual DbSet<ResponseType> ResponseTypes { get; set; }
    public virtual DbSet<Response> Responses { get; set; }
    public virtual DbSet<Evaluatee> Evaluatees { get; set; }
    public virtual DbSet<Instance> Instances { get; set; }
    public virtual DbSet<SisHeading> SisHeadings { get; set; }
    public virtual DbSet<InstanceEvaluateeStatus> InstanceEvaluateeStatuses { get; set; }
    public virtual DbSet<TeamCollection> TeamCollections { get; set; }
    public virtual DbSet<Team> Teams { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Term (dbo.Terms)
        modelBuilder.Entity<Term>(entity =>
        {
            entity.HasKey(e => e.TermCode);
            entity.ToTable("Terms");

            entity.Property(e => e.TermCode).HasColumnName("TermCode");
            entity.Property(e => e.Description).HasColumnName("Term_Description").HasMaxLength(20);
            entity.Property(e => e.OpenDate).HasColumnName("Term_OpenDate");
            entity.Property(e => e.CloseDate).HasColumnName("Term_CloseDate");
            entity.Property(e => e.HarvestDate).HasColumnName("Term_HarvestDate");
            entity.Property(e => e.GradYear).HasColumnName("Term_GradYear");
        });

        // ResponseType (dbo.ResponseTypes)
        modelBuilder.Entity<ResponseType>(entity =>
        {
            entity.HasKey(e => e.ResponseTypeId);
            entity.ToTable("ResponseTypes");

            entity.Property(e => e.ResponseTypeId).HasColumnName("ResponseType_ID").ValueGeneratedNever();
            entity.Property(e => e.Type).HasColumnName("ResponseType_Type").HasMaxLength(50);
            entity.Property(e => e.Order).HasColumnName("ResponseType_Order");
            entity.Property(e => e.InlineLabels).HasColumnName("ResponseType_InlineLabels");
        });

        // SisHeading (dbo.sis_heading)
        modelBuilder.Entity<SisHeading>(entity =>
        {
            entity.HasKey(e => e.HeadingId);
            entity.ToTable("sis_heading");

            entity.Property(e => e.HeadingId).HasColumnName("heading_id");
            entity.Property(e => e.HeadingText).HasColumnName("heading_text").HasMaxLength(100);
        });

        // TeamCollection (dbo.TeamCollection)
        modelBuilder.Entity<TeamCollection>(entity =>
        {
            entity.HasKey(e => e.CollectionId);
            entity.ToTable("TeamCollection");

            entity.Property(e => e.CollectionId).HasColumnName("Collection_ID");
            entity.Property(e => e.Description).HasColumnName("Collection_Description").HasMaxLength(50);
            entity.Property(e => e.ClassYear).HasColumnName("Collection_ClassYear");
            entity.Property(e => e.ClassLevel).HasColumnName("Collection_ClassLevel").HasMaxLength(2);
            entity.Property(e => e.StandardSections).HasColumnName("Collection_StandardSections");
        });

        // Team (dbo.Teams)
        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasKey(e => e.TeamId);
            entity.ToTable("Teams");

            entity.Property(e => e.TeamId).HasColumnName("Team_ID");
            entity.Property(e => e.CollectionId).HasColumnName("Team_Collection_ID");
            entity.Property(e => e.Name).HasColumnName("Team_Name").HasMaxLength(50);

            entity.HasOne<TeamCollection>()
                .WithMany()
                .HasForeignKey(e => e.CollectionId);
        });

        // Template (dbo.Templates)
        modelBuilder.Entity<Template>(entity =>
        {
            entity.HasKey(e => e.TemplateId);
            entity.ToTable("Templates");

            entity.Property(e => e.TemplateId).HasColumnName("Template_ID");
            entity.Property(e => e.Title).HasColumnName("Template_Title").HasMaxLength(50);
            entity.Property(e => e.Course).HasColumnName("Template_Course").HasMaxLength(50);
            entity.Property(e => e.Crn).HasColumnName("Template_CRN");
            entity.Property(e => e.TermCode).HasColumnName("Template_TermCode");
            entity.Property(e => e.Type).HasColumnName("Template_Type").HasMaxLength(50);
            entity.Property(e => e.Order).HasColumnName("Template_Order");
            entity.Property(e => e.Active).HasColumnName("Template_Active");
            entity.Property(e => e.LastEdit).HasColumnName("Template_LastEdit");
            entity.Property(e => e.LastEditMothraId).HasColumnName("Template_LastEdit_Mothra_ID").HasMaxLength(8);
            entity.Property(e => e.Approved).HasColumnName("Template_Approved").HasDefaultValue(false);
            entity.Property(e => e.TypeId).HasColumnName("Template_Type_ID");

            entity.HasOne<Term>()
                .WithMany()
                .HasForeignKey(e => e.TermCode);
        });

        // Question (dbo.Questions)
        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasKey(e => e.QuestionId);
            entity.ToTable("Questions");

            entity.Property(e => e.QuestionId).HasColumnName("Question_ID");
            entity.Property(e => e.ResponseTypeId).HasColumnName("Question_ResponseType_ID");
            entity.Property(e => e.Text).HasColumnName("Question_Text").HasMaxLength(500);
            entity.Property(e => e.Order).HasColumnName("Question_Order");
            entity.Property(e => e.Type).HasColumnName("Question_Type").HasMaxLength(50);
            entity.Property(e => e.TypeDetail).HasColumnName("Question_TypeDetail").HasMaxLength(50);
            entity.Property(e => e.Overall).HasColumnName("Question_Overall");
            entity.Property(e => e.TemplateId).HasColumnName("Question_Template_ID");
            entity.Property(e => e.Header).HasColumnName("Question_Header").HasMaxLength(50);
            entity.Property(e => e.AuthorMothraId).HasColumnName("Question_AuthorMothra_ID").HasMaxLength(250);
            entity.Property(e => e.Required).HasColumnName("Question_Required");
            entity.Property(e => e.ParentId).HasColumnName("Question_Parent_ID");
            entity.Property(e => e.Comment).HasColumnName("Question_Comment").HasMaxLength(500);
            entity.Property(e => e.SisHeading).HasColumnName("Question_Sis_Heading");

            entity.HasOne<Template>()
                .WithMany(t => t.Questions)
                .HasForeignKey(e => e.TemplateId);

            entity.HasOne<ResponseType>()
                .WithMany()
                .HasForeignKey(e => e.ResponseTypeId);

            entity.HasOne<SisHeading>()
                .WithMany()
                .HasForeignKey(e => e.SisHeading);
        });

        // Evaluation (dbo.Evaluations)
        modelBuilder.Entity<Evaluation>(entity =>
        {
            entity.HasKey(e => e.EvalId);
            entity.ToTable("Evaluations");

            entity.Property(e => e.EvalId).HasColumnName("Eval_ID");
            entity.Property(e => e.Course).HasColumnName("Eval_Course").HasMaxLength(100);
            entity.Property(e => e.Crn).HasColumnName("Eval_CRN");
            entity.Property(e => e.TermCode).HasColumnName("Eval_TermCode");
            entity.Property(e => e.Type).HasColumnName("Eval_Type").HasMaxLength(50);
            entity.Property(e => e.OpenDate).HasColumnName("Eval_OpenDate");
            entity.Property(e => e.CloseDate).HasColumnName("Eval_CloseDate");
            entity.Property(e => e.CourseTemplateId).HasColumnName("Eval_Course_Template_ID");
            entity.Property(e => e.SchoolTemplateId).HasColumnName("Eval_School_Template_ID");
            entity.Property(e => e.SelfEval).HasColumnName("Eval_SelfEval");
            entity.Property(e => e.PeerTeamCollectionId).HasColumnName("Eval_PeerTeamCollection_ID");
            entity.Property(e => e.ReleaseDate).HasColumnName("Eval_ReleaseDate");
            entity.Property(e => e.External).HasColumnName("Eval_External");
            entity.Property(e => e.RotId).HasColumnName("Eval_Rot_ID");
            entity.Property(e => e.ProgramId).HasColumnName("Eval_Program_ID");
            entity.Property(e => e.Residents).HasColumnName("Eval_Residents");
            entity.Property(e => e.Notify).HasColumnName("eval_notify");

            entity.HasOne<Template>()
                .WithMany()
                .HasForeignKey(e => e.CourseTemplateId);

            entity.HasOne<Template>()
                .WithMany()
                .HasForeignKey(e => e.SchoolTemplateId);

            entity.HasOne<Term>()
                .WithMany()
                .HasForeignKey(e => e.TermCode);

            entity.HasOne<TeamCollection>()
                .WithMany()
                .HasForeignKey(e => e.PeerTeamCollectionId);
        });

        // Evaluatee (dbo.Evaluatees)
        modelBuilder.Entity<Evaluatee>(entity =>
        {
            entity.HasKey(e => e.EvaluateeId);
            entity.ToTable("Evaluatees");

            entity.Property(e => e.EvaluateeId).HasColumnName("Evaluatee_ID");
            entity.Property(e => e.MothraId).HasColumnName("Evaluatee_Mothra_ID").HasMaxLength(8);
            entity.Property(e => e.EvalId).HasColumnName("Evaluatee_Eval_ID");
            entity.Property(e => e.Position).HasColumnName("Evaluatee_Position");
            entity.Property(e => e.Annotation).HasColumnName("Evaluatee_Annotation").HasMaxLength(50);
            entity.Property(e => e.Type).HasColumnName("Evaluatee_Type").HasMaxLength(50);
            entity.Property(e => e.TypeDetail).HasColumnName("Evaluatee_TypeDetail").HasMaxLength(50);
            entity.Property(e => e.Required).HasColumnName("Evaluatee_Required").HasDefaultValue(false);
            entity.Property(e => e.DueDate).HasColumnName("Evaluatee_Due_Date");
            entity.Property(e => e.PeerTeamId).HasColumnName("Evaluatee_PeerTeam_ID");
            entity.Property(e => e.StartWeekId).HasColumnName("Evaluatee_StartWeek_ID");
            entity.Property(e => e.ExtName).HasColumnName("Evaluatee_Ext_Name").HasMaxLength(100);
            entity.Property(e => e.ExtEmail).HasColumnName("Evaluatee_Ext_Email").HasMaxLength(250);
            entity.Property(e => e.ExtPracticeId).HasColumnName("Evaluatee_Ext_PracticeID");
            entity.Property(e => e.ExtGuid).HasColumnName("Evaluatee_Ext_GUID");
            entity.Property(e => e.Submitted).HasColumnName("Evaluatee_Submitted").HasDefaultValue(false);
            entity.Property(e => e.Incomplete).HasColumnName("Evaluatee_Incomplete").HasDefaultValue(false);
            entity.Property(e => e.Reviewed).HasColumnName("Evaluatee_Reviewed");
            entity.Property(e => e.ReviewedDate).HasColumnName("Evaluatee_Reviewed_Date");
            entity.Property(e => e.ReviewedMothraId).HasColumnName("Evaluatee_Reviewed_Mothra_ID").HasMaxLength(8);
            entity.Property(e => e.EvalLength).HasColumnName("Evaluatee_Eval_Length");

            // StartWeekId references the Weeks table, which hasn't been added to this context yet.

            entity.HasOne<Evaluation>()
                .WithMany()
                .HasForeignKey(e => e.EvalId);

            entity.HasOne<Team>()
                .WithMany()
                .HasForeignKey(e => e.PeerTeamId);
        });

        // Response (dbo.Responses)
        modelBuilder.Entity<Response>(entity =>
        {
            entity.HasKey(e => e.ResponseId);
            entity.ToTable("Responses");

            entity.Property(e => e.ResponseId).HasColumnName("Response_ID");
            entity.Property(e => e.Content).HasColumnName("Response_Content").HasColumnType("text");
            entity.Property(e => e.QuestionId).HasColumnName("Response_Question_ID");
            entity.Property(e => e.InstanceId).HasColumnName("Response_Instance_ID");
            entity.Property(e => e.EvaluateeId).HasColumnName("Response_Evaluatee_ID");
            entity.Property(e => e.Comment).HasColumnName("Response_Comment").HasMaxLength(500);

            entity.HasOne<Question>()
                .WithMany()
                .HasForeignKey(e => e.QuestionId);

            entity.HasOne<Evaluatee>()
                .WithMany()
                .HasForeignKey(e => e.EvaluateeId);

            entity.HasOne<Instance>()
                .WithMany()
                .HasForeignKey(e => e.InstanceId);
        });

        // Instance (dbo.Instances)
        modelBuilder.Entity<Instance>(entity =>
        {
            entity.HasKey(e => e.InstanceId);
            entity.ToTable("Instances");

            entity.Property(e => e.InstanceId).HasColumnName("Instance_ID");
            entity.Property(e => e.Status).HasColumnName("Instance_Status").HasMaxLength(50).HasDefaultValue("Not Started");
            entity.Property(e => e.LastModified).HasColumnName("Instance_LastModified");
            entity.Property(e => e.Created).HasColumnName("Instance_Created");
            entity.Property(e => e.EvalId).HasColumnName("Instance_Eval_ID");
            entity.Property(e => e.Mode).HasColumnName("Instance_Mode").HasMaxLength(50);
            entity.Property(e => e.MothraId).HasColumnName("Instance_Mothra_ID").HasMaxLength(8);
            entity.Property(e => e.CourseQuestionsStatus).HasColumnName("Instance_CourseQuestionsStatus").HasMaxLength(10).HasDefaultValue("0%");
            entity.Property(e => e.DueDate).HasColumnName("Instance_Due_Date");
            entity.Property(e => e.StartWeek).HasColumnName("Instance_Start_Week");
            entity.Property(e => e.PeerTeamId).HasColumnName("Instance_PeerTeam_ID");
            entity.Property(e => e.StartWeekId).HasColumnName("Instance_Start_Week_ID");
            entity.Property(e => e.RemindedDate).HasColumnName("Instance_Reminded_Date");

            // StartWeekId references the Weeks table, which hasn't been added to this context yet.

            entity.HasOne<Evaluation>()
                .WithMany()
                .HasForeignKey(e => e.EvalId);

            entity.HasOne<Team>()
                .WithMany()
                .HasForeignKey(e => e.PeerTeamId);
        });

        // InstanceEvaluateeStatus (dbo.InstanceEvaluateeStatus)
        modelBuilder.Entity<InstanceEvaluateeStatus>(entity =>
        {
            entity.HasKey(e => e.InstEvalStatusId);
            entity.ToTable("InstanceEvaluateeStatus");

            entity.Property(e => e.InstEvalStatusId).HasColumnName("InstEvalStatus_ID");
            entity.Property(e => e.InstanceId).HasColumnName("InstEvalStatus_Instance_ID");
            entity.Property(e => e.EvaluateeId).HasColumnName("InstEvalStatus_Evaluatee_ID");
            entity.Property(e => e.EvalId).HasColumnName("InstEvalStatus_Eval_ID");
            entity.Property(e => e.Status).HasColumnName("InstEvalStatus_Status").HasMaxLength(20);
            entity.Property(e => e.LastModified).HasColumnName("InstEvalStatus_LastModified");
            entity.Property(e => e.InsufficientContact).HasColumnName("InstEvalStatus_InsufficientContact").HasDefaultValue(false);
            entity.Property(e => e.Submitted).HasColumnName("InstEvalStatus_Submitted").HasDefaultValue(false);
            entity.Property(e => e.PrimaryEvaluator).HasColumnName("InstEvalStatus_PrimaryEvaluator").HasDefaultValue(false);
            entity.Property(e => e.SubmitDate).HasColumnName("InstEvalStatus_SubmitDate");
            entity.Property(e => e.RemindedDate).HasColumnName("InstEvalStatus_RemindedDate");

            entity.HasOne<Instance>()
                .WithMany()
                .HasForeignKey(e => e.InstanceId);

            entity.HasOne<Evaluatee>()
                .WithMany()
                .HasForeignKey(e => e.EvaluateeId);

            entity.HasOne<Evaluation>()
                .WithMany()
                .HasForeignKey(e => e.EvalId);
        });
    }
}
