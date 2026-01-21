// Description: Tests for RegexHtmlParser - validating the regex-based HTML parser functionality.

using Xunit;
using RegexHtmlParser;

namespace RegexHtmlParser.Tests;

/// <summary>
/// Tests for HtmlDocument parsing functionality.
/// </summary>
public class HtmlDocumentTests
{
    [Fact]
    public void LoadHtml_SimpleDiv_ParsesCorrectly()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<div>hello</div>");
        
        Assert.NotNull(doc.DocumentNode);
        Assert.Single(doc.DocumentNode.ChildNodes);
        Assert.Equal("div", doc.DocumentNode.FirstChild!.Name);
        Assert.Equal("hello", doc.DocumentNode.FirstChild.InnerText);
    }

    [Fact]
    public void LoadHtml_NestedDivs_ParsesCorrectly()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<div><div>inner</div></div>");
        
        var outer = doc.DocumentNode.FirstChild;
        Assert.NotNull(outer);
        Assert.Equal("div", outer.Name);
        
        var inner = outer.FirstChild;
        Assert.NotNull(inner);
        Assert.Equal("div", inner.Name);
        Assert.Equal("inner", inner.InnerText);
    }

    [Fact]
    public void LoadHtml_WithAttributes_ParsesAttributes()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<div id=\"myId\" class=\"myClass\">content</div>");
        
        var div = doc.DocumentNode.FirstChild;
        Assert.NotNull(div);
        Assert.Equal("myId", div.GetAttributeValue("id", ""));
        Assert.Equal("myClass", div.GetAttributeValue("class", ""));
    }

    [Fact]
    public void LoadHtml_SingleQuoteAttributes_ParsesCorrectly()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<div id='myId' class='myClass'>content</div>");
        
        var div = doc.DocumentNode.FirstChild;
        Assert.NotNull(div);
        Assert.Equal("myId", div.GetAttributeValue("id", ""));
        Assert.Equal("myClass", div.GetAttributeValue("class", ""));
    }

    [Fact]
    public void LoadHtml_UnquotedAttributes_ParsesCorrectly()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<div id=myId class=myClass>content</div>");
        
        var div = doc.DocumentNode.FirstChild;
        Assert.NotNull(div);
        Assert.Equal("myId", div.GetAttributeValue("id", ""));
        Assert.Equal("myClass", div.GetAttributeValue("class", ""));
    }

    [Fact]
    public void LoadHtml_BooleanAttribute_ParsesCorrectly()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<input disabled>");
        
        var input = doc.DocumentNode.FirstChild;
        Assert.NotNull(input);
        Assert.True(input.Attributes.Contains("disabled"));
    }

    [Fact]
    public void LoadHtml_SelfClosingTag_ParsesCorrectly()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<br/>");
        
        Assert.Single(doc.DocumentNode.ChildNodes);
        Assert.Equal("br", doc.DocumentNode.FirstChild!.Name);
    }

    [Fact]
    public void LoadHtml_VoidElement_ParsesCorrectly()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<br><hr><img src=\"test.jpg\">");
        
        Assert.Equal(3, doc.DocumentNode.ChildNodes.Count);
        Assert.Equal("br", doc.DocumentNode.ChildNodes[0].Name);
        Assert.Equal("hr", doc.DocumentNode.ChildNodes[1].Name);
        Assert.Equal("img", doc.DocumentNode.ChildNodes[2].Name);
    }

    [Fact]
    public void LoadHtml_Comment_ParsesCorrectly()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<!-- This is a comment --><div>content</div>");
        
        Assert.Equal(2, doc.DocumentNode.ChildNodes.Count);
        Assert.Equal(HtmlNodeType.Comment, doc.DocumentNode.ChildNodes[0].NodeType);
        Assert.Equal(HtmlNodeType.Element, doc.DocumentNode.ChildNodes[1].NodeType);
    }

    [Fact]
    public void LoadHtml_Script_PreservesContent()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<script>if (x < 5) { alert('hello'); }</script>");
        
        var script = doc.DocumentNode.FirstChild;
        Assert.NotNull(script);
        Assert.Equal("script", script.Name);
        Assert.Contains("if (x < 5)", script.InnerHtml);
    }

    [Fact]
    public void LoadHtml_Style_PreservesContent()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<style>.class { color: red; }</style>");
        
        var style = doc.DocumentNode.FirstChild;
        Assert.NotNull(style);
        Assert.Equal("style", style.Name);
        Assert.Contains(".class { color: red; }", style.InnerHtml);
    }

    [Fact]
    public void LoadHtml_MixedContent_ParsesCorrectly()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<div>Hello <span>World</span>!</div>");
        
        var div = doc.DocumentNode.FirstChild;
        Assert.NotNull(div);
        Assert.Equal(3, div.ChildNodes.Count);
        Assert.Equal("Hello ", div.ChildNodes[0].InnerText);
        Assert.Equal("span", div.ChildNodes[1].Name);
        Assert.Equal("!", div.ChildNodes[2].InnerText);
    }

    [Fact]
    public void GetElementById_ExistingId_ReturnsElement()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<div id=\"myDiv\">content</div>");
        
        var element = doc.GetElementById("myDiv");
        Assert.NotNull(element);
        Assert.Equal("div", element.Name);
    }

    [Fact]
    public void GetElementById_NonExistingId_ReturnsNull()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<div id=\"myDiv\">content</div>");
        
        var element = doc.GetElementById("nonexistent");
        Assert.Null(element);
    }

    [Fact]
    public void CreateElement_ReturnsNewElement()
    {
        var doc = new HtmlDocument();
        var element = doc.CreateElement("div");
        
        Assert.NotNull(element);
        Assert.Equal("div", element.Name);
        Assert.Equal(HtmlNodeType.Element, element.NodeType);
    }

    [Fact]
    public void CreateTextNode_ReturnsNewTextNode()
    {
        var doc = new HtmlDocument();
        var textNode = doc.CreateTextNode("Hello World");
        
        Assert.NotNull(textNode);
        Assert.Equal(HtmlNodeType.Text, textNode.NodeType);
        Assert.Equal("Hello World", textNode.InnerText);
    }

    [Fact]
    public void CreateAttribute_ReturnsNewAttribute()
    {
        var doc = new HtmlDocument();
        var attr = doc.CreateAttribute("href", "http://example.com");
        
        Assert.NotNull(attr);
        Assert.Equal("href", attr.Name);
        Assert.Equal("http://example.com", attr.Value);
    }

    [Fact]
    public void LoadHtml_ComplexHtml_ParsesCorrectly()
    {
        var html = @"
<!DOCTYPE html>
<html>
<head>
    <title>Test Page</title>
</head>
<body>
    <div id=""container"">
        <h1>Welcome</h1>
        <p class=""intro"">This is a test.</p>
        <!-- Comment -->
        <script>console.log('test');</script>
    </div>
</body>
</html>";
        
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        
        Assert.NotNull(doc.DocumentNode);
        var container = doc.GetElementById("container");
        Assert.NotNull(container);
    }

    [Fact]
    public void SelectNodes_XPath_ReturnsMatchingNodes()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<div><span class=\"test\">one</span><span class=\"test\">two</span></div>");
        
        var nodes = doc.DocumentNode.SelectNodes("//span[@class='test']");
        Assert.NotNull(nodes);
        Assert.Equal(2, nodes.Count);
    }

    [Fact]
    public void SelectSingleNode_XPath_ReturnsSingleNode()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<div><span id=\"first\">one</span><span id=\"second\">two</span></div>");
        
        var node = doc.DocumentNode.SelectSingleNode("//span[@id='first']");
        Assert.NotNull(node);
        Assert.Equal("one", node.InnerText);
    }

    [Fact]
    public void OuterHtml_ReturnsCorrectMarkup()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<div class=\"test\">content</div>");
        
        var div = doc.DocumentNode.FirstChild;
        Assert.NotNull(div);
        Assert.Contains("<div", div.OuterHtml);
        Assert.Contains("class=\"test\"", div.OuterHtml);
        Assert.Contains("content", div.OuterHtml);
        Assert.Contains("</div>", div.OuterHtml);
    }

    [Fact]
    public void InnerHtml_ReturnsChildContent()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<div><span>child</span></div>");
        
        var div = doc.DocumentNode.FirstChild;
        Assert.NotNull(div);
        Assert.Contains("<span>", div.InnerHtml);
        Assert.Contains("child", div.InnerHtml);
    }

    [Fact]
    public void Descendants_ReturnsAllDescendants()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<div><p><span>text</span></p></div>");
        
        var div = doc.DocumentNode.FirstChild;
        Assert.NotNull(div);
        
        var descendants = div.Descendants().ToList();
        Assert.Contains(descendants, n => n.Name == "p");
        Assert.Contains(descendants, n => n.Name == "span");
    }

    [Fact]
    public void Ancestors_ReturnsAllAncestors()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<div><p><span>text</span></p></div>");
        
        var span = doc.DocumentNode.SelectSingleNode("//span");
        Assert.NotNull(span);
        
        var ancestors = span.Ancestors().ToList();
        Assert.Contains(ancestors, n => n.Name == "p");
        Assert.Contains(ancestors, n => n.Name == "div");
    }

    [Fact]
    public void NextSibling_ReturnsNextNode()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<div><span>first</span><span>second</span></div>");
        
        var firstSpan = doc.DocumentNode.SelectSingleNode("//span[1]");
        Assert.NotNull(firstSpan);
        Assert.NotNull(firstSpan.NextSibling);
        Assert.Equal("second", firstSpan.NextSibling.InnerText);
    }

    [Fact]
    public void PreviousSibling_ReturnsPreviousNode()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<div><span>first</span><span>second</span></div>");
        
        var secondSpan = doc.DocumentNode.SelectSingleNode("//span[2]");
        Assert.NotNull(secondSpan);
        Assert.NotNull(secondSpan.PreviousSibling);
        Assert.Equal("first", secondSpan.PreviousSibling.InnerText);
    }

    [Fact]
    public void CloneNode_Deep_ClonesAllDescendants()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<div><span>text</span></div>");
        
        var div = doc.DocumentNode.FirstChild;
        Assert.NotNull(div);
        
        var clone = div.CloneNode(true);
        Assert.Equal(div.Name, clone.Name);
        Assert.Equal(div.InnerHtml, clone.InnerHtml);
        Assert.NotSame(div, clone);
    }

    [Fact]
    public void CloneNode_Shallow_DoesNotCloneChildren()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<div><span>text</span></div>");
        
        var div = doc.DocumentNode.FirstChild;
        Assert.NotNull(div);
        
        var clone = div.CloneNode(false);
        Assert.Equal(div.Name, clone.Name);
        Assert.False(clone.HasChildNodes);
    }

    [Fact]
    public void AppendChild_AddsChildNode()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<div></div>");
        
        var div = doc.DocumentNode.FirstChild;
        Assert.NotNull(div);
        
        var span = doc.CreateElement("span");
        div.AppendChild(span);
        
        Assert.Single(div.ChildNodes);
        Assert.Equal("span", div.FirstChild!.Name);
    }

    [Fact]
    public void RemoveChild_RemovesChildNode()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<div><span>text</span></div>");
        
        var div = doc.DocumentNode.FirstChild;
        Assert.NotNull(div);
        
        var span = div.FirstChild;
        Assert.NotNull(span);
        
        div.RemoveChild(span);
        Assert.Empty(div.ChildNodes);
    }

    [Fact]
    public void SetAttributeValue_SetsAttribute()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<div></div>");
        
        var div = doc.DocumentNode.FirstChild;
        Assert.NotNull(div);
        
        div.SetAttributeValue("id", "myId");
        Assert.Equal("myId", div.GetAttributeValue("id", ""));
    }

    [Fact]
    public void Textarea_PreservesContent()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<textarea><div>not a tag</div></textarea>");
        
        var textarea = doc.DocumentNode.FirstChild;
        Assert.NotNull(textarea);
        Assert.Equal("textarea", textarea.Name);
        // Content should be treated as text, not parsed as HTML
        Assert.Contains("<div>not a tag</div>", textarea.InnerHtml);
    }

    [Fact]
    public void LoadHtml_PreservesCaseSensitivity_ForSvgElements()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<svg viewBox=\"0 0 100 100\"><clipPath id=\"myClip\"></clipPath></svg>");
        
        var svg = doc.DocumentNode.FirstChild;
        Assert.NotNull(svg);
        Assert.Equal("svg", svg.Name);
    }

    [Fact]
    public void HtmlEntity_DeEntitize_DecodesEntities()
    {
        var result = HtmlEntity.DeEntitize("&lt;div&gt;&amp;nbsp;&lt;/div&gt;");
        Assert.Equal("<div>&nbsp;</div>", result);
    }

    [Fact]
    public void HtmlEntity_Entitize_EncodesSpecialChars()
    {
        var result = HtmlEntity.Entitize("<div>\"test\"</div>");
        Assert.Contains("&lt;", result);
        Assert.Contains("&gt;", result);
        Assert.Contains("&quot;", result);
    }

    [Fact]
    public void CreateNode_ParsesHtmlString()
    {
        var node = HtmlNode.CreateNode("<div class=\"test\">content</div>");
        
        Assert.NotNull(node);
        Assert.Equal("div", node.Name);
        Assert.Equal("test", node.GetAttributeValue("class", ""));
    }

    [Fact]
    public void LoadHtml_MultipleRootElements_ParsesAll()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<div>one</div><div>two</div><div>three</div>");
        
        Assert.Equal(3, doc.DocumentNode.ChildNodes.Count);
    }

    [Fact]
    public void LineInfo_IsTrackedCorrectly()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<div>\n<span>text</span>\n</div>");
        
        var span = doc.DocumentNode.SelectSingleNode("//span");
        Assert.NotNull(span);
        Assert.Equal(2, span.Line);
    }

    [Fact]
    public void StreamPosition_IsTrackedCorrectly()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<div><span>text</span></div>");
        
        var span = doc.DocumentNode.SelectSingleNode("//span");
        Assert.NotNull(span);
        Assert.True(span.StreamPosition > 0);
    }
}
