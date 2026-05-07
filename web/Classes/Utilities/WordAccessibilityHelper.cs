using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DrawingNonVisualPicture = DocumentFormat.OpenXml.Drawing.Pictures.NonVisualDrawingProperties;
using DrawingDocProperties = DocumentFormat.OpenXml.Drawing.Wordprocessing.DocProperties;

namespace Viper.Classes.Utilities;

/// <summary>
/// OpenXML helpers that add the structural accessibility features assistive
/// technology expects in tagged Word documents — heading styles, image alt
/// text, table-header rows, document language, and core file properties.
/// Pair these with the existing OpenXML calls in *ExportService.cs files to
/// satisfy WCAG 2.1 SC 1.3.1 / Word Accessibility Checker expectations.
/// </summary>
#pragma warning disable S3220 // OpenXML SDK uses params overloads by design for fluent object construction
public static class WordAccessibilityHelper
{
    public const string DefaultLanguage = "en-US";
    public const string DefaultCreator = "UC Davis School of Veterinary Medicine";

    /// <summary>
    /// Built-in Word style identifier for the document title.
    /// </summary>
    public const string TitleStyleId = "Title";

    /// <summary>Built-in Word style identifier for first-level heading.</summary>
    public const string Heading1StyleId = "Heading1";

    /// <summary>Built-in Word style identifier for second-level heading.</summary>
    public const string Heading2StyleId = "Heading2";

    /// <summary>Built-in Word style identifier for third-level heading.</summary>
    public const string Heading3StyleId = "Heading3";

    /// <summary>
    /// Add a styles part containing the heading and title style definitions
    /// the document references. Without this, paragraphs that point to
    /// "Heading1" / "Title" via <see cref="ParagraphStyleId"/> are treated
    /// as Normal by Word — defeating the structural intent.
    /// Also sets the default document language so screen readers pronounce
    /// content correctly.
    /// Intended for documents being built fresh; this overwrites any existing
    /// <see cref="DocDefaults"/> and is not safe to call on a template-derived
    /// document.
    /// </summary>
    public static void EnsureAccessibilityStyles(
        MainDocumentPart mainPart,
        string language = DefaultLanguage)
    {
        var stylesPart = mainPart.StyleDefinitionsPart
            ?? mainPart.AddNewPart<StyleDefinitionsPart>();

        // Reuse the existing Styles tree when present. Reassigning
        // stylesPart.Styles to a tree that's already attached throws on
        // some OpenXML SDK versions; only set it on a fresh part.
        var existingStyles = stylesPart.Styles;
        var styles = existingStyles ?? new Styles();

        // Document defaults — sets baseline language for the whole document.
        var docDefaults = new DocDefaults(
            new RunPropertiesDefault(
                new RunPropertiesBaseStyle(
                    new Languages { Val = language }
                )
            ),
            new ParagraphPropertiesDefault(
                new ParagraphPropertiesBaseStyle()
            )
        );

        styles.RemoveAllChildren<DocDefaults>();
        styles.AppendChild(docDefaults);

        AddStyleIfMissing(styles, TitleStyleId, "Title",
            fontSize: "40", bold: true);
        AddStyleIfMissing(styles, Heading1StyleId, "Heading 1",
            fontSize: "32", bold: true, outlineLevel: 0);
        AddStyleIfMissing(styles, Heading2StyleId, "Heading 2",
            fontSize: "26", bold: true, outlineLevel: 1);
        AddStyleIfMissing(styles, Heading3StyleId, "Heading 3",
            fontSize: "22", bold: true, outlineLevel: 2);

        if (existingStyles is null)
        {
            stylesPart.Styles = styles;
        }
        styles.Save(stylesPart);
    }

    /// <summary>
    /// Mark the document as Word 2013+ (compatibility mode 15) so Word does
    /// NOT open it in legacy compatibility mode. Without this setting, Word
    /// shows the title-bar message "This document is in an older format with
    /// limited functionality" and the Accessibility Checker is unavailable.
    /// </summary>
    public static void EnsureModernCompatibility(MainDocumentPart mainPart)
    {
        var settingsPart = mainPart.DocumentSettingsPart
            ?? mainPart.AddNewPart<DocumentSettingsPart>();

        // OpenXML annotates Settings as non-null, but it IS null on a freshly
        // added DocumentSettingsPart until we assign it below.
        var existingSettings = settingsPart.Settings;
        // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
        var settings = existingSettings ?? new Settings();

        var compatibility = settings.GetFirstChild<Compatibility>()
            ?? settings.AppendChild(new Compatibility());

        // Replace any existing CompatibilityMode entry; leave other settings alone.
        var existingMode = compatibility.Elements<CompatibilitySetting>()
            .FirstOrDefault(s => s.Name?.Value == CompatSettingNameValues.CompatibilityMode);
        existingMode?.Remove();

        compatibility.AppendChild(new CompatibilitySetting
        {
            Name = CompatSettingNameValues.CompatibilityMode,
            Uri = "http://schemas.microsoft.com/office/word",
            Val = "15",
        });

        if (existingSettings is null)
        {
            settingsPart.Settings = settings;
        }
        settings.Save(settingsPart);
    }

    /// <summary>
    /// Add a description to a layout table (one used purely for visual grid
    /// layout, e.g. a photo gallery). The description tells assistive tech
    /// the table's purpose so screen readers don't try to announce it as
    /// data. Pair with NOT calling <see cref="MarkAsTableHeaderRow"/> — layout
    /// tables have no header row.
    /// </summary>
    public static void MarkAsLayoutTable(Table table, string description)
    {
        var tblProperties = table.Elements<TableProperties>().FirstOrDefault()
            ?? table.PrependChild(new TableProperties());

        tblProperties.RemoveAllChildren<TableCaption>();
        tblProperties.RemoveAllChildren<TableDescription>();

        tblProperties.AppendChild(new TableCaption { Val = description });
        tblProperties.AppendChild(new TableDescription { Val = description });
    }

    /// <summary>
    /// Set the core document properties Word reads for accessibility checks
    /// and the title bar (Title, Subject, Creator, Language).
    /// </summary>
    public static void SetCoreProperties(
        WordprocessingDocument document,
        string title,
        string? subject = null,
        string creator = DefaultCreator,
        string language = DefaultLanguage)
    {
        var corePart = document.PackageProperties;
        corePart.Title = title;
        corePart.Subject = subject ?? title;
        corePart.Creator = creator;
        corePart.Language = language;
        corePart.Created = DateTime.Now;
        corePart.Modified = DateTime.Now;
    }

    /// <summary>
    /// Build a paragraph that uses a built-in heading style.
    /// Pass one of the <c>*StyleId</c> constants on this class.
    /// </summary>
    public static Paragraph CreateHeadingParagraph(string text, string styleId)
    {
        var paragraph = new Paragraph();

        var paragraphProperties = new ParagraphProperties(
            new ParagraphStyleId { Val = styleId }
        );
        paragraph.AppendChild(paragraphProperties);

        var run = paragraph.AppendChild(new Run());
        run.AppendChild(new Text(text));

        return paragraph;
    }

    /// <summary>
    /// Set descriptive alt text on an inline drawing.
    /// <para>
    /// <paramref name="description"/> is the long alt text — Word reads
    /// <see cref="DrawingDocProperties.Description"/> as the primary
    /// accessibility label, and the underlying picture's
    /// <see cref="DrawingNonVisualPicture.Description"/> is also set so
    /// downstream PDF-conversion paths (e.g. Word → Save As PDF) carry the
    /// alt text into the tagged PDF.
    /// </para>
    /// <para>
    /// <paramref name="title"/> is the optional short label. Office 2019+
    /// distinguishes Title from Description, and the accessibility checker
    /// flags identical Title + Description as a warning. Pass a Title only
    /// when it adds information beyond <paramref name="description"/>;
    /// otherwise leave it null.
    /// </para>
    /// </summary>
    public static void SetImageAltText(
        DrawingDocProperties wpDocProperties,
        DrawingNonVisualPicture pictureProperties,
        string description,
        string? title = null)
    {
        wpDocProperties.Description = description;
        pictureProperties.Description = description;

        if (!string.IsNullOrEmpty(title))
        {
            wpDocProperties.Title = title;
            pictureProperties.Title = title;
        }
    }

    /// <summary>
    /// Mark a table row as a header row that repeats on each page break.
    /// Screen readers announce subsequent data rows in terms of the labels
    /// in marked header rows. Apply only to data tables — never to layout
    /// tables (e.g. photo grids).
    /// </summary>
    public static void MarkAsTableHeaderRow(TableRow row)
    {
        var rowProperties = row.Elements<TableRowProperties>().FirstOrDefault();
        if (rowProperties == null)
        {
            rowProperties = new TableRowProperties();
            row.PrependChild(rowProperties);
        }

        if (!rowProperties.Elements<TableHeader>().Any())
        {
            rowProperties.AppendChild(new TableHeader());
        }
    }

    private static void AddStyleIfMissing(Styles styles, string styleId,
        string styleName, string fontSize, bool bold,
        int? outlineLevel = null)
    {
        if (styles.Elements<Style>().Any(s => s.StyleId?.Value == styleId))
        {
            return;
        }

        var style = new Style
        {
            Type = StyleValues.Paragraph,
            StyleId = styleId,
            CustomStyle = false,
            Default = false
        };

        style.AppendChild(new StyleName { Val = styleName });
        style.AppendChild(new BasedOn { Val = "Normal" });
        style.AppendChild(new NextParagraphStyle { Val = "Normal" });
        style.AppendChild(new PrimaryStyle());

        var paragraphProperties = new StyleParagraphProperties();
        if (outlineLevel.HasValue)
        {
            paragraphProperties.AppendChild(new OutlineLevel { Val = outlineLevel.Value });
        }
        style.AppendChild(paragraphProperties);

        var runProperties = new StyleRunProperties();
        if (bold)
        {
            runProperties.AppendChild(new Bold());
            runProperties.AppendChild(new BoldComplexScript());
        }
        runProperties.AppendChild(new FontSize { Val = fontSize });
        runProperties.AppendChild(new FontSizeComplexScript { Val = fontSize });
        style.AppendChild(runProperties);

        styles.AppendChild(style);
    }
}
#pragma warning restore S3220
