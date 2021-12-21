using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Faithlife.NotesApi.v1;
using Libronix.DataTypes;
using Libronix.DataTypes.RichText;
using Libronix.DigitalLibrary;
using Libronix.DigitalLibrary.RichText;
using Libronix.Globalization;
using Libronix.RichText;
using Libronix.Utility.Threading;
using Serilog;

// Disable warnings when passing interpolated strings to Serilog
// ReSharper disable TemplateIsNotCompileTimeConstantProblem

namespace FaithlifeUtils;

#pragma warning disable S1135 // Track uses of "TODO" tags
// TODO: Ideally we would be able to specific some of this formatting via template(s)
#pragma warning restore S1135 // Track uses of "TODO" tags

public static class MarkdownRenderer
{
    // https://stackoverflow.com/a/54174691/346808
    private static readonly char _nbsp = ' ';
    private static readonly ILogger _log = Log.ForContext(typeof(MarkdownRenderer));

    /// <summary>
    /// Renders a <see cref="NotebookDto"/> as markdown in the specified folder.
    /// </summary>
    /// <param name="connector">The <see cref="FaithlifeConnector"/> instance</param>
    /// <param name="notebook">The <see cref="NotebookDto"/> to save</param>
    /// <param name="outputPath">The path to save the markdown into</param>
    /// <remarks>
    /// The file name is based on the
    /// <param cref="notebook"/>
    /// 's title.
    /// If the title is null, the file name is based on the <paramref name="notebook"/>'s id.
    /// If the id is null, the file name defaults to 'Unknown Notebook'
    /// </remarks>
    public static void RenderNotebook(FaithlifeConnector connector, NotebookDto notebook, string outputPath)
    {
        ArgumentNullException.ThrowIfNull(connector);
        ArgumentNullException.ThrowIfNull(notebook);
        ArgumentNullException.ThrowIfNull(outputPath);

        var notebookTitle = notebook.Title ?? notebook.Id ?? "Unknown Notebook";
        var markdownPath = Path.Combine(outputPath, $"{PathEx.CleanseFileName(notebookTitle)}.md");
        _log.Information($"Saving notebook to {markdownPath}");

        var sb = new StringBuilder();
        sb.AppendLine($"# {notebookTitle}\n");

        var notes = connector.GetNotes(notebook.Id);
        foreach (var note in notes.OrderBy(n => n.Created ?? DateTime.Now.ToString(CultureInfo.InvariantCulture)))
        {
            _log.Debug($"Rendering note {note.Id}");
            if (sb.Length > 0)
                sb.AppendLine();

            var noteTitle = note.Created != null ? DateTime.Parse(note.Created).ToLongDateString() : $"Note {note.Id}";
            sb.AppendLine($"### {noteTitle}");

            RenderNoteTags(sb, note.Tags);

            if (note.Anchors != null)
            {
                note.Anchors.ForEach(a => RenderNoteAnchor(connector, sb, a));
                sb.AppendLine();
            }

            if (!String.IsNullOrWhiteSpace(note.Content?.RichText))
            {
                var richText = DigitalLibraryRichText.Serializer.ReadRichTextFromXml(note.Content.RichText);
                RenderRichText(sb, richText);
            }
        }

        File.WriteAllText(markdownPath, sb.ToString());
    }

    /// <summary>
    /// Renders a <see cref="NoteAnchorDto"/> element into markdown
    /// </summary>
    /// <param name="connector">The <see cref="FaithlifeConnector"/> reference to be used for resource retrieval</param>
    /// <param name="sb">The <see cref="StringBuilder"/> holding the rendered markdown</param>
    /// <param name="anchor">The <see cref="NoteAnchorDto"/> element to render</param>
    private static void RenderNoteAnchor(FaithlifeConnector connector, StringBuilder sb, NoteAnchorDto anchor)
    {
        ArgumentNullException.ThrowIfNull(connector);
        ArgumentNullException.ThrowIfNull(sb);
        ArgumentNullException.ThrowIfNull(anchor);

        if (anchor.Reference != null)
        {
            var reference = DataTypeManager.Instance.LoadReference(anchor.Reference.Raw).Render(Culture.Current, DataTypeRenderStyle.Long);
            sb.AppendLine($" - {reference}");
        }
        else if (anchor.TextRange != null)
        {
            var resourceId = anchor.TextRange.ResourceId ?? throw new ArgumentException("Unexpected empty value for TextRange.ResourceId");
            var offset = anchor.TextRange.Offset ?? throw new ArgumentException("Unexpected empty value for TextRange.Offset");
            var length = anchor.TextRange.Length ?? throw new ArgumentException("Unexpected empty value for TextRange.Length");
            var resource = connector.OpenResource(resourceId);
            var tr = resource.CreateTextRangeFromIndexedOffset(offset, length);
            RenderTextRange(sb, tr);
        }
        else
            _log.Warning("Don't know how to render {@Anchor}", anchor);
    }

    /// <summary>
    /// Renders <see cref="NoteTagDto"/> elements into markdown
    /// </summary>
    /// <param name="sb">The <see cref="StringBuilder"/> holding the rendered markdown</param>
    /// <param name="tags">A collection of <see cref="NoteTagDto"/>s to render</param>
    private static void RenderNoteTags(StringBuilder sb, IEnumerable<NoteTagDto>? tags)
    {
        ArgumentNullException.ThrowIfNull(sb);
        if (tags == null)
            return;

        var markdown = String.Join(" ", tags.Select(t => t.Plain?.Text ?? String.Empty));
        if (!String.IsNullOrWhiteSpace(markdown))
            sb.AppendLine($"Tags: {markdown}");
    }

    /// <summary>
    /// Renders an array of <see cref="RichTextElement"/> elements into markdown
    /// </summary>
    /// <param name="sb">The <see cref="StringBuilder"/> holding the rendered markdown</param>
    /// <param name="elements">The elements to render</param>
    private static void RenderRichText(StringBuilder sb, IReadOnlyList<RichTextElement> elements)
    {
        ArgumentNullException.ThrowIfNull(sb);
        ArgumentNullException.ThrowIfNull(elements);

        var skipToEndElement = false;
        var margin = 0;
        for (var i = 0; i < elements.Count; i++)
        {
            var element = elements[i];
            if (skipToEndElement)
            {
                if (element is RichTextEndElement)
                    skipToEndElement = false;
                continue;
            }

            switch (element)
            {
                case RichTextResourcePopupLink:
                {
                    while ((i < (elements.Count - 1)) && elements[i + 1] is not RichTextEndElement)
                        i++;
                    break;
                }
                case RichTextParagraph paragraph:
                {
                    var leftMargin = paragraph.Margin?.Left ?? 0;
                    if (leftMargin > 0)
                    {
                        var fontSize = paragraph.FontSize ?? 10;
                        margin = (int) (leftMargin / fontSize) * 2;
                    }

                    if (i > 0)
                        sb.AppendLine("  ");
                    if (margin > 0)
                        sb.Append(_nbsp, margin);
                    break;
                }
                case RichTextRun run:
                {
                    var isBold = run.FontBold ?? false;
                    var isItalic = run.FontItalic ?? false;
                    if (!isBold && !isItalic)
                    {
                        sb.Append(run.Text);
                        break;
                    }

                    var text = run.Text;
                    if (text.Length < 1)
                        break;
                    var startsWithWhitespace = Char.IsWhiteSpace(text[0]);
                    var endsWithWhitespace = Char.IsWhiteSpace(text[^1]);
                    if (startsWithWhitespace || endsWithWhitespace)
                        text = text.Trim();
                    if (sb[^1] != ' ')
                        sb.Append(' ');

                    if (isBold)
                        sb.Append("**");
                    if (isItalic)
                        sb.Append('_');
                    sb.Append(text);
                    if (isItalic)
                        sb.Append("_ ");
                    if (isBold)
                        sb.Append("** ");
                    break;
                }
                case RichTextReference reference:
                    sb.Append($" **{reference.Reference}** ");
                    skipToEndElement = true;
                    break;
            }
        }

        sb.AppendLine();
    }

    /// <summary>
    /// Renders a <see cref="ResourceTextRange"/> element into markdown
    /// </summary>
    /// <param name="sb">The <see cref="StringBuilder"/> holding the rendered markdown</param>
    /// <param name="textRange">The <see cref="ResourceTextRange"/> to render</param>
    private static void RenderTextRange(StringBuilder sb, ResourceTextRange textRange)
    {
        ArgumentNullException.ThrowIfNull(sb);
        ArgumentNullException.ThrowIfNull(textRange);

        var resourceTitle = textRange.Resource.GetLibraryCatalogInfo().Title.Text;
        var contentsTitle = textRange.Position.ContentsEntry.Title;
        var elements = textRange.GetRichTextContent(WorkState.None).ToArray();

        sb.AppendLine(RenderTextRangeTitle(resourceTitle, contentsTitle, elements));

        sb.Append("> ");

        RenderRichText(sb, elements);
    }

    /// <summary>
    /// Renders a title for a <see cref="ResourceTextRange"/> into markdown
    /// </summary>
    /// <param name="resourceTitle">The title of the referring <see cref="Resource"/></param>
    /// <param name="contentsTitle">The title of the <see cref="ResourceContentsEntry"/></param>
    /// <param name="elements">The rich text elements of this <see cref="ResourceTextRange"/></param>
    /// <returns>The title as markdown</returns>
    private static string RenderTextRangeTitle(string resourceTitle, string contentsTitle, RichTextElement[] elements)
    {
        ArgumentNullException.ThrowIfNull(resourceTitle);
        ArgumentNullException.ThrowIfNull(elements);

        if (elements[1] is RichTextReferenceMilestoneStart milestoneStart && milestoneStart.References[0] is NumericHierarchyReferencePoint referencePoint)
        {
            var startPoint = referencePoint.StartPoint.Values[0];
            var endPoint = referencePoint.EndPoint.Values[0];
            if (startPoint.Equals(endPoint))
                return $" - {resourceTitle} - {startPoint}";
            return $" - {resourceTitle} - {startPoint} - {endPoint}";
        }

        return $" - {resourceTitle} - {contentsTitle}";
    }
}
