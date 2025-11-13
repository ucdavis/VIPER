using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Viper.Areas.Students.Models;
using Viper.Classes.Utilities;
using WordDocument = DocumentFormat.OpenXml.Wordprocessing.Document;
using PdfDocument = QuestPDF.Fluent.Document;

namespace Viper.Areas.Students.Services
{
    public interface IPhotoExportService
    {
        Task<PhotoExportResult> ExportToWordAsync(PhotoExportRequest request);
        Task<PhotoExportResult> ExportToPdfAsync(PhotoExportRequest request);
        Task<object> GetExportStatusAsync(string exportId);
    }

    public class PhotoExportService : IPhotoExportService
    {
        private readonly IStudentGroupService _studentGroupService;
        private readonly IPhotoService _photoService;
        private readonly ILogger<PhotoExportService> _logger;
        private readonly Curriculum.Services.TermCodeService _termCodeService;
        private readonly ICourseService _courseService;

        public PhotoExportService(
            IStudentGroupService studentGroupService,
            IPhotoService photoService,
            ILogger<PhotoExportService> logger,
            Curriculum.Services.TermCodeService termCodeService,
            ICourseService courseService)
        {
            _studentGroupService = studentGroupService;
            _photoService = photoService;
            _logger = logger;
            _termCodeService = termCodeService;
            _courseService = courseService;
        }

        public async Task<PhotoExportResult> ExportToWordAsync(PhotoExportRequest request)
        {
            try
            {
                _logger.LogDebug("ExportToWordAsync called with ClassLevel={ClassLevel}, GroupType={GroupType}, GroupId={GroupId}, IncludeRoss={IncludeRoss}",
                    LogSanitizer.SanitizeString(request.ClassLevel), LogSanitizer.SanitizeString(request.GroupType), LogSanitizer.SanitizeString(request.GroupId), request.IncludeRossStudents);

                var students = await GetStudentsForExport(request);

                if (!students.Any())
                {
                    _logger.LogWarning("No students found for export request");
                    return null;
                }

                using var stream = new MemoryStream();
                using (var wordDocument = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
                {
                    var mainPart = wordDocument.AddMainDocumentPart();
                    mainPart.Document = new WordDocument();
                    var body = mainPart.Document.AppendChild(new Body());

                    var titleLines = (await GetExportTitleAsync(request)).Split('\n');

                    var titleParagraph = body.AppendChild(new Paragraph());
                    var titleRun = titleParagraph.AppendChild(new Run());
                    titleRun.AppendChild(new Text(titleLines[0]));

                    var titleProps = titleRun.PrependChild(new RunProperties());
                    titleProps.AppendChild(new Bold());
                    titleProps.AppendChild(new FontSize() { Val = "32" });

                    // Add export date on new line with smaller font
                    if (titleLines.Length > 1)
                    {
                        titleRun.AppendChild(new Break());
                        var dateRun = titleParagraph.AppendChild(new Run());
                        dateRun.AppendChild(new Text(titleLines[1]));
                        var dateProps = dateRun.PrependChild(new RunProperties());
                        dateProps.AppendChild(new FontSize() { Val = "16" });
                        dateProps.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Color() { Val = "808080" }); // Gray color
                    }

                    var table = body.AppendChild(new Table());
                    var tableProps = table.AppendChild(new TableProperties());
                    tableProps.AppendChild(new TableBorders(
                        new TopBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                        new BottomBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                        new LeftBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                        new RightBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                        new InsideHorizontalBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                        new InsideVerticalBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 }
                    ));

                    const int photosPerRow = 4;
                    uint imageId = 1;
                    for (int i = 0; i < students.Count; i += photosPerRow)
                    {
                        var row = table.AppendChild(new TableRow());
                        var rowStudents = students.Skip(i).Take(photosPerRow).ToList();

                        foreach (var student in rowStudents)
                        {
                            var cell = row.AppendChild(new TableCell());

                            // Add photo
                            var photoBytes = await _photoService.GetStudentPhotoAsync(student.MailId);
                            if (photoBytes != null && photoBytes.Length > 0)
                            {
                                var photoParagraph = cell.AppendChild(new Paragraph());
                                var photoParagraphProps = photoParagraph.AppendChild(new ParagraphProperties());
                                photoParagraphProps.AppendChild(new Justification() { Val = JustificationValues.Center });
                                var photoRun = photoParagraph.AppendChild(new Run());

                                var imagePart = mainPart.AddImagePart(DocumentFormat.OpenXml.Packaging.ImagePartType.Jpeg);
                                using (var photoStream = new MemoryStream(photoBytes))
                                {
                                    imagePart.FeedData(photoStream);
                                }

                                imageId++; // Increment before use to avoid duplicate IDs

                                var inline = new DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline(
                                    new DocumentFormat.OpenXml.Drawing.Wordprocessing.Extent() { Cx = 950000L, Cy = 1267000L },
                                    new DocumentFormat.OpenXml.Drawing.Wordprocessing.EffectExtent() { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                                    new DocumentFormat.OpenXml.Drawing.Wordprocessing.DocProperties() { Id = (UInt32Value)imageId, Name = $"Photo{imageId}" },
                                    new DocumentFormat.OpenXml.Drawing.Wordprocessing.NonVisualGraphicFrameDrawingProperties(
                                        new DocumentFormat.OpenXml.Drawing.GraphicFrameLocks() { NoChangeAspect = true }),
                                    new DocumentFormat.OpenXml.Drawing.Graphic(
                                        new DocumentFormat.OpenXml.Drawing.GraphicData(
                                            new DocumentFormat.OpenXml.Drawing.Pictures.Picture(
                                                new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualPictureProperties(
                                                    new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualDrawingProperties() { Id = (UInt32Value)imageId, Name = $"Photo{imageId}.jpg" },
                                                    new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualPictureDrawingProperties()),
                                                new DocumentFormat.OpenXml.Drawing.Pictures.BlipFill(
                                                    new DocumentFormat.OpenXml.Drawing.Blip() { Embed = mainPart.GetIdOfPart(imagePart) },
                                                    new DocumentFormat.OpenXml.Drawing.Stretch(
                                                        new DocumentFormat.OpenXml.Drawing.FillRectangle())),
                                                new DocumentFormat.OpenXml.Drawing.Pictures.ShapeProperties(
                                                    new DocumentFormat.OpenXml.Drawing.Transform2D(
                                                        new DocumentFormat.OpenXml.Drawing.Offset() { X = 0L, Y = 0L },
                                                        new DocumentFormat.OpenXml.Drawing.Extents() { Cx = 950000L, Cy = 1267000L }),
                                                    new DocumentFormat.OpenXml.Drawing.PresetGeometry(
                                                        new DocumentFormat.OpenXml.Drawing.AdjustValueList())
                                                    { Preset = DocumentFormat.OpenXml.Drawing.ShapeTypeValues.Rectangle }))
                                        )
                                        { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" })
                                )
                                { DistanceFromTop = (UInt32Value)0U, DistanceFromBottom = (UInt32Value)0U, DistanceFromLeft = (UInt32Value)0U, DistanceFromRight = (UInt32Value)0U };

                                var drawing = new DocumentFormat.OpenXml.Wordprocessing.Drawing(inline);

                                photoRun.AppendChild(drawing);
                            }

                            // Add name and secondary text using centralized display logic
                            var cellParagraph = cell.AppendChild(new Paragraph());
                            var cellParagraphProps = cellParagraph.AppendChild(new ParagraphProperties());
                            cellParagraphProps.AppendChild(new Justification() { Val = JustificationValues.Center });
                            var cellRun = cellParagraph.AppendChild(new Run());
                            cellRun.AppendChild(new Text(student.FullName));

                            foreach (var line in student.SecondaryTextLines)
                            {
                                cellRun.AppendChild(new Break());
                                cellRun.AppendChild(new Text(line));
                            }
                        }

                        for (int j = rowStudents.Count; j < photosPerRow; j++)
                        {
                            row.AppendChild(new TableCell(new Paragraph()));
                        }
                    }

                    mainPart.Document.Save();
                }

                stream.Position = 0;
                var fileData = stream.ToArray();

                return new PhotoExportResult
                {
                    ExportId = Guid.NewGuid().ToString(),
                    FileData = fileData,
                    FileName = $"StudentPhotos_{DateTime.Now:yyyyMMddHHmmss}.docx",
                    ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                };
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Access denied while exporting photos to Word");
                return null;
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "I/O error exporting photos to Word");
                return null;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation while exporting photos to Word");
                return null;
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid argument exporting photos to Word");
                return null;
            }
        }

        public async Task<PhotoExportResult> ExportToPdfAsync(PhotoExportRequest request)
        {
            try
            {
                _logger.LogDebug("ExportToPdfAsync called with ClassLevel={ClassLevel}, GroupType={GroupType}, GroupId={GroupId}, IncludeRoss={IncludeRoss}",
                    LogSanitizer.SanitizeString(request.ClassLevel), LogSanitizer.SanitizeString(request.GroupType), LogSanitizer.SanitizeString(request.GroupId), request.IncludeRossStudents);

                var students = await GetStudentsForExport(request);

                if (!students.Any())
                {
                    _logger.LogWarning("No students found for export request");
                    return null;
                }

                // Pre-load all photos with concurrency limit
                _logger.LogDebug("Pre-loading photos for {Count} students", students.Count);
                var photoCache = new Dictionary<string, byte[]>();
                using var semaphore = new SemaphoreSlim(10); // Limit to 10 concurrent I/O operations

                var photoTasks = students.Select(async student =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        var photoBytes = await _photoService.GetStudentPhotoAsync(student.MailId);
                        _logger.LogDebug("Student {MailId}: photoBytes is {Status}, Length = {Length}",
                            LogSanitizer.SanitizeId(student.MailId),
                            photoBytes == null ? "null" : "not null",
                            photoBytes?.Length ?? -1);
                        return new { student.MailId, photoBytes };
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }).ToList();

                var photoResults = await Task.WhenAll(photoTasks);
                foreach (var result in photoResults.Where(r => r.photoBytes != null && r.photoBytes.Length > 0))
                {
                    photoCache[result.MailId] = result.photoBytes;
                }
                _logger.LogInformation("Loaded {Count} photos into cache", photoCache.Count);

                // Fetch title before entering QuestPDF builder to avoid async void lambda
                var titleLines = (await GetExportTitleAsync(request)).Split('\n');

                QuestPDF.Settings.License = LicenseType.Community;

                var document = PdfDocument.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.Letter);
                        page.Margin(1, Unit.Inch);
                        page.PageColor(Colors.White);

                        page.Header()
                            .Column(column =>
                            {
                                column.Item().Text(titleLines[0])
                                    .SemiBold()
                                    .FontSize(20)
                                    .FontColor(Colors.Black);

                                if (titleLines.Length > 1)
                                {
                                    column.Item().Text(titleLines[1])
                                        .FontSize(10)
                                        .FontColor(Colors.Grey.Darken1);
                                }
                            });

                        page.Content()
                            .PaddingVertical(1, Unit.Centimetre)
                            .Column(column =>
                            {
                                column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                    });

                                    const int photosPerRow = 4;
                                    for (int i = 0; i < students.Count; i += photosPerRow)
                                    {
                                        var rowStudents = students.Skip(i).Take(photosPerRow).ToList();

                                        foreach (var student in rowStudents)
                                        {
                                            table.Cell().Border(1).Padding(5).Column(innerColumn =>
                                            {
                                                // Add photo from cache
                                                if (photoCache.TryGetValue(student.MailId, out var photoBytes))
                                                {
                                                    innerColumn.Item().Image(photoBytes).FitWidth();
                                                }

                                                // Use centralized display logic
                                                innerColumn.Item().Text(student.FullName)
                                                    .FontSize(10);

                                                foreach (var line in student.SecondaryTextLines)
                                                {
                                                    innerColumn.Item().Text(line)
                                                        .FontSize(8)
                                                        .FontColor(Colors.Grey.Darken1);
                                                }
                                            });
                                        }

                                        for (int j = rowStudents.Count; j < photosPerRow; j++)
                                        {
                                            table.Cell().Border(1).Element(Block);
                                            static void Block(IContainer container) => container.Height(50);
                                        }
                                    }
                                });
                            });

                        page.Footer()
                            .AlignCenter()
                            .Text(x =>
                            {
                                x.Span("Page ");
                                x.CurrentPageNumber();
                                x.Span(" of ");
                                x.TotalPages();
                            });
                    });
                });

                var pdfBytes = document.GeneratePdf();

                return new PhotoExportResult
                {
                    ExportId = Guid.NewGuid().ToString(),
                    FileData = pdfBytes,
                    FileName = $"StudentPhotos_{DateTime.Now:yyyyMMddHHmmss}.pdf",
                    ContentType = "application/pdf"
                };
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Access denied while exporting photos to PDF");
                return null;
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "I/O error exporting photos to PDF");
                return null;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation while exporting photos to PDF");
                return null;
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid argument exporting photos to PDF");
                return null;
            }
        }

        public async Task<object> GetExportStatusAsync(string exportId)
        {
            await Task.Delay(100);

            return new
            {
                ExportId = exportId,
                Status = "completed",
                Message = "Export completed successfully"
            };
        }

        private async Task<List<StudentPhoto>> GetStudentsForExport(PhotoExportRequest request)
        {
            if (!string.IsNullOrEmpty(request.ClassLevel))
            {
                _logger.LogDebug("Fetching students by class level: {ClassLevel}, IncludeRoss={IncludeRoss}",
                    LogSanitizer.SanitizeString(request.ClassLevel), request.IncludeRossStudents);
                var students = await _studentGroupService.GetStudentsByClassLevelAsync(
                    request.ClassLevel,
                    request.IncludeRossStudents);
                _logger.LogDebug("Found {Count} students for class level {ClassLevel}", students.Count, LogSanitizer.SanitizeString(request.ClassLevel));
                return students;
            }
            else if (!string.IsNullOrEmpty(request.GroupType) && !string.IsNullOrEmpty(request.GroupId))
            {
                _logger.LogDebug("Fetching students by group: {GroupType}/{GroupId}",
                    LogSanitizer.SanitizeString(request.GroupType), LogSanitizer.SanitizeString(request.GroupId));
                var students = await _studentGroupService.GetStudentsByGroupAsync(
                    request.GroupType,
                    request.GroupId);
                _logger.LogDebug("Found {Count} students for group {GroupType}/{GroupId}",
                    students.Count, LogSanitizer.SanitizeString(request.GroupType), LogSanitizer.SanitizeString(request.GroupId));
                return students;
            }
            else if (!string.IsNullOrEmpty(request.TermCode) && !string.IsNullOrEmpty(request.Crn))
            {
                _logger.LogDebug("Fetching students by course: {TermCode}/{Crn}",
                    LogSanitizer.SanitizeString(request.TermCode), LogSanitizer.SanitizeString(request.Crn));
                var students = await _studentGroupService.GetStudentsByCourseAsync(
                    request.TermCode,
                    request.Crn,
                    request.IncludeRossStudents);
                _logger.LogDebug("Found {Count} students for course {TermCode}/{Crn}",
                    students.Count,
                    LogSanitizer.SanitizeString(request.TermCode),
                    LogSanitizer.SanitizeString(request.Crn));
                return students;
            }

            _logger.LogWarning("No valid export parameters provided (ClassLevel and GroupType/GroupId are both empty)");
            return new List<StudentPhoto>();
        }

        private async Task<string> GetExportTitleAsync(PhotoExportRequest request)
        {
            var titleParts = new List<string>();

            // Add class year information if available
            if (!string.IsNullOrEmpty(request.ClassLevel))
            {
                // Get the class year for the class level
                var classYears = await _termCodeService.GetActiveClassYears();
                var classLevelIndex = request.ClassLevel switch
                {
                    "V4" => 0,
                    "V3" => 1,
                    "V2" => 2,
                    "V1" => 3,
                    _ => -1
                };

                if (classLevelIndex >= 0 && classLevelIndex < classYears.Count)
                {
                    var year = classYears[classLevelIndex];
                    titleParts.Add($"Class of {year} ({request.ClassLevel})");
                }
                else
                {
                    titleParts.Add($"Class {request.ClassLevel}");
                }
            }

            // Add group information if available
            if (!string.IsNullOrEmpty(request.GroupType) && !string.IsNullOrEmpty(request.GroupId))
            {
                var groupTypeLabel = request.GroupType switch
                {
                    "eighths" => "Eighths",
                    "twentieths" => "Twentieths",
                    "teams" => "Team",
                    "v3specialty" => "Stream",
                    _ => request.GroupType
                };
                titleParts.Add($"{groupTypeLabel} {request.GroupId}");
            }

            // Add course information if available
            if (!string.IsNullOrEmpty(request.TermCode) && !string.IsNullOrEmpty(request.Crn))
            {
                try
                {
                    var termLabel = await _termCodeService.GetTermDescriptionAsync(request.TermCode);
                    var courseInfo = await _courseService.GetCourseInfoAsync(request.TermCode, request.Crn);
                    if (courseInfo != null)
                    {
                        titleParts.Add($"{courseInfo.SubjectCode}{courseInfo.CourseNumber} - {courseInfo.Title}");
                        titleParts.Add(termLabel);
                    }
                    else
                    {
                        // Fallback if course info not found
                        titleParts.Add($"Course {request.Crn}");
                        titleParts.Add(termLabel);
                    }
                }
                catch (ArgumentException ex)
                {
                    _logger.LogWarning(ex, "Invalid term code format for {TermCode}/{Crn}",
                        LogSanitizer.SanitizeString(request.TermCode), LogSanitizer.SanitizeString(request.Crn));
                    titleParts.Add($"Course {request.Crn}");
                }
                catch (KeyNotFoundException ex)
                {
                    _logger.LogWarning(ex, "Term code not found for {TermCode}/{Crn}",
                        LogSanitizer.SanitizeString(request.TermCode), LogSanitizer.SanitizeString(request.Crn));
                    // Fallback to basic course info without term label
                    titleParts.Add($"Course {request.Crn}");
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogWarning(ex, "Invalid operation getting course info for {TermCode}/{Crn}",
                        LogSanitizer.SanitizeString(request.TermCode), LogSanitizer.SanitizeString(request.Crn));
                    // Try to at least get term label
                    try
                    {
                        var termLabel = await _termCodeService.GetTermDescriptionAsync(request.TermCode);
                        titleParts.Add($"Course {request.Crn}");
                        titleParts.Add(termLabel);
                    }
                    catch (ArgumentException)
                    {
                        titleParts.Add($"Course {request.Crn}");
                    }
                    catch (KeyNotFoundException)
                    {
                        titleParts.Add($"Course {request.Crn}");
                    }
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogWarning(ex, "Database error getting course info for {TermCode}/{Crn}",
                        LogSanitizer.SanitizeString(request.TermCode), LogSanitizer.SanitizeString(request.Crn));
                    // Try to at least get term label
                    try
                    {
                        var termLabel = await _termCodeService.GetTermDescriptionAsync(request.TermCode);
                        titleParts.Add($"Course {request.Crn}");
                        titleParts.Add(termLabel);
                    }
                    catch (ArgumentException)
                    {
                        titleParts.Add($"Course {request.Crn}");
                    }
                    catch (KeyNotFoundException)
                    {
                        titleParts.Add($"Course {request.Crn}");
                    }
                }
            }

            var title = titleParts.Count > 0
                ? string.Join(" - ", titleParts)
                : "Student Photos";

            if (request.IncludeRossStudents)
            {
                title += " (including Ross Students)";
            }

            // Add export date
            title += $"\nAs of {DateTime.Now:M/d/yyyy}";

            return title;
        }
    }
}
