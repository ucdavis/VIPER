using Microsoft.AspNetCore.Mvc;
using Viper.Areas.Personnel.Models.PersonCollector;
using Viper.Areas.Personnel.Services;
using Viper.Classes;
using Viper.Classes.Utilities;
using Web.Authorization;

namespace Viper.Areas.Personnel.Controllers;

/// <summary>
/// The Person Collector: lists of names and email addresses (and, with extra permissions, IDs)
/// for chosen groups of faculty, staff and students. Results are POSTed because the choices are
/// lists; nothing is changed.
/// </summary>
[Route("/api/personnel/person-collector")]
[Permission(Allow = PersonCollectorPermissions.Access)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class PersonCollectorController : ApiController
{
    private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const string ExportName = "PersonCollector";
    private const string NothingChosen = "Choose at least one group.";

    private readonly IPersonCollectorService _collector;
    private readonly IUserHelper _userHelper;
    private readonly ILogger<PersonCollectorController> _logger;

    public PersonCollectorController(
        IPersonCollectorService collector, IUserHelper userHelper, ILogger<PersonCollectorController> logger)
    {
        _collector = collector;
        _userHelper = userHelper;
        _logger = logger;
    }

    /// <summary>The form's choices and the columns this user will see.</summary>
    [HttpGet("form")]
    public ActionResult<PersonCollectorForm> GetForm()
    {
        return Ok(_collector.GetForm());
    }

    [HttpPost("results")]
    public async Task<ActionResult<PersonCollectorResult>> GetResults(
        [FromBody] PersonCollectorRequest request, CancellationToken ct = default)
    {
        if (Refuse(request) is { } refused)
        {
            return refused;
        }

        PersonCollectorResult result = await _collector.CollectAsync(request, ct);
        Audit("viewed", result);
        return Ok(result);
    }

    [HttpPost("export")]
    public async Task<ActionResult> Export([FromBody] PersonCollectorRequest request, CancellationToken ct = default)
    {
        if (Refuse(request) is { } refused)
        {
            return refused;
        }

        PersonCollectorResult result = await _collector.CollectAsync(request, ct);
        Audit("exported", result);
        string filename = ExcelHelper.BuildExportFilename(new ExportFilenameOptions { ReportName = ExportName });
        return File(PersonCollectorExcel.Build(result), XlsxContentType, filename);
    }

    /// <summary>A 400 for an empty request or one with keys that aren't on the form; otherwise null.</summary>
    private BadRequestObjectResult? Refuse(PersonCollectorRequest? request)
    {
        if (request is null)
        {
            return BadRequest(NothingChosen);
        }

        if (PersonCollectorPlan.UnknownKeys(request).Count > 0)
        {
            return BadRequest("One or more choices aren't on the form.");
        }

        return PersonCollectorPlan.Create(request).IsEmpty ? BadRequest(NothingChosen) : null;
    }

    /// <summary>Logs who pulled which lists: they hold names, email addresses and IDs.</summary>
    private void Audit(string action, PersonCollectorResult result)
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Person Collector {Action} by {LoginId}: {Sections}",
                action,
                LogSanitizer.SanitizeId(_userHelper.GetCurrentUser()?.LoginId),
                string.Join(", ", result.Sections.Select(section => $"{section.Key} ({section.People.Count})")));
        }
    }
}
