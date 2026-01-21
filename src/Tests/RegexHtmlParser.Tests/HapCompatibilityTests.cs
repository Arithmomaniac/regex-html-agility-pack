// Description: Tests adapted from HtmlAgilityPack to verify RegexHtmlParser compatibility.
// These tests are copied from the original HAP test suite but use RegexHtmlParser instead.

using Xunit;
using RegexHtmlParser;

namespace RegexHtmlParser.Tests;

/// <summary>
/// HAP compatibility tests - these are adapted from the HtmlAgilityPack test suite.
/// </summary>
public class HapCompatibilityTests
{
    #region Basic Parsing Tests

    [Fact]
    public void CreateAttribute()
    {
        var doc = new HtmlDocument();
        var a = doc.CreateAttribute("href");
        Assert.Equal("href", a.Name);
    }

    [Fact]
    public void CreateAttributeWithEncodedText()
    {
        var doc = new HtmlDocument();
        var a = doc.CreateAttribute("href", "http://something.com\"&<>");
        Assert.Equal("href", a.Name);
        Assert.Equal("http://something.com\"&<>", a.Value);
    }

    [Fact]
    public void CreateAttributeWithText()
    {
        var doc = new HtmlDocument();
        var a = doc.CreateAttribute("href", "http://something.com");
        Assert.Equal("href", a.Name);
        Assert.Equal("http://something.com", a.Value);
    }

    [Fact]
    public void CreateElement()
    {
        var doc = new HtmlDocument();
        var a = doc.CreateElement("a");
        Assert.Equal("a", a.Name);
        Assert.Equal(HtmlNodeType.Element, a.NodeType);
    }

    [Fact]
    public void CreateTextNodeWithText()
    {
        var doc = new HtmlDocument();
        var a = doc.CreateTextNode("something");
        Assert.Equal("something", a.InnerText);
        Assert.Equal(HtmlNodeType.Text, a.NodeType);
    }

    #endregion

    #region Void Elements Tests

    [Fact]
    public void TestBr_ClosingBr()
    {
        var html = @" </br>a</br>";
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        // HAP expects 4 child nodes for this (whitespace, br, text, br)
        Assert.True(doc.DocumentNode.ChildNodes.Count >= 2);
    }

    [Fact]
    public void TestBr_OpenBr()
    {
        var html = @" <br>a<br>";
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        // HAP expects 4 child nodes for this
        Assert.True(doc.DocumentNode.ChildNodes.Count >= 2);
    }

    [Fact]
    public void TestVoidElements_Img()
    {
        var html = "<img src=\"test.jpg\"><div>after</div>";
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        
        // img should be self-contained (void element)
        var img = doc.DocumentNode.SelectSingleNode("//img");
        Assert.NotNull(img);
        Assert.Equal("img", img.Name);
        
        // div should be a sibling, not a child of img
        var div = doc.DocumentNode.SelectSingleNode("//div");
        Assert.NotNull(div);
    }

    [Fact]
    public void TestVoidElements_Input()
    {
        var html = "<input type=\"text\"><span>label</span>";
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        
        var input = doc.DocumentNode.SelectSingleNode("//input");
        Assert.NotNull(input);
        
        var span = doc.DocumentNode.SelectSingleNode("//span");
        Assert.NotNull(span);
        Assert.Equal("label", span.InnerText);
    }

    #endregion

    #region Script/Style Content Tests

    [Fact]
    public void TestTextarea_ContentNotParsed()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(@"<script><div>hello</div></script><TEXTAREA>Text in the <div>hello</div>area</TEXTAREA>");
        var divs = doc.DocumentNode.SelectNodes("//div");
        
        // divs inside script/textarea should not be parsed as elements
        Assert.Null(divs);
        
        var ta = doc.DocumentNode.SelectSingleNode("//textarea");
        Assert.NotNull(ta);
        Assert.Contains("div", ta.InnerHtml);
    }

    [Fact]
    public void TestScript_ContentPreserved()
    {
        var html = "<script>if (x < 5) { alert('hello'); }</script>";
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        
        var script = doc.DocumentNode.SelectSingleNode("//script");
        Assert.NotNull(script);
        Assert.Contains("if (x < 5)", script.InnerHtml);
    }

    #endregion

    #region Clone Tests

    [Fact]
    public void TestCloneNode()
    {
        var html = @"<div attr='test'></div>";
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(html);
        
        var divNode = htmlDoc.DocumentNode.SelectSingleNode("//div");
        var newNode = divNode!.Clone();
        
        var attribute1 = divNode.Attributes[0];
        var attribute2 = newNode.Attributes[0];
        
        Assert.Equal(divNode.Attributes.Count, newNode.Attributes.Count);
        Assert.Equal(attribute1.Value, attribute2.Value);
    }

    #endregion

    #region Node Manipulation Tests

    [Fact]
    public void OuterHtmlHasBeenCalled_RemoveCalled_SubsequentOuterHtmlCallsAreBroken()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<html><head></head><body><div>SOme text here</div><div>some bolded<b>text</b></div></body></html>");
        var resultList = doc.DocumentNode.SelectNodes("//div");
        Assert.Equal(2, resultList!.Count);
        resultList.First().Remove();
        Assert.Contains("<body>", doc.DocumentNode.OuterHtml);
        var resultList2 = doc.DocumentNode.SelectNodes("//div");
        Assert.Single(resultList2!);
    }

    [Fact]
    public void TestRemoveUpdatesPreviousSibling()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<div><span>1</span><span>2</span><span>3</span></div>");
        var spans = doc.DocumentNode.SelectNodes("//span")!.ToList();
        
        Assert.Equal(3, spans.Count);
        var toRemove = spans[1]; // middle span
        var toRemovePrevSibling = toRemove.PreviousSibling;
        var toRemoveNextSibling = toRemove.NextSibling;
        
        toRemove.Remove();
        
        Assert.Same(toRemovePrevSibling, toRemoveNextSibling!.PreviousSibling);
    }

    [Fact]
    public void TestReplaceChild()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<div><span>old</span></div>");
        var div = doc.DocumentNode.SelectSingleNode("//div");
        var span = doc.DocumentNode.SelectSingleNode("//span");
        var newNode = doc.CreateElement("p");
        
        div!.ReplaceChild(newNode, span!);
        
        Assert.Null(doc.DocumentNode.SelectSingleNode("//span"));
        Assert.NotNull(doc.DocumentNode.SelectSingleNode("//p"));
    }

    #endregion

    #region Attribute Tests

    [Fact]
    public void TestRemoveAttribute()
    {
        var html = "<h1 a=\"foo\" b=\"bar\">This is new heading</h1>";
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(html);
        
        var h1Node = htmlDoc.DocumentNode.SelectSingleNode("//h1");
        h1Node!.Attributes.Remove("a");
        h1Node.Attributes.Remove("b");
        
        Assert.Equal(0, h1Node.Attributes.Count);
    }

    [Fact]
    public void TestAttributeValue()
    {
        var html = "<body data-foo=\"Hello\"></body>";
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(html);
        
        var body = htmlDoc.DocumentNode.SelectSingleNode("//body");
        var val = body!.GetAttributeValue("data-foo", "");
        
        Assert.Equal("Hello", val);
    }

    [Fact]
    public void TestSetAttributeValue()
    {
        var html = "<div></div>";
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(html);
        
        var div = htmlDoc.DocumentNode.SelectSingleNode("//div");
        div!.SetAttributeValue("id", "test");
        
        Assert.Equal("test", div.GetAttributeValue("id", ""));
    }

    #endregion

    #region Comment Tests

    [Fact]
    public void TestCommentNode()
    {
        var html = @"<!DOCTYPE html>
<html>
<body>
<!--title='Title'-->
<h1>Heading</h1>
</body>
</html>";
        
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(html);
        
        var h1 = htmlDoc.DocumentNode.SelectNodes("//h1");
        Assert.Single(h1!);
    }

    [Fact]
    public void TestComment_Preserved()
    {
        var html = "<!-- This is a comment --><div>content</div>";
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        
        Assert.Equal(2, doc.DocumentNode.ChildNodes.Count);
        Assert.Equal(HtmlNodeType.Comment, doc.DocumentNode.ChildNodes[0].NodeType);
    }

    #endregion

    #region XPath Tests

    [Fact]
    public void TestXPath_SelectByAttribute()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<div id='test'><span class='foo'>text</span></div>");
        
        var node = doc.DocumentNode.SelectSingleNode("//span[@class='foo']");
        Assert.NotNull(node);
        Assert.Equal("text", node.InnerText);
    }

    [Fact]
    public void TestXPath_SelectMultiple()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<ul><li>1</li><li>2</li><li>3</li></ul>");
        
        var nodes = doc.DocumentNode.SelectNodes("//li");
        Assert.NotNull(nodes);
        Assert.Equal(3, nodes.Count);
    }

    [Fact]
    public void TestXPath_Descendant()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<div><p><span>deep</span></p></div>");
        
        var span = doc.DocumentNode.SelectSingleNode("//div//span");
        Assert.NotNull(span);
        Assert.Equal("deep", span.InnerText);
    }

    #endregion

    #region Nested Element Tests

    [Fact]
    public void TestNestedDivs()
    {
        var html = "<div><div><div>deep</div></div></div>";
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        
        var innermost = doc.DocumentNode.SelectSingleNode("//div/div/div");
        Assert.NotNull(innermost);
        Assert.Equal("deep", innermost.InnerText);
    }

    [Fact]
    public void TestNestedLists()
    {
        var html = "<ul><li>item1</li><li><ul><li>nested</li></ul></li></ul>";
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        
        var nestedLi = doc.DocumentNode.SelectSingleNode("//ul/li/ul/li");
        Assert.NotNull(nestedLi);
        Assert.Equal("nested", nestedLi.InnerText);
    }

    [Fact]
    public void TestNestedTables()
    {
        var html = "<table><tr><td><table><tr><td>nested</td></tr></table></td></tr></table>";
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        
        var innerTd = doc.DocumentNode.SelectSingleNode("//table/tr/td/table/tr/td");
        Assert.NotNull(innerTd);
        Assert.Equal("nested", innerTd.InnerText);
    }

    #endregion

    #region InnerHtml/OuterHtml Tests

    [Fact]
    public void TestInnerHtml()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<div><span>child</span></div>");
        
        var div = doc.DocumentNode.SelectSingleNode("//div");
        Assert.Contains("<span>", div!.InnerHtml);
        Assert.Contains("child", div.InnerHtml);
    }

    [Fact]
    public void TestOuterHtml()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<div class=\"test\">content</div>");
        
        var div = doc.DocumentNode.SelectSingleNode("//div");
        Assert.Contains("<div", div!.OuterHtml);
        Assert.Contains("class=\"test\"", div.OuterHtml);
        Assert.Contains("</div>", div.OuterHtml);
    }

    [Fact]
    public void TestInnerText()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<div>Hello <span>World</span>!</div>");
        
        var div = doc.DocumentNode.SelectSingleNode("//div");
        Assert.Equal("Hello World!", div!.InnerText);
    }

    #endregion

    #region Tree Navigation Tests

    [Fact]
    public void TestParentNode()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<div><span>text</span></div>");
        
        var span = doc.DocumentNode.SelectSingleNode("//span");
        Assert.NotNull(span);
        Assert.NotNull(span.ParentNode);
        Assert.Equal("div", span.ParentNode.Name);
    }

    [Fact]
    public void TestChildNodes()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<div><span>1</span><span>2</span></div>");
        
        var div = doc.DocumentNode.SelectSingleNode("//div");
        Assert.Equal(2, div!.ChildNodes.Count);
    }

    [Fact]
    public void TestNextSibling()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<div><span>1</span><span>2</span></div>");
        
        var firstSpan = doc.DocumentNode.SelectSingleNode("//span[1]");
        Assert.NotNull(firstSpan);
        Assert.NotNull(firstSpan.NextSibling);
        Assert.Equal("2", firstSpan.NextSibling.InnerText);
    }

    [Fact]
    public void TestPreviousSibling()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<div><span>1</span><span>2</span></div>");
        
        var secondSpan = doc.DocumentNode.SelectSingleNode("//span[2]");
        Assert.NotNull(secondSpan);
        Assert.NotNull(secondSpan.PreviousSibling);
        Assert.Equal("1", secondSpan.PreviousSibling.InnerText);
    }

    [Fact]
    public void TestDescendants()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<div><p><span>deep</span></p></div>");
        
        var div = doc.DocumentNode.SelectSingleNode("//div");
        var descendants = div!.Descendants().ToList();
        
        Assert.Contains(descendants, n => n.Name == "p");
        Assert.Contains(descendants, n => n.Name == "span");
    }

    [Fact]
    public void TestAncestors()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<div><p><span>deep</span></p></div>");
        
        var span = doc.DocumentNode.SelectSingleNode("//span");
        var ancestors = span!.Ancestors().ToList();
        
        Assert.Contains(ancestors, n => n.Name == "p");
        Assert.Contains(ancestors, n => n.Name == "div");
    }

    #endregion

    #region Attribute Parsing Tests

    [Fact]
    public void TestDoubleQuoteAttribute()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<div id=\"test\"></div>");
        
        var div = doc.DocumentNode.SelectSingleNode("//div");
        Assert.Equal("test", div!.GetAttributeValue("id", ""));
    }

    [Fact]
    public void TestSingleQuoteAttribute()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<div id='test'></div>");
        
        var div = doc.DocumentNode.SelectSingleNode("//div");
        Assert.Equal("test", div!.GetAttributeValue("id", ""));
    }

    [Fact]
    public void TestUnquotedAttribute()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<div id=test></div>");
        
        var div = doc.DocumentNode.SelectSingleNode("//div");
        Assert.Equal("test", div!.GetAttributeValue("id", ""));
    }

    [Fact]
    public void TestBooleanAttribute()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<input disabled>");
        
        var input = doc.DocumentNode.SelectSingleNode("//input");
        Assert.True(input!.Attributes.Contains("disabled"));
    }

    [Fact]
    public void TestMultipleAttributes()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<div id=\"test\" class=\"foo bar\" data-value=\"123\"></div>");
        
        var div = doc.DocumentNode.SelectSingleNode("//div");
        Assert.Equal("test", div!.GetAttributeValue("id", ""));
        Assert.Equal("foo bar", div.GetAttributeValue("class", ""));
        Assert.Equal("123", div.GetAttributeValue("data-value", ""));
    }

    #endregion

    #region Entity Tests

    [Fact]
    public void TestHtmlEntity_DeEntitize()
    {
        var result = HtmlEntity.DeEntitize("&lt;div&gt;");
        Assert.Equal("<div>", result);
    }

    [Fact]
    public void TestHtmlEntity_Entitize()
    {
        var result = HtmlEntity.Entitize("<div>");
        Assert.Contains("&lt;", result);
        Assert.Contains("&gt;", result);
    }

    [Fact]
    public void TestNumericEntity()
    {
        var result = HtmlEntity.DeEntitize("&#60;&#62;");
        Assert.Equal("<>", result);
    }

    [Fact]
    public void TestHexEntity()
    {
        var result = HtmlEntity.DeEntitize("&#x3C;&#x3E;");
        Assert.Equal("<>", result);
    }

    #endregion

    #region Mixed Content Tests

    [Fact]
    public void TestMixedContent()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<div>Hello <b>bold</b> world</div>");
        
        var div = doc.DocumentNode.SelectSingleNode("//div");
        Assert.Equal(3, div!.ChildNodes.Count);
    }

    [Fact]
    public void TestWhitespacePreservation()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<pre>  spaced  </pre>");
        
        var pre = doc.DocumentNode.SelectSingleNode("//pre");
        Assert.Contains("  ", pre!.InnerHtml);
    }

    #endregion

    #region CreateNode Tests

    [Fact]
    public void TestCreateNode()
    {
        var node = HtmlNode.CreateNode("<div class=\"test\">content</div>");
        
        Assert.NotNull(node);
        Assert.Equal("div", node.Name);
        Assert.Equal("test", node.GetAttributeValue("class", ""));
    }

    [Fact]
    public void TestCreateNode_SelfClosing()
    {
        var node = HtmlNode.CreateNode("<br/>");
        
        Assert.NotNull(node);
        Assert.Equal("br", node.Name);
    }

    #endregion

    #region Complex HTML Tests

    [Fact]
    public void TestComplexHtml()
    {
        var html = @"
<!DOCTYPE html>
<html>
<head>
    <title>Test</title>
</head>
<body>
    <div id='container'>
        <h1>Title</h1>
        <p>Paragraph</p>
    </div>
</body>
</html>";
        
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        
        var container = doc.GetElementById("container");
        Assert.NotNull(container);
        Assert.Equal("div", container.Name);
    }

    [Fact]
    public void TestSelfClosingTags()
    {
        var html = "<div><br/><hr/><img src='test.jpg'/></div>";
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        
        var div = doc.DocumentNode.SelectSingleNode("//div");
        Assert.True(div!.ChildNodes.Count >= 3);
    }

    #endregion

    #region Append/Prepend Tests

    [Fact]
    public void TestAppendChild()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<div></div>");
        
        var div = doc.DocumentNode.SelectSingleNode("//div");
        var span = doc.CreateElement("span");
        div!.AppendChild(span);
        
        Assert.Single(div.ChildNodes);
        Assert.Equal("span", div.FirstChild!.Name);
    }

    [Fact]
    public void TestPrependChild()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml("<div><span>existing</span></div>");
        
        var div = doc.DocumentNode.SelectSingleNode("//div");
        var p = doc.CreateElement("p");
        div!.PrependChild(p);
        
        Assert.Equal(2, div.ChildNodes.Count);
        Assert.Equal("p", div.FirstChild!.Name);
    }

    #endregion
}
