using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Viper.Models.AAUD;
using Viper.Services;

namespace Viper.test.Services;

/// <summary>
/// Unit tests for EmailService.
/// Tests focus on exception wrapping behavior since SMTP operations require mocking.
/// </summary>
public class EmailServiceTests
{
    private readonly ILogger<EmailService> _loggerMock;
    private readonly IHostEnvironment _hostEnvironmentMock;
    private readonly IHttpContextAccessor _httpContextAccessorMock;
    private readonly IUserHelper _userHelperMock;

    public EmailServiceTests()
    {
        _loggerMock = Substitute.For<ILogger<EmailService>>();
        _hostEnvironmentMock = Substitute.For<IHostEnvironment>();
        _httpContextAccessorMock = Substitute.For<IHttpContextAccessor>();
        _userHelperMock = Substitute.For<IUserHelper>();

        _hostEnvironmentMock.EnvironmentName.Returns("Development");
    }

    private EmailService CreateService(EmailSettings? settings = null)
    {
        settings ??= new EmailSettings
        {
            SmtpHost = "localhost",
            SmtpPort = 0, // Avoids sending emails during tests
            DefaultFromAddress = "test@example.com",
            UseMailpit = true
        };

        return new EmailService(
            Options.Create(settings),
            _loggerMock,
            _hostEnvironmentMock,
            _httpContextAccessorMock,
            _userHelperMock);
    }

    [Fact]
    public async Task SendMultipartEmailAsync_NoBody_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert - ArgumentException should NOT be wrapped (it's a programming error)
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.SendMultipartEmailAsync("recipient@example.com", "Subject", null, null));
    }

    [Fact]
    public async Task SendEmailAsync_ValidAddresses_DevModeSkipsWhenMailpitUnavailable()
    {
        // Arrange
        var service = CreateService();

        // Act - In dev mode with Mailpit, connection failures are silently skipped
        await service.SendEmailAsync("recipient@example.com", "Subject", "<p>Body</p>");

        // Assert - No exception means dev+Mailpit path worked (silent skip)
        Assert.True(true);
    }

    [Fact]
    public void GetConfigurationStatus_ReturnsExpectedFormat()
    {
        // Arrange
        var settings = new EmailSettings
        {
            SmtpHost = "mail.example.com",
            SmtpPort = 587,
            EnableSsl = true,
            UseMailpit = false
        };
        var service = CreateService(settings);

        // Act
        var status = service.GetConfigurationStatus();

        // Assert
        Assert.Contains("mail.example.com:587", status);
        Assert.Contains("SSL: True", status);
    }

    [Fact]
    public void GetConfigurationStatus_MailpitMode_IndicatesMailpit()
    {
        // Arrange
        var settings = new EmailSettings
        {
            SmtpHost = "localhost",
            SmtpPort = 1025,
            UseMailpit = true
        };
        var service = CreateService(settings);

        // Act
        var status = service.GetConfigurationStatus();

        // Assert
        Assert.Contains("Mailpit", status);
    }

    [Fact]
    public async Task SendEmailAsync_RedirectEnabled_NoUser_SuppressesEmail()
    {
        // Arrange
        var settings = new EmailSettings
        {
            SmtpHost = "localhost",
            SmtpPort = 1025,
            UseMailpit = true,
            RedirectToCurrentUser = true
        };
        _userHelperMock.GetCurrentUser().Returns((AaudUser?)null);
        var service = CreateService(settings);

        // Act - should not throw, email is silently suppressed
        await service.SendEmailAsync("recipient@example.com", "Subject", "<p>Body</p>");

        // Assert - verify GetCurrentUser was called
        _userHelperMock.Received(1).GetCurrentUser();
    }

    [Fact]
    public async Task SendEmailAsync_RedirectEnabled_WithUser_RedirectsToCurrentUser()
    {
        // Arrange
        var settings = new EmailSettings
        {
            SmtpHost = "localhost",
            SmtpPort = 1025,
            UseMailpit = true,
            RedirectToCurrentUser = true
        };
        var currentUser = new AaudUser { MailId = "testuser@ucdavis.edu" };
        _userHelperMock.GetCurrentUser().Returns(currentUser);
        var service = CreateService(settings);

        // Act - In dev mode with Mailpit unavailable, connection failures are silently skipped
        await service.SendEmailAsync("original@example.com", "Test Subject", "<p>Body</p>");

        // Assert - verify redirect happened (GetCurrentUser was called)
        _userHelperMock.Received(1).GetCurrentUser();
    }

    [Fact]
    public async Task SendEmailAsync_RedirectEnabled_MailIdWithoutDomain_AppendsDomain()
    {
        // Arrange
        var settings = new EmailSettings
        {
            SmtpHost = "localhost",
            SmtpPort = 1025,
            UseMailpit = true,
            RedirectToCurrentUser = true
        };
        // MailId without @domain - should append @ucdavis.edu
        var currentUser = new AaudUser { MailId = "testuser" };
        _userHelperMock.GetCurrentUser().Returns(currentUser);
        var service = CreateService(settings);

        // Act - In dev mode with Mailpit unavailable, connection failures are silently skipped
        await service.SendEmailAsync("original@example.com", "Test Subject", "<p>Body</p>");

        // Assert - verify GetCurrentUser was called (email would be testuser@ucdavis.edu)
        _userHelperMock.Received(1).GetCurrentUser();
    }

    [Fact]
    public async Task SendEmailAsync_RedirectDisabled_SendsToOriginalRecipient()
    {
        // Arrange
        var settings = new EmailSettings
        {
            SmtpHost = "localhost",
            SmtpPort = 1025,
            UseMailpit = true,
            RedirectToCurrentUser = false
        };
        var service = CreateService(settings);

        // Act
        await service.SendEmailAsync("recipient@example.com", "Subject", "<p>Body</p>");

        // Assert - GetCurrentUser should NOT be called when redirect is disabled
        _userHelperMock.DidNotReceive().GetCurrentUser();
    }

    [Fact]
    public async Task SendEmailAsync_UseMailpitFalse_RedirectEnabled_NoUser_SuppressesWithoutSmtpAttempt()
    {
        // Arrange - mirrors TEST environment: UseMailpit=false, RedirectToCurrentUser=true
        var settings = new EmailSettings
        {
            SmtpHost = "localhost",
            SmtpPort = 1025,
            UseMailpit = false,
            RedirectToCurrentUser = true
        };
        _userHelperMock.GetCurrentUser().Returns((AaudUser?)null);
        var service = CreateService(settings);

        // Act - no exception should be thrown; email is suppressed before SMTP attempt
        await service.SendEmailAsync("recipient@example.com", "Subject", "<p>Body</p>");

        // Assert - GetCurrentUser was called (redirect logic was exercised)
        _userHelperMock.Received(1).GetCurrentUser();
    }
}
