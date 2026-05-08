using System.Text.RegularExpressions;
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
    /// <summary>
    /// Builds Word and PDF photo galleries for a class / course / student-group
    /// selection.
    /// </summary>
    public interface IPhotoExportService
    {
        /// <summary>
        /// Export student photos to a Word document (.docx). Layout is either
        /// the standard 6-up grid or, for grouped exports, a 3-up large layout.
        /// </summary>
        Task<PhotoExportResult?> ExportToWordAsync(PhotoExportRequest request);

        /// <summary>
        /// Export student photos to a PDF. Layout matches the Word variant.
        /// Returns null when no students match the request.
        /// </summary>
        Task<PhotoExportResult?> ExportToPdfAsync(PhotoExportRequest request);

        /// <summary>
        /// Export the tabular student list (no photos) to a PDF — mirrors the
        /// "Student List" tab on the gallery page. Used by the list-tab
        /// Print/PDF button so the saved file is a real, accessible PDF
        /// instead of a browser-rendered print capture.
        /// </summary>
        Task<PhotoExportResult?> ExportStudentListToPdfAsync(PhotoExportRequest request);
    }

    public class PhotoExportService : IPhotoExportService
    {
        // Source data (student names, group labels) occasionally contains stray
        // double-spaces from upstream systems. Microsoft's accessibility checker
        // and our own home-grown checker flag 3+ consecutive whitespace chars
        // ("RepeatedBlanks"); collapsing them on the way out is a safe no-op for
        // clean data and a structural improvement for messy data.
        private static readonly Regex RepeatedWhitespaceRegex = new(@"\s{2,}", RegexOptions.Compiled);

        private static string CollapseWhitespace(string? text) =>
            string.IsNullOrEmpty(text) ? string.Empty : RepeatedWhitespaceRegex.Replace(text, " ").Trim();

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

                // Resolve title up-front so the same string is used for both core
                // properties and the visible H1 paragraph.
                var titleLines = (await GetExportTitleAsync(request)).Split('\n');
                var documentTitle = titleLines[0];

                using var stream = new MemoryStream();
                using (var wordDocument = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
                {
                    var mainPart = wordDocument.AddMainDocumentPart();
                    mainPart.Document = new WordDocument();
                    var body = mainPart.Document.AppendChild(new Body());

                    WordAccessibilityHelper.EnsureAccessibilityStyles(mainPart);
                    WordAccessibilityHelper.EnsureModernCompatibility(mainPart);
                    WordAccessibilityHelper.SetCoreProperties(wordDocument, documentTitle, subject: "Student photo gallery export");

                    // Only show title and date for non-group exports.
                    // Use Heading1, not Title — accessibility checkers (Microsoft's
                    // built-in and our own check-docx-accessibility.ps1) recognise
                    // Heading1-9 as document headings; Word's "Title" style is a
                    // separate concept and doesn't satisfy the heading-styles rule.
                    if (!useLargeLayout)
                    {
                        body.AppendChild(WordAccessibilityHelper.CreateHeadingParagraph(
                            CollapseWhitespace(documentTitle), WordAccessibilityHelper.Heading1StyleId));

                        // Date subtitle is regular body text, not a heading — keeps the
                        // outline a single H1 instead of two competing top-level entries.
                        if (titleLines.Length > 1)
                        {
                            var dateParagraph = body.AppendChild(new Paragraph());
                            // Tight spacing keeps the date hugging the title like the
                            // original soft-break version before the H1 split.
                            var dateParaProps = dateParagraph.AppendChild(new ParagraphProperties());
                            dateParaProps.AppendChild(new SpacingBetweenLines
                            {
                                Before = "0",
                                After = "0",
                            });
                            var dateRun = dateParagraph.AppendChild(new Run());
                            dateRun.AppendChild(new Text(CollapseWhitespace(titleLines[1])));
                            var dateProps = dateRun.PrependChild(new RunProperties());
                            dateProps.AppendChild(new FontSize() { Val = "16" });
                            // Matches Quasar's grey-7 (the same theme color the on-page caption
                            // text uses); ≈4.6:1 contrast against white clears WCAG AA.
                            dateProps.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Color() { Val = "757575" });
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
                                if (ShouldShowGroupHeaders(groupedStudents))
                                {
                                    body.AppendChild(WordAccessibilityHelper.CreateHeadingParagraph(
                                        CollapseWhitespace($"{group.Key} ({chunkStart + 1}-{chunkEnd})"),
                                        WordAccessibilityHelper.Heading2StyleId));
                                }

                                // Create a borderless table for this chunk (layout table — photos in a grid)
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
                                WordAccessibilityHelper.MarkAsLayoutTable(chunkTable, "Photo gallery layout grid");

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

                                            // Alt text follows the rule from PLAN-Word-PDF-WCAG.md:
                                            // photos that convey identity get the student's displayed name.
                                            var altText = $"Photo of {student.GroupExportName}";

                                            var wpDocProps = new DocumentFormat.OpenXml.Drawing.Wordprocessing.DocProperties()
                                            {
                                                Id = (UInt32Value)imageId,
                                                Name = $"Photo{imageId}"
                                            };
                                            var picNonVisualProps = new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualDrawingProperties()
                                            {
                                                Id = (UInt32Value)imageId,
                                                Name = $"Photo{imageId}.jpg"
                                            };
                                            WordAccessibilityHelper.SetImageAltText(wpDocProps, picNonVisualProps, altText);

                                            var inline = new DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline(
                                                new DocumentFormat.OpenXml.Drawing.Wordprocessing.Extent() { Cx = layout.Width, Cy = layout.Height },
                                                new DocumentFormat.OpenXml.Drawing.Wordprocessing.EffectExtent() { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                                                wpDocProps,
                                                new DocumentFormat.OpenXml.Drawing.Wordprocessing.NonVisualGraphicFrameDrawingProperties(
                                                    new DocumentFormat.OpenXml.Drawing.GraphicFrameLocks() { NoChangeAspect = true }),
                                                new DocumentFormat.OpenXml.Drawing.Graphic(
                                                    new DocumentFormat.OpenXml.Drawing.GraphicData(
                                                        new DocumentFormat.OpenXml.Drawing.Pictures.Picture(
                                                            new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualPictureProperties(
                                                                picNonVisualProps,
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
                                        cellRun.AppendChild(new Text(CollapseWhitespace(student.GroupExportName)));
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
                        if (!isFirstGroup && ShouldShowGroupHeaders(groupedStudents))
                        {
                            var pageBreakPara = body.AppendChild(new Paragraph());
                            pageBreakPara.AppendChild(new Run(new Break() { Type = BreakValues.Page }));
                        }
                        isFirstGroup = false;

                        // Add group header if there are multiple groups
                        if (ShouldShowGroupHeaders(groupedStudents))
                        {
                            body.AppendChild(WordAccessibilityHelper.CreateHeadingParagraph(
                                CollapseWhitespace(GetGroupHeaderText(group.Key, groupStudents.Count)),
                                WordAccessibilityHelper.Heading2StyleId));
                        }

                        // Create a borderless table for this group (layout table — photos in a grid)
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
                        WordAccessibilityHelper.MarkAsLayoutTable(table, "Photo gallery layout grid");

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

                                    var displayedName = useLargeLayout ? student.GroupExportName : student.FullName;
                                    var altText = $"Photo of {displayedName}";

                                    var wpDocProps = new DocumentFormat.OpenXml.Drawing.Wordprocessing.DocProperties()
                                    {
                                        Id = (UInt32Value)imageId,
                                        Name = $"Photo{imageId}"
                                    };
                                    var picNonVisualProps = new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualDrawingProperties()
                                    {
                                        Id = (UInt32Value)imageId,
                                        Name = $"Photo{imageId}.jpg"
                                    };
                                    WordAccessibilityHelper.SetImageAltText(wpDocProps, picNonVisualProps, altText);

                                    var inline = new DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline(
                                        new DocumentFormat.OpenXml.Drawing.Wordprocessing.Extent() { Cx = layout.Width, Cy = layout.Height },
                                        new DocumentFormat.OpenXml.Drawing.Wordprocessing.EffectExtent() { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                                        wpDocProps,
                                        new DocumentFormat.OpenXml.Drawing.Wordprocessing.NonVisualGraphicFrameDrawingProperties(
                                            new DocumentFormat.OpenXml.Drawing.GraphicFrameLocks() { NoChangeAspect = true }),
                                        new DocumentFormat.OpenXml.Drawing.Graphic(
                                            new DocumentFormat.OpenXml.Drawing.GraphicData(
                                                new DocumentFormat.OpenXml.Drawing.Pictures.Picture(
                                                    new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualPictureProperties(
                                                        picNonVisualProps,
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
                                cellRun.AppendChild(new Text(CollapseWhitespace(useLargeLayout ? student.GroupExportName : student.FullName)));

                                // Only show secondary text (group info) if not using large layout
                                if (!useLargeLayout)
                                {
                                    foreach (var line in student.SecondaryTextLines)
                                    {
                                        cellRun.AppendChild(new Break());
                                        cellRun.AppendChild(new Text(CollapseWhitespace(line)));
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
                    FileName = await GetExportFilenameAsync(request, ".docx"),
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
                var documentTitle = titleLines[0];

                const int studentsPerPage = 9; // For large layouts: 3 rows × 3 students

                var document = PdfDocument.Create(container =>
                {
                    foreach (var group in groupedStudents)
                    {
                        var groupStudents = group.Value;
                        var totalStudents = groupStudents.Count;

                        // For large layouts with many students, split into chunks with repeating headers
                        if (useLargeLayout && totalStudents > studentsPerPage)
                        {
                            // Process group in chunks of studentsPerPage (9 students per page)
                            for (int chunkStart = 0; chunkStart < totalStudents; chunkStart += studentsPerPage)
                            {
                                var chunkEnd = Math.Min(chunkStart + studentsPerPage, totalStudents);
                                var chunkStudents = groupStudents.Skip(chunkStart).Take(chunkEnd - chunkStart).ToList();

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
                                            // Large layout intentionally has no on-page H1 — the
                                            // document title is exposed via core properties (see
                                            // WithAccessibility below) and the visual design has
                                            // never shown a title for grouped photo sheets.
                                            if (ShouldShowGroupHeaders(groupedStudents))
                                            {
                                                var headerText = $"{group.Key} ({chunkStart + 1}-{chunkEnd})";
                                                column.Item().SemanticHeader2().Text(headerText)
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

                                                for (int i = 0; i < chunkStudents.Count; i += layout.PerRow)
                                                {
                                                    var rowStudents = chunkStudents.Skip(i).Take(layout.PerRow).ToList();

                                                    foreach (var student in rowStudents)
                                                    {
                                                        table.Cell().Padding(5).Column(innerColumn =>
                                                        {
                                                            innerColumn.Spacing(2);

                                                            // Add photo from cache with explicit dimensions
                                                            if (photoCache.TryGetValue(student.MailId, out var photoBytes))
                                                            {
                                                                innerColumn.Item()
                                                                    .SemanticImage($"Photo of {student.GroupExportName}")
                                                                    .Width(1.74f, Unit.Inch).Height(2.26f, Unit.Inch).Image(photoBytes);
                                                            }

                                                            // Show student name (centered)
                                                            innerColumn.Item().AlignCenter().Text(student.GroupExportName)
                                                                .FontSize(10);
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
                                        .SemanticIgnore()
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
                        }
                        else
                        {
                            // Standard processing (no chunking needed)
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
                                        // Only show title and date for non-group exports
                                        if (!useLargeLayout)
                                        {
                                            column.Item().SemanticHeader1().Text(titleLines[0])
                                                .SemiBold()
                                                .FontSize(20)
                                                .FontColor(Colors.Black);

                                            if (titleLines.Length > 1)
                                            {
                                                column.Item().Text(titleLines[1])
                                                    .FontSize(10)
                                                    .FontColor(Colors.Grey.Darken1);
                                            }
                                        }

                                        // Add group header if there are multiple groups
                                        if (ShouldShowGroupHeaders(groupedStudents))
                                        {
                                            var headerText = GetGroupHeaderText(group.Key, groupStudents.Count);
                                            column.Item().PaddingTop(useLargeLayout ? 0 : 10).SemanticHeader2().Text(headerText)
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

                                            for (int i = 0; i < groupStudents.Count; i += layout.PerRow)
                                            {
                                                var rowStudents = groupStudents.Skip(i).Take(layout.PerRow).ToList();

                                                foreach (var student in rowStudents)
                                                {
                                                    table.Cell().Padding(5).Column(innerColumn =>
                                                    {
                                                        innerColumn.Spacing(2);

                                                        // Add photo from cache with explicit dimensions based on layout
                                                        if (photoCache.TryGetValue(student.MailId, out var photoBytes))
                                                        {
                                                            var displayedName = useLargeLayout ? student.GroupExportName : student.FullName;
                                                            var altText = $"Photo of {displayedName}";

                                                            if (useLargeLayout)
                                                            {
                                                                // Large layout: 1.74" x 2.26"
                                                                innerColumn.Item().SemanticImage(altText)
                                                                    .Width(1.74f, Unit.Inch).Height(2.26f, Unit.Inch).Image(photoBytes);
                                                            }
                                                            else
                                                            {
                                                                // Standard layout: 0.69" x 0.92"
                                                                innerColumn.Item().SemanticImage(altText)
                                                                    .Width(0.69f, Unit.Inch).Height(0.92f, Unit.Inch).Image(photoBytes);
                                                            }
                                                        }

                                                        // Show student name (centered)
                                                        innerColumn.Item().AlignCenter().Text(useLargeLayout ? student.GroupExportName : student.FullName)
                                                            .FontSize(10);

                                                        // Only show secondary text (group info) if not using large layout
                                                        if (!useLargeLayout)
                                                        {
                                                            foreach (var line in student.SecondaryTextLines)
                                                            {
                                                                innerColumn.Item().AlignCenter().Text(line)
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
                                    .SemanticIgnore()
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
                    }
                })
                .WithAccessibility(documentTitle, subject: "Student photo gallery export");

                var pdfBytes = document.GeneratePdf();

                return new PhotoExportResult
                {
                    ExportId = Guid.NewGuid().ToString(),
                    FileData = pdfBytes,
                    FileName = await GetExportFilenameAsync(request, ".pdf"),
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
        /// Exports the student list (no photos) as a tabular PDF. Mirrors the
        /// "Student List" tab on the gallery page so the Print/PDF button can
        /// download a real accessible PDF instead of a browser print capture.
        /// </summary>
        public async Task<PhotoExportResult?> ExportStudentListToPdfAsync(PhotoExportRequest request)
        {
            try
            {
                _logger.LogDebug("ExportStudentListToPdfAsync called with ClassLevel={ClassLevel}, IncludeRoss={IncludeRoss}",
                    LogSanitizer.SanitizeString(request.ClassLevel), request.IncludeRossStudents);

                var students = await GetStudentsForExport(request);
                if (!students.Any())
                {
                    _logger.LogWarning("No students found for student list PDF export");
                    return null;
                }

                var ordered = students.OrderBy(s => s.LastName).ThenBy(s => s.FirstName).ToList();

                var titleLines = (await GetExportTitleAsync(request)).Split('\n');
                var documentTitle = titleLines[0];
                var generatedLabel = titleLines.Length > 1 ? titleLines[1] : $"As of {DateTime.Now:M/d/yyyy}";
                var fullTitle = $"{documentTitle} - {ordered.Count} Student{(ordered.Count != 1 ? "s" : "")}";

                var document = PdfDocument.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.Letter);
                        page.Margin(0.5f, Unit.Inch);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(10));

                        page.Header().Column(col =>
                        {
                            col.Item().SemanticHeader1().Text(fullTitle).SemiBold().FontSize(16);
                            col.Item().PaddingTop(2).Text(generatedLabel).FontSize(9).FontColor(Colors.Grey.Darken1);
                        });

                        page.Content().PaddingVertical(0.5f, Unit.Centimetre).SemanticTable().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(40);     // #
                                columns.RelativeColumn(2);      // Name
                                columns.RelativeColumn(3);      // Email
                            });

                            var hdrStyle = TextStyle.Default.SemiBold().Underline();
                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).Text("#").Style(hdrStyle);
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).Text("Name").Style(hdrStyle);
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).Text("Email").Style(hdrStyle);
                            });

                            for (int i = 0; i < ordered.Count; i++)
                            {
                                var s = ordered[i];
                                var rowBg = i % 2 == 0 ? "#FFFFFF" : "#FAFAFA";
                                var fullName = !string.IsNullOrWhiteSpace(s.LastName) || !string.IsNullOrWhiteSpace(s.FirstName)
                                    ? $"{s.LastName}, {s.FirstName}".Trim(',', ' ')
                                    : s.GroupExportName;
                                var email = !string.IsNullOrWhiteSpace(s.MailId) ? $"{s.MailId}@ucdavis.edu" : "—";

                                table.Cell().Background(rowBg).PaddingVertical(3).PaddingHorizontal(4).Text((i + 1).ToString());
                                table.Cell().Background(rowBg).PaddingVertical(3).PaddingHorizontal(4).Text(CollapseWhitespace(fullName));
                                table.Cell().Background(rowBg).PaddingVertical(3).PaddingHorizontal(4).Text(CollapseWhitespace(email));
                            }
                        });

                        page.Footer().SemanticIgnore().AlignCenter().Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                    });
                })
                .WithAccessibility(fullTitle, subject: "Student list export");

                var pdfBytes = document.GeneratePdf();

                return new PhotoExportResult
                {
                    ExportId = Guid.NewGuid().ToString(),
                    FileData = pdfBytes,
                    FileName = await GetExportFilenameAsync(request, ".pdf"),
                    ContentType = "application/pdf"
                };
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Access denied exporting student list to PDF");
                return null;
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "I/O error exporting student list to PDF");
                return null;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation exporting student list to PDF");
                return null;
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid argument exporting student list to PDF");
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

        /// <summary>
        /// Determines if group headers should be displayed.
        /// Group headers are shown when there are multiple groups being exported.
        /// </summary>
        private static bool ShouldShowGroupHeaders(Dictionary<string, List<StudentPhoto>> groupedStudents)
        {
            return groupedStudents.Count > 1;
        }

        /// <summary>
        /// Generates the header text for a group, showing the group name and student count.
        /// </summary>
        private static string GetGroupHeaderText(string groupKey, int studentCount)
        {
            return $"{groupKey} ({studentCount})";
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

        /// <summary>
        /// Generates a descriptive filename for the export based on context (course, class level, group).
        /// </summary>
        private async Task<string> GetExportFilenameAsync(PhotoExportRequest request, string extension)
        {
            var filenameParts = new List<string> { "Student Groups" };

            // Add course code or class level
            if (!string.IsNullOrEmpty(request.TermCode) && !string.IsNullOrEmpty(request.Crn))
            {
                try
                {
                    var courseInfo = await _courseService.GetCourseInfoAsync(request.TermCode, request.Crn);
                    if (courseInfo != null)
                    {
                        filenameParts.Add($"{courseInfo.SubjectCode}{courseInfo.CourseNumber}");
                    }
                }
                catch (ArgumentException ex)
                {
                    _logger.LogWarning(ex, "Invalid argument when getting course info for filename generation");
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogWarning(ex, "Invalid operation when getting course info for filename generation");
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogWarning(ex, "Database error when getting course info for filename generation");
                }
            }
            else if (!string.IsNullOrEmpty(request.ClassLevel))
            {
                filenameParts.Add(request.ClassLevel);
            }

            // Add group type if grouping is enabled
            if (!string.IsNullOrEmpty(request.GroupType) && string.IsNullOrEmpty(request.GroupId))
            {
                // When GroupType is set but GroupId is not, we're showing all groups of that type
                filenameParts.Add(request.GroupType.ToLower());
            }
            else if (!string.IsNullOrEmpty(request.GroupId))
            {
                // When filtering by a specific group, include the group ID
                var groupTypeLabel = request.GroupType?.ToLower() ?? "group";
                filenameParts.Add($"{groupTypeLabel} {request.GroupId}");
            }

            if (!string.IsNullOrWhiteSpace(request.View))
            {
                filenameParts.Add(request.View.Trim().ToLowerInvariant());
            }

            // Build filename with spaces (not underscores) and add extension
            var filename = string.Join(" ", filenameParts) + extension;

            return filename;
        }
    }
}
