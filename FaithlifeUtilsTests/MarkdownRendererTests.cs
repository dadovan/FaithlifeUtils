using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Faithlife.NotesApi.v1;
using FaithlifeUtils;
using Libronix.DataTypes;
using Libronix.DataTypes.GrammarDriven;
using Libronix.DataTypes.RichText;
using Libronix.DigitalLibrary.RichText;
using Libronix.RichText;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FaithlifeUtilsTests;

[TestClass]
public class MarkdownRendererTests
{
    private static string RenderNoteTags(IReadOnlyCollection<NoteTagDto>? tags, bool trimEnd = true)
    {
        var sb = new StringBuilder();
        var method = typeof(MarkdownRenderer).GetMethod("RenderNoteTags", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(method);
#pragma warning disable CS8601 // Null is OK here
        method.Invoke(null, new object[] { sb, tags });
#pragma warning restore CS8601
        return trimEnd ? sb.ToString().TrimEnd() : sb.ToString();
    }

    private static string RenderRichText(IReadOnlyList<RichTextElement> richText, bool trimEnd = true)
    {
        var sb = new StringBuilder();
        var method = typeof(MarkdownRenderer).GetMethod("RenderRichText", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(method);
        method.Invoke(null, new object[] { sb, richText });
        return trimEnd ? sb.ToString().TrimEnd() : sb.ToString();
    }

    private static string RenderTextRangeTitle(string resourceTitle, string contentsTitle, RichTextElement[] elements, bool trimEnd = true)
    {
        var method = typeof(MarkdownRenderer).GetMethod("RenderTextRangeTitle", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(method);
        var title = method.Invoke(null, new object[] { resourceTitle, contentsTitle, elements }) as string;
        ArgumentNullException.ThrowIfNull(title);
        return trimEnd ? title.TrimEnd() : title;
    }

    [TestMethod]
    public void RenderNoteTagsTests()
    {
        var tags = new List<NoteTagDto>().AsReadOnly();
        Assert.AreEqual(String.Empty, RenderNoteTags(tags));

        tags = new List<NoteTagDto> { new() { Plain = new NoteTagPlainDto { Text = "#Tag1" } } }.AsReadOnly();
        Assert.AreEqual("Tags: #Tag1", RenderNoteTags(tags));

        tags = new List<NoteTagDto>
            {
                new() { Plain = new NoteTagPlainDto { Text = "#Tag1" } },
                new() { Plain = new NoteTagPlainDto { Text = "#Tag3" } }
            }
            .AsReadOnly();
        Assert.AreEqual("Tags: #Tag1 #Tag3", RenderNoteTags(tags));
    }

    [TestMethod]
    public void RenderRichText_IndentTest()
    {
        var elements = new List<RichTextElement>
        {
            new RichTextParagraph { FontSize = 10, Margin = new RichTextFrameThickness(10, 0, 0, 0) },
            new RichTextReferenceMilestoneStart(),
            new RichTextRun { Text = "Father, accept this offering" },
            new RichTextEndElement()
        };

        Assert.AreEqual(@"  Father, accept this offering", RenderRichText(elements.ToArray()));

        elements[0] = new RichTextParagraph { FontSize = 10, Margin = new RichTextFrameThickness(20, 0, 0, 0) };
        Assert.AreEqual(@"    Father, accept this offering", RenderRichText(elements.ToArray()));
    }

    [TestMethod]
    public void RenderRichText_MissingRichTextEndElement()
    {
        // RichTextResourcePopupLink searches to the end of the collection for a RichTextEndElement.  Let's make sure we don't crash if we don't give it one.
        // While I don't expect Faithlife to produce invalid rich text, I may misunderstand what output is valid.  :)
        var elements = new List<RichTextElement>
        {
            new RichTextParagraph { FontSize = 10 },
            new RichTextRun { Text = "Father, accept this offering" },
            new RichTextResourcePopupLink { FontBold = true },
            new RichTextRun { Text = "from your whole family." },
            new RichTextRun { Text = "Grant us your peace in this life," },
            new RichTextRun { Text = "save us from final damnation," },
            new RichTextRun { Text = "and count us among those you have chosen." }
        };

        Assert.AreEqual("Father, accept this offering", RenderRichText(elements.ToArray()));
    }

    [TestMethod]
    public void RenderRichText_SimpleParagraphTest()
    {
        var elements = new List<RichTextElement>
        {
            new RichTextParagraph { FontSize = 10 },
            new RichTextReferenceMilestoneStart(),
            new RichTextRun { Text = "Father, accept this offering" },
            new RichTextEndElement()
        };

        Assert.AreEqual(@"Father, accept this offering", RenderRichText(elements.ToArray()));
    }

    [TestMethod]
    public void RenderRichText_StyleTests()
    {
        var elements = new List<RichTextElement>
        {
            new RichTextParagraph { FontSize = 10 },
            new RichTextReferenceMilestoneStart { Name = "ccc" },
            new RichTextRun { Text = "Father, accept this offering" },
            new RichTextRun { FontItalic = true, Text = "Grant us your peace in this life," },
            new RichTextRun { FontBold = true, Text = "save us from final damnation," },
            new RichTextRun { Text = "and count us among those you have chosen." },
            new RichTextEndElement()
        };

        var expected = @"Father, accept this offering _Grant us your peace in this life,_ **save us from final damnation,** and count us among those you have chosen.";
        Assert.AreEqual(expected, RenderRichText(elements.ToArray()));
    }

    [TestMethod]
    public void RenderRichText_StyleSpacingTests()
    {
        var elements = new List<RichTextElement>
        {
            new RichTextParagraph { FontSize = 10 },
            new RichTextReferenceMilestoneStart { Name = "ccc" },
            new RichTextRun { Text = "TrailingSpace " },
            new RichTextRun { FontItalic = true, Text = "Italic-NoSpace" },
            new RichTextRun { FontItalic = true, Text = " Italic-LeadingSpace" },
            new RichTextRun { FontItalic = true, Text = "Italic-TrailingSpace " },
            new RichTextRun { FontItalic = true, Text = " Italic-Spaced " },
            new RichTextRun { Text = " Spaced " },
            new RichTextRun { FontBold = true, Text = "Bold-NoSpace" },
            new RichTextRun { FontBold = true, Text = " Bold-LeadingSpace" },
            new RichTextRun { FontBold = true, Text = "Bold-TrailingSpace " },
            new RichTextRun { FontBold = true, Text = " Bold-Spaced " },
            new RichTextRun { Text = " LeadingSpace" },
            new RichTextEndElement()
        };

        var expected = @"TrailingSpace _Italic-NoSpace_ _Italic-LeadingSpace_ _Italic-TrailingSpace_ _Italic-Spaced_  Spaced **Bold-NoSpace** **Bold-LeadingSpace** **Bold-TrailingSpace** **Bold-Spaced**  LeadingSpace";
        var actual = RenderRichText(elements.ToArray());
        Console.WriteLine($"Expected: {expected.Replace(" ", ".")}");
        Console.WriteLine($"Actual:   {actual.Replace(" ", ".")}");
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void RenderTextRangeTitle_ReferenceMilestoneStart()
    {
        var newCCCReferencePoint = (int point) =>
        {
            var constructor = typeof(NumericHierarchyReferencePoint).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, new[] { typeof(NumericHierarchyDataTypeBase), typeof(int[]) });
            if (constructor == null)
                throw new NotSupportedException($"Unable to find the expected constructor for {nameof(NumericHierarchyReferencePoint)}");
            var instance = constructor.Invoke(new object[] { new CatechismOfTheCatholicChurchDataType("ccc"), new[] { 1037 } });
            if (instance is not NumericHierarchyReferencePoint referencePoint)
                throw new NotSupportedException($"Unable to create an new instance of {nameof(NumericHierarchyReferencePoint)}");
            return referencePoint;
        };

        var milestoneStartReferences = new List<DataTypeReference> { newCCCReferencePoint(1037) }.AsReadOnly();

        var elements = new List<RichTextElement>
        {
            new RichTextParagraph { FontSize = 10 },
            new RichTextReferenceMilestoneStart { Name = "ccc", References = milestoneStartReferences },
            new RichTextRun { FontBold = true, Text = "1037" },
            new RichTextRun { Text = " God predestines no one to go to hell;" },
            new RichTextResourcePopupLink { ResourceId = "LLS:CATCATHCHRCHITL", Position = "FN.270.620" },
            new RichTextRun { FontVariant = RichTextFontVariant.Superscript, Text = "620" },
            new RichTextEndElement(),
            new RichTextRun { Text = " for this, a willful turning away from God (a mortal sin) is necessary...:" },
            new RichTextResourcePopupLink { ResourceId = "LLS:CATCATHCHRCHITL", Position = "FN.270.621" },
            new RichTextRun { FontVariant = RichTextFontVariant.Superscript, Text = "621" },
            new RichTextEndElement(),
            new RichTextRun { Text = " (" },
            new RichTextReference { IsLink = true, Reference = newCCCReferencePoint(162) },
            new RichTextRun { FontItalic = true, Text = "162" },
            new RichTextEndElement(),
            new RichTextRun { FontItalic = true, Text = "; " },
            new RichTextReference { IsLink = true, Reference = newCCCReferencePoint(1014) },
            new RichTextRun { FontItalic = true, Text = "1014" },
            new RichTextEndElement(),
            new RichTextRun { Text = ")" },
            new RichTextEndElement() // RichTextParagraph
        };

        Assert.AreEqual(" - Catechism of the Catholic Church - 1037", RenderTextRangeTitle("Catechism of the Catholic Church", String.Empty, elements.ToArray()));
    }

    [TestMethod]
    public void RenderTextRangeTitle()
    {
        var fields = new List<string> { "bible" }.AsReadOnly();
        var elements = new List<RichTextElement>
        {
            new RichTextParagraph { FontSize = 10 },
            new RichTextField { Fields = fields },
            new RichTextRun { Text = "whoever" },
            new RichTextEndElement(),
            new RichTextField { Fields = fields },
            new RichTextRun { Text = " " },
            new RichTextEndElement(),
            new RichTextField { Fields = fields },
            new RichTextRun { Text = "is" },
            new RichTextEndElement(),
            new RichTextField { Fields = fields },
            new RichTextRun { Text = " " },
            new RichTextEndElement(),
            new RichTextField { Fields = fields },
            new RichTextReverseInterlinearPlaceholder { ResourceId = "RVI:NABREGRKOT", ColumnId = 103651 },
            new RichTextEndElement(),
            new RichTextField { Fields = fields },
            new RichTextRun { Text = "free" },
            new RichTextEndElement(),
            new RichTextField { Fields = fields },
            new RichTextRun { Text = " " },
            new RichTextEndElement(),
            new RichTextEndElement() // RichTextParagraph
        };

        Assert.AreEqual(" - New American Bible: Revised Edition - Chapter 38", RenderTextRangeTitle("New American Bible: Revised Edition", "Chapter 38", elements.ToArray()));
    }
}
