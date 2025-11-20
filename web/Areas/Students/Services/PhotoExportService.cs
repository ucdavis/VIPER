using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Viper.Areas.Curriculum.Services;
using Viper.Areas.Students.Models;
using Viper.Classes.Utilities;
using WordDocument = DocumentFormat.OpenXml.Wordprocessing.Document;
using PdfDocument = QuestPDF.Fluent.Document;

namespace Viper.Areas.Students.Services
{
    public interface IPhotoExportService
    {
        Task<PhotoExportResult?> ExportToWordAsync(PhotoExportRequest request);
        Task<PhotoExportResult?> ExportToPdfAsync(PhotoExportRequest request);
    }

    public class PhotoExportService : IPhotoExportService
    {
        // Photo dimensions in EMUs (English Metric Units) for Word/PDF export
        // EMU = 1/914400 inch

        // Standard layout: 6 photos per row, approximately 0.69" x 0.92"
        private const long StandardPhotoWidthEmu = 633333L;
        private const long StandardPhotoHeightEmu = 844667L;
        private const int StandardPhotosPerRow = 6;

        // Group-filtered layout: 3 photos per row, matching legacy at 1.74" x 2.26" (portrait)
        private const long LargePhotoWidthEmu = 1591086L;   // 1.74 inches
        private const long LargePhotoHeightEmu = 2066514L;  // 2.26 inches
        private const int LargePhotosPerRow = 3;

        // Timestamp format for export filenames
        private const string ExportFilenameTimestampFormat = "yyyyMMddHHmmss";

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

        /// <summary>
        /// Exports student photos to a formatted Word document with optional grouping by class, course, or student groups.
        /// </summary>
        /// <param name="request">Export request containing student selection criteria and options</param>
        /// <returns>PhotoExportResult containing the Word document bytes and metadata, or null if no students found</returns>
#pragma warning disable S3220 // OpenXML SDK uses params overloads by design for fluent object construction
        public async Task<PhotoExportResult?> ExportToWordAsync(PhotoExportRequest request)
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

                // Load all photos upfront for faster rendering
                var photoCache = await BatchLoadPhotosAsync(students);

                // Determine if we should group: GroupType is provided but GroupId is not (meaning "All <group type>")
                var shouldGroup = !string.IsNullOrEmpty(request.GroupType) && string.IsNullOrEmpty(request.GroupId);
                var groupedStudents = shouldGroup ? GroupStudentsByType(students, request.GroupType) : GroupStudentsByType(students, null);

                // Get layout configuration (standard or large)
                var layout = GetLayoutConfiguration(request);
                var useLargeLayout = ShouldUseLargeLayout(request);

                using var stream = new MemoryStream();
                using (var wordDocument = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
                {
                    var mainPart = wordDocument.AddMainDocumentPart();
                    mainPart.Document = new WordDocument();
                    var body = mainPart.Document.AppendChild(new Body());

                    // Only show title and date for non-group exports
                    if (!useLargeLayout)
                    {
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
                    }

                    uint imageId = 1;
                    bool isFirstGroup = true;
                    const int studentsPerPage = 9; // For large layouts: 3 rows × 3 students

                    // Iterate over groups
                    foreach (var group in groupedStudents)
                    {
                        var groupStudents = group.Value;
                        var totalStudents = groupStudents.Count;

                        // For large layouts with many students, split into chunks with repeating headers
                        if (useLargeLayout && totalStudents > studentsPerPage)
                        {
                            // Process group in chunks of studentsPerPage (9 students per "page")
                            for (int chunkStart = 0; chunkStart < totalStudents; chunkStart += studentsPerPage)
                            {
                                // Add page break before each chunk (except the very first one)
                                if (!isFirstGroup)
                                {
                                    var pageBreakPara = body.AppendChild(new Paragraph());
                                    pageBreakPara.AppendChild(new Run(new Break() { Type = BreakValues.Page }));
                                }
                                isFirstGroup = false;

                                var chunkEnd = Math.Min(chunkStart + studentsPerPage, totalStudents);
                                var chunkStudents = groupStudents.Skip(chunkStart).Take(chunkEnd - chunkStart).ToList();

                                // Add group header with student range numbering
                                if (groupedStudents.Count > 1)
                                {
                                    var groupHeaderPara = body.AppendChild(new Paragraph());
                                    var groupHeaderRun = groupHeaderPara.AppendChild(new Run());
                                    groupHeaderRun.AppendChild(new Text($"{group.Key} ({chunkStart + 1}-{chunkEnd})"));
                                    var groupHeaderProps = groupHeaderRun.PrependChild(new RunProperties());
                                    groupHeaderProps.AppendChild(new Bold());
                                    groupHeaderProps.AppendChild(new FontSize() { Val = "24" });
                                }

                                // Create a borderless table for this chunk
                                var chunkTable = body.AppendChild(new Table());
                                var chunkTableProps = chunkTable.AppendChild(new TableProperties());
                                chunkTableProps.AppendChild(new TableBorders(
                                    new TopBorder() { Val = new EnumValue<BorderValues>(BorderValues.None), Size = 0 },
                                    new BottomBorder() { Val = new EnumValue<BorderValues>(BorderValues.None), Size = 0 },
                                    new LeftBorder() { Val = new EnumValue<BorderValues>(BorderValues.None), Size = 0 },
                                    new RightBorder() { Val = new EnumValue<BorderValues>(BorderValues.None), Size = 0 },
                                    new InsideHorizontalBorder() { Val = new EnumValue<BorderValues>(BorderValues.None), Size = 0 },
                                    new InsideVerticalBorder() { Val = new EnumValue<BorderValues>(BorderValues.None), Size = 0 }
                                ));

                                // Render rows for this chunk
                                for (int i = 0; i < chunkStudents.Count; i += layout.PerRow)
                                {
                                    var row = chunkTable.AppendChild(new TableRow());
                                    var rowStudents = chunkStudents.Skip(i).Take(layout.PerRow).ToList();

                                    foreach (var student in rowStudents)
                                    {
                                        var cell = row.AppendChild(new TableCell());

                                        // Add photo from cache (already batch loaded)
                                        if (photoCache.TryGetValue(student.MailId, out var photoBytes) && photoBytes != null && photoBytes.Length > 0)
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
                                                new DocumentFormat.OpenXml.Drawing.Wordprocessing.Extent() { Cx = layout.Width, Cy = layout.Height },
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
                                                                    new DocumentFormat.OpenXml.Drawing.Extents() { Cx = layout.Width, Cy = layout.Height }),
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

                                        // Add name (no secondary text for large layouts)
                                        var cellParagraph = cell.AppendChild(new Paragraph());
                                        var cellParagraphProps = cellParagraph.AppendChild(new ParagraphProperties());
                                        cellParagraphProps.AppendChild(new Justification() { Val = JustificationValues.Center });
                                        var cellRun = cellParagraph.AppendChild(new Run());
                                        cellRun.AppendChild(new Text(useLargeLayout ? student.GroupExportName : student.FullName));
                                    }

                                    for (int j = rowStudents.Count; j < layout.PerRow; j++)
                                    {
                                        row.AppendChild(new TableCell(new Paragraph()));
                                    }
                                }
                            }
                            continue; // Skip normal processing for this group
                        }

                        // Standard processing (no chunking needed)
                        // Add page break before each group (except the first one)
                        if (!isFirstGroup && groupedStudents.Count > 1)
                        {
                            var pageBreakPara = body.AppendChild(new Paragraph());
                            pageBreakPara.AppendChild(new Run(new Break() { Type = BreakValues.Page }));
                        }
                        isFirstGroup = false;

                        // Add group header if there are multiple groups
                        if (groupedStudents.Count > 1)
                        {
                            var groupHeaderPara = body.AppendChild(new Paragraph());
                            var groupHeaderRun = groupHeaderPara.AppendChild(new Run());

                            // For large layouts, just show group name; for standard, show count
                            var headerText = useLargeLayout ? group.Key : $"{group.Key} ({group.Value.Count})";
                            groupHeaderRun.AppendChild(new Text(headerText));

                            var groupHeaderProps = groupHeaderRun.PrependChild(new RunProperties());
                            groupHeaderProps.AppendChild(new Bold());
                            groupHeaderProps.AppendChild(new FontSize() { Val = "24" });
                        }

                        // Create a borderless table for this group
                        var table = body.AppendChild(new Table());
                        var tableProps = table.AppendChild(new TableProperties());
                        tableProps.AppendChild(new TableBorders(
                            new TopBorder() { Val = new EnumValue<BorderValues>(BorderValues.None), Size = 0 },
                            new BottomBorder() { Val = new EnumValue<BorderValues>(BorderValues.None), Size = 0 },
                            new LeftBorder() { Val = new EnumValue<BorderValues>(BorderValues.None), Size = 0 },
                            new RightBorder() { Val = new EnumValue<BorderValues>(BorderValues.None), Size = 0 },
                            new InsideHorizontalBorder() { Val = new EnumValue<BorderValues>(BorderValues.None), Size = 0 },
                            new InsideVerticalBorder() { Val = new EnumValue<BorderValues>(BorderValues.None), Size = 0 }
                        ));

                        for (int i = 0; i < groupStudents.Count; i += layout.PerRow)
                        {
                            var row = table.AppendChild(new TableRow());
                            var rowStudents = groupStudents.Skip(i).Take(layout.PerRow).ToList();

                            foreach (var student in rowStudents)
                            {
                                var cell = row.AppendChild(new TableCell());

                                // Add photo from cache (already batch loaded)
                                if (photoCache.TryGetValue(student.MailId, out var photoBytes) && photoBytes != null && photoBytes.Length > 0)
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
                                        new DocumentFormat.OpenXml.Drawing.Wordprocessing.Extent() { Cx = layout.Width, Cy = layout.Height },
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
                                                            new DocumentFormat.OpenXml.Drawing.Extents() { Cx = layout.Width, Cy = layout.Height }),
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

                                // Add name and optionally secondary text (hide group info for large layouts)
                                var cellParagraph = cell.AppendChild(new Paragraph());
                                var cellParagraphProps = cellParagraph.AppendChild(new ParagraphProperties());
                                cellParagraphProps.AppendChild(new Justification() { Val = JustificationValues.Center });
                                var cellRun = cellParagraph.AppendChild(new Run());
                                cellRun.AppendChild(new Text(useLargeLayout ? student.GroupExportName : student.FullName));

                                // Only show secondary text (group info) if not using large layout
                                if (!useLargeLayout)
                                {
                                    foreach (var line in student.SecondaryTextLines)
                                    {
                                        cellRun.AppendChild(new Break());
                                        cellRun.AppendChild(new Text(line));
                                    }
                                }
                            }

                            for (int j = rowStudents.Count; j < layout.PerRow; j++)
                            {
                                row.AppendChild(new TableCell(new Paragraph()));
                            }
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
                    FileName = $"StudentPhotos_{DateTime.Now.ToString(ExportFilenameTimestampFormat)}.docx",
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
#pragma warning restore S3220

        /// <summary>
        /// Exports student photos to a formatted PDF document with optional grouping by class, course, or student groups.
        /// </summary>
        /// <param name="request">Export request containing student selection criteria and options</param>
        /// <returns>PhotoExportResult containing the PDF document bytes and metadata, or null if no students found</returns>
        public async Task<PhotoExportResult?> ExportToPdfAsync(PhotoExportRequest request)
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

                // Load all photos upfront for faster rendering
                var photoCache = await BatchLoadPhotosAsync(students);

                // Determine if we should group
                var shouldGroup = !string.IsNullOrEmpty(request.GroupType) && string.IsNullOrEmpty(request.GroupId);
                var groupedStudents = shouldGroup ? GroupStudentsByType(students, request.GroupType) : GroupStudentsByType(students, null);

                // Get layout configuration (standard or large)
                var layout = GetLayoutConfiguration(request);
                var useLargeLayout = ShouldUseLargeLayout(request);

                // Fetch title before entering QuestPDF builder to avoid async void lambda
                var titleLines = (await GetExportTitleAsync(request)).Split('\n');

                QuestPDF.Settings.License = LicenseType.Community;

                var document = PdfDocument.Create(container =>
                {
                    foreach (var group in groupedStudents)
                    {
                        container.Page(page =>
                        {
                            page.Size(PageSizes.Letter);
                            page.MarginTop(1, Unit.Inch);
                            page.MarginLeft(1, Unit.Inch);
                            page.MarginRight(1, Unit.Inch);
                            page.MarginBottom(0.25f, Unit.Inch);
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

                                    // Add group header if there are multiple groups
                                    if (groupedStudents.Count > 1)
                                    {
                                        column.Item().PaddingTop(10).Text($"{group.Key} ({group.Value.Count})")
                                            .SemiBold()
                                            .FontSize(16)
                                            .FontColor(Colors.Black);
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
                                            for (int col = 0; col < layout.PerRow; col++)
                                            {
                                                columns.RelativeColumn();
                                            }
                                        });

                                        var groupStudents = group.Value;
                                        for (int i = 0; i < groupStudents.Count; i += layout.PerRow)
                                        {
                                            var rowStudents = groupStudents.Skip(i).Take(layout.PerRow).ToList();

                                            foreach (var student in rowStudents)
                                            {
                                                table.Cell().Padding(5).ShowOnce().Column(innerColumn =>
                                                {
                                                    innerColumn.Spacing(2);

                                                    // Add photo from cache
                                                    if (photoCache.TryGetValue(student.MailId, out var photoBytes))
                                                    {
                                                        innerColumn.Item().Image(photoBytes).FitWidth();
                                                    }

                                                    // Show student name
                                                    innerColumn.Item().Text(useLargeLayout ? student.GroupExportName : student.FullName)
                                                        .FontSize(10);

                                                    // Only show secondary text (group info) if not using large layout
                                                    if (!useLargeLayout)
                                                    {
                                                        foreach (var line in student.SecondaryTextLines)
                                                        {
                                                            innerColumn.Item().Text(line)
                                                                .FontSize(8)
                                                                .FontColor(Colors.Grey.Darken1);
                                                        }
                                                    }
                                                });
                                            }

                                            for (int j = rowStudents.Count; j < layout.PerRow; j++)
                                            {
                                                table.Cell().Element(Block);
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
                    }
                });

                var pdfBytes = document.GeneratePdf();

                return new PhotoExportResult
                {
                    ExportId = Guid.NewGuid().ToString(),
                    FileData = pdfBytes,
                    FileName = $"StudentPhotos_{DateTime.Now.ToString(ExportFilenameTimestampFormat)}.pdf",
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

        /// <summary>
        /// Loads all student photos upfront in parallel for faster export generation.
        /// Returns a dictionary keyed by student MailId for quick lookup during rendering.
        /// </summary>
        private async Task<Dictionary<string, byte[]>> BatchLoadPhotosAsync(List<StudentPhoto> students)
        {
            var photoCache = new Dictionary<string, byte[]>();

            // Load all photos in parallel
            var photoTasks = students
                .Where(s => !string.IsNullOrWhiteSpace(s.MailId))
                .Select(async s =>
                {
                    try
                    {
                        var photoBytes = await _photoService.GetStudentPhotoAsync(s.MailId);
                        return new { MailId = s.MailId, PhotoBytes = (byte[]?)photoBytes };
                    }
                    catch (IOException ex)
                    {
                        _logger.LogWarning(ex, "IO error loading photo for student {MailId}", LogSanitizer.SanitizeId(s.MailId));
                        return new { MailId = s.MailId, PhotoBytes = (byte[]?)null };
                    }
                    catch (HttpRequestException ex)
                    {
                        _logger.LogWarning(ex, "HTTP error loading photo for student {MailId}", LogSanitizer.SanitizeId(s.MailId));
                        return new { MailId = s.MailId, PhotoBytes = (byte[]?)null };
                    }
                    catch (InvalidOperationException ex)
                    {
                        _logger.LogWarning(ex, "Invalid operation loading photo for student {MailId}", LogSanitizer.SanitizeId(s.MailId));
                        return new { MailId = s.MailId, PhotoBytes = (byte[]?)null };
                    }
                });

            var photos = await Task.WhenAll(photoTasks);

            foreach (var photo in photos.Where(p => p.PhotoBytes != null && p.PhotoBytes.Length > 0))
            {
                photoCache[photo.MailId] = photo.PhotoBytes!;
            }

            _logger.LogDebug("Batch loaded {Count} photos for {TotalStudents} students", photoCache.Count, students.Count);
            return photoCache;
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

        /// <summary>
        /// Groups students by the specified group type and sorts them alphabetically within each group.
        /// Students without a group assignment are placed in an "Unassigned" group.
        /// </summary>
        /// <param name="students">List of students to group</param>
        /// <param name="groupType">Type of grouping: "eighths", "twentieths", "teams", "v3specialty", or null for no grouping</param>
        /// <returns>Dictionary mapping group names to sorted lists of students</returns>
        private static Dictionary<string, List<StudentPhoto>> GroupStudentsByType(List<StudentPhoto> students, string? groupType)
        {
            var grouped = new Dictionary<string, List<StudentPhoto>>();

            if (string.IsNullOrEmpty(groupType))
            {
                // No grouping - return all students in a single group
                grouped["All Students"] = students.OrderBy(s => s.LastName).ThenBy(s => s.FirstName).ToList();
                return grouped;
            }

            // Group students by the specified field
            foreach (var student in students)
            {
                var groupValue = groupType.ToLower() switch
                {
                    "eighths" => student.EighthsGroup,
                    "twentieths" => student.TwentiethsGroup,
                    "teams" => student.TeamNumber,
                    "v3specialty" => student.V3SpecialtyGroup,
                    _ => null
                };

                var groupName = string.IsNullOrEmpty(groupValue) ? "Unassigned" : groupValue;

                if (!grouped.ContainsKey(groupName))
                {
                    grouped[groupName] = new List<StudentPhoto>();
                }
                grouped[groupName].Add(student);
            }

            // Sort students within each group by last name, then first name
            foreach (var group in grouped.Keys.ToList())
            {
                grouped[group] = grouped[group].OrderBy(s => s.LastName).ThenBy(s => s.FirstName).ToList();
            }

            // Return groups sorted (put "Unassigned" last, otherwise alphabetically)
            return grouped
                .OrderBy(kvp => kvp.Key == "Unassigned" ? 1 : 0)
                .ThenBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        /// <summary>
        /// Determines if the export should use the large photo layout (3x3 grid) based on the group type.
        /// </summary>
        private static bool ShouldUseLargeLayout(PhotoExportRequest request)
        {
            if (string.IsNullOrEmpty(request.GroupType))
            {
                return false;
            }

            var groupType = request.GroupType.ToLower();
            return groupType == "eighths" || groupType == "twentieths" || groupType == "teams";
        }

        /// <summary>
        /// Gets the photo dimensions and photos-per-row configuration based on the export request.
        /// </summary>
        private static (long Width, long Height, int PerRow) GetLayoutConfiguration(PhotoExportRequest request)
        {
            if (ShouldUseLargeLayout(request))
            {
                return (LargePhotoWidthEmu, LargePhotoHeightEmu, LargePhotosPerRow);
            }
            return (StandardPhotoWidthEmu, StandardPhotoHeightEmu, StandardPhotosPerRow);
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
                    // Use static method instead of database lookup
                    var termLabel = int.TryParse(request.TermCode, out var termCodeInt)
                        ? TermCodeService.GetTermCodeDescription(termCodeInt)
                        : request.TermCode; // Fallback to raw term code if parse fails

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
                catch (InvalidOperationException ex)
                {
                    _logger.LogWarning(ex, "Invalid operation getting course info for {TermCode}/{Crn}",
                        LogSanitizer.SanitizeString(request.TermCode), LogSanitizer.SanitizeString(request.Crn));
                    titleParts.Add($"Course {request.Crn}");
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogWarning(ex, "Database error getting course info for {TermCode}/{Crn}",
                        LogSanitizer.SanitizeString(request.TermCode), LogSanitizer.SanitizeString(request.Crn));
                    titleParts.Add($"Course {request.Crn}");
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
