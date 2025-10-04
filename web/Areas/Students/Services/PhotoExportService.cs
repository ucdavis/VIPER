using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Viper.Areas.Students.Models;
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

        public PhotoExportService(
            IStudentGroupService studentGroupService,
            IPhotoService photoService,
            ILogger<PhotoExportService> logger,
            Curriculum.Services.TermCodeService termCodeService)
        {
            _studentGroupService = studentGroupService;
            _photoService = photoService;
            _logger = logger;
            _termCodeService = termCodeService;
        }

        public async Task<PhotoExportResult> ExportToWordAsync(PhotoExportRequest request)
        {
            try
            {
                _logger.LogInformation("ExportToWordAsync called with ClassLevel={ClassLevel}, GroupType={GroupType}, GroupId={GroupId}, IncludeRoss={IncludeRoss}",
                    request.ClassLevel, request.GroupType, request.GroupId, request.IncludeRossStudents);

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

                                var drawing = new DocumentFormat.OpenXml.Wordprocessing.Drawing(
                                    new DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline(
                                        new DocumentFormat.OpenXml.Drawing.Wordprocessing.Extent() { Cx = 950000L, Cy = 1267000L },
                                        new DocumentFormat.OpenXml.Drawing.Wordprocessing.EffectExtent() { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                                        new DocumentFormat.OpenXml.Drawing.Wordprocessing.DocProperties() { Id = (UInt32Value)1U, Name = $"Photo{i}" },
                                        new DocumentFormat.OpenXml.Drawing.Wordprocessing.NonVisualGraphicFrameDrawingProperties(
                                            new DocumentFormat.OpenXml.Drawing.GraphicFrameLocks() { NoChangeAspect = true }),
                                        new DocumentFormat.OpenXml.Drawing.Graphic(
                                            new DocumentFormat.OpenXml.Drawing.GraphicData(
                                                new DocumentFormat.OpenXml.Drawing.Pictures.Picture(
                                                    new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualPictureProperties(
                                                        new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualDrawingProperties() { Id = (UInt32Value)0U, Name = $"Photo{i}.jpg" },
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
                                    { DistanceFromTop = (UInt32Value)0U, DistanceFromBottom = (UInt32Value)0U, DistanceFromLeft = (UInt32Value)0U, DistanceFromRight = (UInt32Value)0U }
                                );

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting photos to Word");
                return null;
            }
        }

        public async Task<PhotoExportResult> ExportToPdfAsync(PhotoExportRequest request)
        {
            try
            {
                _logger.LogInformation("ExportToPdfAsync called with ClassLevel={ClassLevel}, GroupType={GroupType}, GroupId={GroupId}, IncludeRoss={IncludeRoss}",
                    request.ClassLevel, request.GroupType, request.GroupId, request.IncludeRossStudents);

                var students = await GetStudentsForExport(request);

                if (!students.Any())
                {
                    _logger.LogWarning("No students found for export request");
                    return null;
                }

                // Pre-load all photos in parallel with throttling
                _logger.LogInformation("Pre-loading photos for {Count} students", students.Count);
                var photoCache = new Dictionary<string, byte[]>();
                var semaphore = new SemaphoreSlim(10); // Limit to 10 concurrent I/O operations

                var photoTasks = students.Select(async student =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        var photoBytes = await _photoService.GetStudentPhotoAsync(student.MailId);
                        _logger.LogDebug("Student {MailId}: photoBytes is {Status}, Length = {Length}",
                            student.MailId,
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
                foreach (var result in photoResults)
                {
                    if (result.photoBytes != null && result.photoBytes.Length > 0)
                    {
                        photoCache[result.MailId] = result.photoBytes;
                    }
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting photos to PDF");
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
                _logger.LogInformation("Fetching students by class level: {ClassLevel}, IncludeRoss={IncludeRoss}",
                    request.ClassLevel, request.IncludeRossStudents);
                var students = await _studentGroupService.GetStudentsByClassLevelAsync(
                    request.ClassLevel,
                    request.IncludeRossStudents);
                _logger.LogInformation("Found {Count} students for class level {ClassLevel}", students.Count, request.ClassLevel);
                return students;
            }
            else if (!string.IsNullOrEmpty(request.GroupType) && !string.IsNullOrEmpty(request.GroupId))
            {
                _logger.LogInformation("Fetching students by group: {GroupType}/{GroupId}",
                    request.GroupType, request.GroupId);
                var students = await _studentGroupService.GetStudentsByGroupAsync(
                    request.GroupType,
                    request.GroupId);
                _logger.LogInformation("Found {Count} students for group {GroupType}/{GroupId}",
                    students.Count, request.GroupType, request.GroupId);
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
