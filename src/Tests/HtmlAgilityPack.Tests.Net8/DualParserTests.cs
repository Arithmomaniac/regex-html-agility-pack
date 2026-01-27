using HtmlAgilityPack;
using HtmlAgilityPack.RegexParser;
using Xunit;

namespace HtmlAgilityPack.Tests.Net8
{
    /// <summary>
    /// Provides parser instances for xUnit Theory tests.
    /// </summary>
    public class ParserTestData
    {
        /// <summary>
        /// All parsers that should pass every test.
        /// </summary>
        public static IEnumerable<object[]> AllParsers =>
            new List<object[]>
            {
                new object[] { new MultiPassRegexParser() },
                new object[] { new PureRegexParser() }
            };

        /// <summary>
        /// Multi-pass parser only (for tests with implicit closing rules).
        /// </summary>
        public static IEnumerable<object[]> MultiPassParser =>
            new List<object[]>
            {
                new object[] { new MultiPassRegexParser() }
            };

        /// <summary>
        /// Pure regex parser only.
        /// </summary>
        public static IEnumerable<object[]> PureRegexParser =>
            new List<object[]>
            {
                new object[] { new PureRegexParser() }
            };
    }

    /// <summary>
    /// Tests that both parsers should pass - basic HTML parsing functionality.
    /// </summary>
    public class DualParserBasicTests
    {
        private static HtmlDocument ParseWith(IHtmlParser parser, string html)
        {
            var doc = new HtmlDocument();
            doc.OptionUseIdAttribute = true;
            parser.Parse(doc, html);
            return doc;
        }

        [Theory]
        [MemberData(nameof(ParserTestData.AllParsers), MemberType = typeof(ParserTestData))]
        public void SimpleElement_ParsesCorrectly(IHtmlParser parser)
        {
            var doc = ParseWith(parser, "<div>Hello</div>");
            Assert.Equal("Hello", doc.DocumentNode.InnerText);
        }

        [Theory]
        [MemberData(nameof(ParserTestData.AllParsers), MemberType = typeof(ParserTestData))]
        public void NestedElements_ParsesCorrectly(IHtmlParser parser)
        {
            var doc = ParseWith(parser, "<div><span>Inner</span></div>");
            var span = doc.DocumentNode.SelectSingleNode("//span");
            Assert.NotNull(span);
            Assert.Equal("Inner", span.InnerText);
        }

        [Theory]
        [MemberData(nameof(ParserTestData.AllParsers), MemberType = typeof(ParserTestData))]
        public void Attributes_ParsedCorrectly(IHtmlParser parser)
        {
            var doc = ParseWith(parser, "<div class='test' id=\"foo\">Content</div>");
            var div = doc.DocumentNode.SelectSingleNode("//div");
            Assert.NotNull(div);
            Assert.Equal("test", div.GetAttributeValue("class", ""));
            Assert.Equal("foo", div.GetAttributeValue("id", ""));
        }

        [Theory]
        [MemberData(nameof(ParserTestData.AllParsers), MemberType = typeof(ParserTestData))]
        public void VoidElements_Br_ParsedCorrectly(IHtmlParser parser)
        {
            var doc = ParseWith(parser, "<p>Line1<br>Line2</p>");
            var brNodes = doc.DocumentNode.SelectNodes("//br");
            Assert.NotNull(brNodes);
            Assert.Single(brNodes);
        }

        [Theory]
        [MemberData(nameof(ParserTestData.AllParsers), MemberType = typeof(ParserTestData))]
        public void VoidElements_Img_ParsedCorrectly(IHtmlParser parser)
        {
            var doc = ParseWith(parser, "<img src='x.png'><p>After</p>");
            var p = doc.DocumentNode.SelectSingleNode("//p");
            Assert.NotNull(p);
            Assert.Equal("After", p.InnerText);
        }

        [Theory]
        [MemberData(nameof(ParserTestData.AllParsers), MemberType = typeof(ParserTestData))]
        public void SelfClosingSyntax_ParsedCorrectly(IHtmlParser parser)
        {
            var doc = ParseWith(parser, "<br/><hr/><input/>");
            Assert.Equal(3, doc.DocumentNode.ChildNodes.Count);
        }

        [Theory]
        [MemberData(nameof(ParserTestData.AllParsers), MemberType = typeof(ParserTestData))]
        public void DeeplyNested_ParsesCorrectly(IHtmlParser parser)
        {
            var doc = ParseWith(parser, "<div><div><div><div>Deep</div></div></div></div>");
            var deepest = doc.DocumentNode.SelectSingleNode("//div/div/div/div");
            Assert.NotNull(deepest);
            Assert.Equal("Deep", deepest.InnerText);
        }

        [Theory]
        [MemberData(nameof(ParserTestData.AllParsers), MemberType = typeof(ParserTestData))]
        public void MultipleRoots_ParsesCorrectly(IHtmlParser parser)
        {
            var doc = ParseWith(parser, "<div>A</div><div>B</div><div>C</div>");
            var divs = doc.DocumentNode.SelectNodes("//div");
            Assert.NotNull(divs);
            Assert.Equal(3, divs.Count);
        }

        [Theory]
        [MemberData(nameof(ParserTestData.AllParsers), MemberType = typeof(ParserTestData))]
        public void TextBetweenTags_ParsesCorrectly(IHtmlParser parser)
        {
            var doc = ParseWith(parser, "<p>Hello <b>World</b>!</p>");
            var innerText = doc.DocumentNode.InnerText.Trim();
            Assert.Equal("Hello World!", innerText);
        }

        [Theory]
        [MemberData(nameof(ParserTestData.AllParsers), MemberType = typeof(ParserTestData))]
        public void GetElementById_Works(IHtmlParser parser)
        {
            var doc = ParseWith(parser, "<div id='target'>Found</div>");
            var element = doc.GetElementbyId("target");
            Assert.NotNull(element);
            Assert.Equal("Found", element.InnerText);
        }

        [Theory]
        [MemberData(nameof(ParserTestData.AllParsers), MemberType = typeof(ParserTestData))]
        public void Comments_ParsedCorrectly(IHtmlParser parser)
        {
            var doc = ParseWith(parser, "<!-- comment --><div>Content</div>");
            var div = doc.DocumentNode.SelectSingleNode("//div");
            Assert.NotNull(div);
            Assert.Equal("Content", div.InnerText);
        }

        [Theory]
        [MemberData(nameof(ParserTestData.AllParsers), MemberType = typeof(ParserTestData))]
        public void MixedCaseTags_ParsesCorrectly(IHtmlParser parser)
        {
            var doc = ParseWith(parser, "<DIV><Span>Text</SPAN></div>");
            var div = doc.DocumentNode.SelectSingleNode("//div");
            Assert.NotNull(div);
            Assert.Equal("Text", div.InnerText);
        }

        [Theory]
        [MemberData(nameof(ParserTestData.AllParsers), MemberType = typeof(ParserTestData))]
        public void BooleanAttributes_ParsedCorrectly(IHtmlParser parser)
        {
            var doc = ParseWith(parser, "<input disabled readonly>");
            var input = doc.DocumentNode.SelectSingleNode("//input");
            Assert.NotNull(input);
            Assert.NotNull(input.GetAttributeValue("disabled", null));
        }

        [Theory]
        [MemberData(nameof(ParserTestData.AllParsers), MemberType = typeof(ParserTestData))]
        public void UnquotedAttributes_ParsedCorrectly(IHtmlParser parser)
        {
            var doc = ParseWith(parser, "<div class=test>Content</div>");
            var div = doc.DocumentNode.SelectSingleNode("//div");
            Assert.NotNull(div);
            Assert.Equal("test", div.GetAttributeValue("class", ""));
        }

        [Theory]
        [MemberData(nameof(ParserTestData.AllParsers), MemberType = typeof(ParserTestData))]
        public void DocType_ParsedCorrectly(IHtmlParser parser)
        {
            var doc = ParseWith(parser, "<!DOCTYPE html><html><body>Test</body></html>");
            var body = doc.DocumentNode.SelectSingleNode("//body");
            Assert.NotNull(body);
            Assert.Equal("Test", body.InnerText);
        }

        [Theory]
        [MemberData(nameof(ParserTestData.AllParsers), MemberType = typeof(ParserTestData))]
        public void NestedSameTag_ParsesCorrectly(IHtmlParser parser)
        {
            // This is the "impossible" case that balancing groups handle
            var doc = ParseWith(parser, "<div><div>Inner</div></div>");
            var divs = doc.DocumentNode.SelectNodes("//div");
            Assert.NotNull(divs);
            Assert.Equal(2, divs.Count);
        }

        [Theory]
        [MemberData(nameof(ParserTestData.AllParsers), MemberType = typeof(ParserTestData))]
        public void ComplexNestedStructure_ParsesCorrectly(IHtmlParser parser)
        {
            var doc = ParseWith(parser, "<div class=\"outer\"><p>Intro</p><div class=\"inner\"><span>Nested</span></div><p>Outro</p></div>");
            var innerDiv = doc.DocumentNode.SelectSingleNode("//div[@class='inner']");
            Assert.NotNull(innerDiv);
            var span = innerDiv.SelectSingleNode(".//span");
            Assert.NotNull(span);
            Assert.Equal("Nested", span.InnerText);
        }
    }

    /// <summary>
    /// Tests for implicit closing rules - now both parsers should pass!
    /// The PureRegexParser uses regex with lookahead patterns to handle implicit closing.
    /// </summary>
    public class ImplicitClosingTests
    {
        private static HtmlDocument ParseWith(IHtmlParser parser, string html)
        {
            var doc = new HtmlDocument();
            doc.OptionUseIdAttribute = true;
            parser.Parse(doc, html);
            return doc;
        }

        [Theory]
        [MemberData(nameof(ParserTestData.AllParsers), MemberType = typeof(ParserTestData))]
        public void ImplicitPClosing_BothParsers(IHtmlParser parser)
        {
            // <p> tags close implicitly when another <p> is encountered
            var doc = ParseWith(parser, "<p>A<p>B<p>C");
            var pTags = doc.DocumentNode.SelectNodes("//p");
            Assert.NotNull(pTags);
            Assert.Equal(3, pTags.Count);
        }

        [Theory]
        [MemberData(nameof(ParserTestData.AllParsers), MemberType = typeof(ParserTestData))]
        public void ImplicitLiClosing_BothParsers(IHtmlParser parser)
        {
            // <li> tags close implicitly when another <li> is encountered
            var doc = ParseWith(parser, "<ul><li>A<li>B<li>C</ul>");
            var liTags = doc.DocumentNode.SelectNodes("//li");
            Assert.NotNull(liTags);
            Assert.Equal(3, liTags.Count);
        }

        [Theory]
        [MemberData(nameof(ParserTestData.AllParsers), MemberType = typeof(ParserTestData))]
        public void ImplicitDtDdClosing_BothParsers(IHtmlParser parser)
        {
            var doc = ParseWith(parser, "<dl><dt>Term<dd>Definition<dt>Term2<dd>Definition2</dl>");
            var dtTags = doc.DocumentNode.SelectNodes("//dt");
            var ddTags = doc.DocumentNode.SelectNodes("//dd");
            Assert.NotNull(dtTags);
            Assert.NotNull(ddTags);
            Assert.Equal(2, dtTags.Count);
            Assert.Equal(2, ddTags.Count);
        }
    }

    /// <summary>
    /// Tests for raw text elements (script, style, textarea) - now both parsers should pass!
    /// The PureRegexParser uses special regex patterns to capture raw content without parsing.
    /// </summary>
    public class RawTextElementTests
    {
        private static HtmlDocument ParseWith(IHtmlParser parser, string html)
        {
            var doc = new HtmlDocument();
            parser.Parse(doc, html);
            return doc;
        }

        [Theory]
        [MemberData(nameof(ParserTestData.AllParsers), MemberType = typeof(ParserTestData))]
        public void ScriptContent_NotParsedAsHtml(IHtmlParser parser)
        {
            // Script content should not be parsed - <div> inside script is not a real element
            var doc = ParseWith(parser, "<script>var x = '<div>fake</div>';</script><div>Real</div>");
            var div = doc.DocumentNode.SelectSingleNode("//div");
            Assert.NotNull(div);
            Assert.Equal("Real", div.InnerText);
        }

        [Theory]
        [MemberData(nameof(ParserTestData.AllParsers), MemberType = typeof(ParserTestData))]
        public void TextareaContent_Preserved(IHtmlParser parser)
        {
            var doc = ParseWith(parser, "<textarea><div>Not a tag</div></textarea>");
            var textarea = doc.DocumentNode.SelectSingleNode("//textarea");
            Assert.NotNull(textarea);
            // The inner content should contain the literal text, not be parsed as HTML
            Assert.Contains("div", textarea.InnerHtml);
        }
    }

    /// <summary>
    /// Parser identification tests.
    /// </summary>
    public class ParserIdentificationTests
    {
        [Fact]
        public void MultiPassParser_HasCorrectName()
        {
            var parser = new MultiPassRegexParser();
            Assert.Equal("MultiPass (Tokenizer + TreeBuilder)", parser.ParserName);
        }

        [Fact]
        public void PureRegexParser_HasCorrectName()
        {
            var parser = new PureRegexParser();
            Assert.Equal("Pure Regex (Single-Pass Balancing Groups)", parser.ParserName);
        }

        [Fact]
        public void ExtensionMethods_ReturnCorrectParsers()
        {
            var multiPass = HtmlDocumentRegexExtensions.GetMultiPassParser();
            var pureRegex = HtmlDocumentRegexExtensions.GetPureRegexParser();

            Assert.IsType<MultiPassRegexParser>(multiPass);
            Assert.IsType<PureRegexParser>(pureRegex);
        }
    }

    /// <summary>
    /// Extension method tests.
    /// </summary>
    public class ExtensionMethodTests
    {
        [Fact]
        public void LoadHtmlWithRegex_UsesMultiPassParser()
        {
            var doc = new HtmlDocument();
            doc.LoadHtmlWithRegex("<div>Test</div>");
            Assert.Equal("Test", doc.DocumentNode.InnerText);
        }

        [Fact]
        public void LoadHtmlWithPureRegex_UsesPureParser()
        {
            var doc = new HtmlDocument();
            doc.LoadHtmlWithPureRegex("<div>Test</div>");
            Assert.Equal("Test", doc.DocumentNode.InnerText);
        }

        [Fact]
        public void LoadHtmlWithParser_UsesSpecifiedParser()
        {
            var doc = new HtmlDocument();
            var parser = new MultiPassRegexParser();
            doc.LoadHtmlWithParser("<div>Custom</div>", parser);
            Assert.Equal("Custom", doc.DocumentNode.InnerText);
        }
    }
}
