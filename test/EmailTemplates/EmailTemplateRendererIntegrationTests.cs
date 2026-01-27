using Microsoft.Extensions.Logging;
using Moq;
using Viper.Areas.Effort.EmailTemplates.Models;
using Viper.EmailTemplates.Services;

namespace Viper.test.EmailTemplates;

/// <summary>
/// Integration tests for EmailTemplateRenderer.
/// These tests verify that Razor templates can be rendered correctly.
/// </summary>
public class EmailTemplateRendererIntegrationTests
{
    private readonly EmailTemplateRenderer _renderer;
    private readonly Mock<ILogger<EmailTemplateRenderer>> _loggerMock;

    public EmailTemplateRendererIntegrationTests()
    {
        _loggerMock = new Mock<ILogger<EmailTemplateRenderer>>();
        _renderer = new EmailTemplateRenderer(_loggerMock.Object);
    }

    [Fact]
    public async Task RenderAsync_VerificationReminder_RendersSuccessfully()
    {
        // Arrange
        var viewModel = new VerificationReminderViewModel
        {
            TermDescription = "Fall 2024",
            ReplyByDate = "January 15, 2025",
            VerificationUrl = "https://viper.example.com/effort/verify?term=202410",
            HasZeroEffort = false,
            Courses =
            [
                new EffortCourseGroup
                {
                    CourseCode = "DVM 443-001",
                    Units = 4,
                    Enrollment = 20,
                    Role = "Coordinator",
                    EffortItems =
                    [
                        new EffortLineItem
                        {
                            EffortType = "LEC",
                            Value = 10,
                            Unit = "hours"
                        }
                    ]
                }
            ],
            ChildCourses = []
        };

        // Act
        var html = await _renderer.RenderAsync(
            "/Areas/Effort/EmailTemplates/Views/VerificationReminder.cshtml",
            viewModel);

        // Assert
        Assert.NotNull(html);
        Assert.NotEmpty(html);
        Assert.Contains("Fall 2024", html);
        Assert.Contains("DVM 443-001", html);
        Assert.Contains("Verify My Effort", html);
        Assert.Contains("https://viper.example.com/effort/verify?term=202410", html);
    }

    [Fact]
    public async Task RenderAsync_VerificationReminder_ShowsZeroEffortWarning()
    {
        // Arrange
        var viewModel = new VerificationReminderViewModel
        {
            TermDescription = "Fall 2024",
            ReplyByDate = "January 15, 2025",
            VerificationUrl = "https://viper.example.com/effort/verify",
            HasZeroEffort = true,
            Courses =
            [
                new EffortCourseGroup
                {
                    CourseCode = "VME 200-001",
                    Units = 3,
                    Enrollment = 15,
                    Role = "Instructor",
                    EffortItems =
                    [
                        new EffortLineItem
                        {
                            EffortType = "CLI",
                            Value = 0,
                            Unit = "weeks"
                        }
                    ]
                }
            ],
            ChildCourses = []
        };

        // Act
        var html = await _renderer.RenderAsync(
            "/Areas/Effort/EmailTemplates/Views/VerificationReminder.cshtml",
            viewModel);

        // Assert
        Assert.NotNull(html);
        Assert.Contains("Action Required", html);
        Assert.Contains("ZERO", html);
        Assert.Contains("weeks", html);
        Assert.DoesNotContain("Verify My Effort", html);
    }

    [Fact]
    public async Task RenderAsync_VerificationReminder_ShowsChildCourses()
    {
        // Arrange
        var viewModel = new VerificationReminderViewModel
        {
            TermDescription = "Fall 2024",
            ReplyByDate = "January 15, 2025",
            VerificationUrl = "https://viper.example.com/effort/verify",
            HasZeroEffort = false,
            Courses =
            [
                new EffortCourseGroup
                {
                    CourseCode = "DVM 443-001",
                    Units = 4,
                    Enrollment = 20,
                    Role = "Coordinator",
                    EffortItems =
                    [
                        new EffortLineItem { EffortType = "LEC", Value = 10, Unit = "hours" }
                    ]
                }
            ],
            ChildCourses =
            [
                new ChildCourseDisplay { CourseCode = "DVM 443-002", RelationshipType = "Section" },
                new ChildCourseDisplay { CourseCode = "VME 443-001", RelationshipType = "Cross-Listed" }
            ]
        };

        // Act
        var html = await _renderer.RenderAsync(
            "/Areas/Effort/EmailTemplates/Views/VerificationReminder.cshtml",
            viewModel);

        // Assert
        Assert.NotNull(html);
        Assert.Contains("Cross Listed / Sectioned Courses", html);
        Assert.Contains("DVM 443-002", html);
        Assert.Contains("VME 443-001", html);
        Assert.Contains("Section", html);
        Assert.Contains("Cross-Listed", html);
    }

    [Fact]
    public async Task RenderAsync_VerificationReminder_ShowsNoEffortMessage()
    {
        // Arrange - Instructor with no effort records for the term
        var viewModel = new VerificationReminderViewModel
        {
            TermDescription = "Fall 2024",
            TermStartDate = new DateTime(2024, 9, 25),
            TermEndDate = new DateTime(2024, 12, 13),
            ReplyByDate = "January 15, 2025",
            VerificationUrl = "https://viper.example.com/effort/verify?term=202410",
            HasZeroEffort = false,
            HasNoRecords = true,
            Courses = [],
            ChildCourses = []
        };

        // Act
        var html = await _renderer.RenderAsync(
            "/Areas/Effort/EmailTemplates/Views/VerificationReminder.cshtml",
            viewModel);

        // Assert
        Assert.NotNull(html);
        Assert.NotEmpty(html);
        Assert.Contains("Fall 2024", html);
        Assert.Contains("September 25, 2024", html);
        Assert.Contains("December 13, 2024", html);
        Assert.Contains("no teaching effort recorded", html);
        Assert.Contains("Verify No Effort", html);
        Assert.DoesNotContain("Verify My Effort", html);
        Assert.DoesNotContain("DVM", html); // No course table
    }

    [Fact]
    public async Task RenderAsync_InvalidTemplate_ThrowsInvalidOperationException()
    {
        // Arrange
        var viewModel = new VerificationReminderViewModel();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _renderer.RenderAsync(
                "/NonExistent/Template.cshtml",
                viewModel));

        Assert.Contains("Failed to render email template", exception.Message);
    }
}
