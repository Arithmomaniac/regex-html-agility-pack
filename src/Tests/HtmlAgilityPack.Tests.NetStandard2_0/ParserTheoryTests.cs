using System;
using HtmlAgilityPack.RegexParser;
using Xunit;

namespace HtmlAgilityPack.Tests.NetStandard2_0
{
    /// <summary>
    /// Enum to specify which parser implementation to test.
    /// </summary>
    public enum ParserType
    {
        /// <summary>Current production parser (multi-pass: tokenizer + tree builder)</summary>
        Hybrid,
        
        /// <summary>Experimental single-pass pure regex parser</summary>
        Pure
    }

    /// <summary>
    /// Helper class to create parser instances for testing.
    /// </summary>
    public static class ParserFactory
    {
        public static IHtmlParser CreateParser(ParserType type)
        {
            return type switch
            {
                ParserType.Hybrid => new HybridRegexParser(),
                ParserType.Pure => new PureRegexParser(),
                _ => throw new ArgumentException($"Unknown parser type: {type}")
            };
        }

        /// <summary>
        /// Loads HTML using the specified parser type.
        /// </summary>
        public static void LoadHtmlWithParser(this HtmlDocument document, string html, ParserType parserType)
        {
            var parser = CreateParser(parserType);
            document.LoadHtmlWithRegex(html, parser);
        }
    }

    /// <summary>
    /// Theory-based tests that run against both parser implementations.
    /// </summary>
    public class ParserTheoryTests
    {
        private static readonly string SimpleHtml = "<html><body><div>Hello World</div></body></html>";

        [Theory]
        [InlineData(ParserType.Hybrid)]
        [InlineData(ParserType.Pure)]
        public void Simple_Html_Parsing(ParserType parserType)
        {
            var doc = new HtmlDocument();
            doc.LoadHtmlWithParser(SimpleHtml, parserType);

            var div = doc.DocumentNode.SelectSingleNode("//div");
            Assert.NotNull(div);
            Assert.Equal("Hello World", div.InnerText);
        }

        [Theory]
        [InlineData(ParserType.Hybrid)]
        [InlineData(ParserType.Pure)]
        public void Nested_Elements(ParserType parserType)
        {
            var html = "<div><p>Outer<span>Inner</span></p></div>";
            var doc = new HtmlDocument();
            doc.LoadHtmlWithParser(html, parserType);

            var div = doc.DocumentNode.SelectSingleNode("//div");
            Assert.NotNull(div);
            
            var p = div.SelectSingleNode("p");
            Assert.NotNull(p);
            
            var span = p.SelectSingleNode("span");
            Assert.NotNull(span);
            Assert.Equal("Inner", span.InnerText);
        }

        [Theory]
        [InlineData(ParserType.Hybrid)]
        [InlineData(ParserType.Pure)]
        public void Self_Closing_Tags(ParserType parserType)
        {
            var html = "<div><br/><img src='test.jpg'/></div>";
            var doc = new HtmlDocument();
            doc.LoadHtmlWithParser(html, parserType);

            var br = doc.DocumentNode.SelectSingleNode("//br");
            Assert.NotNull(br);
            
            var img = doc.DocumentNode.SelectSingleNode("//img");
            Assert.NotNull(img);
            Assert.Equal("test.jpg", img.GetAttributeValue("src", ""));
        }

        [Theory]
        [InlineData(ParserType.Hybrid)]
        [InlineData(ParserType.Pure)]
        public void Void_Elements(ParserType parserType)
        {
            var html = "<div><br><hr><input type='text'></div>";
            var doc = new HtmlDocument();
            doc.LoadHtmlWithParser(html, parserType);

            var br = doc.DocumentNode.SelectSingleNode("//br");
            Assert.NotNull(br);
            
            var hr = doc.DocumentNode.SelectSingleNode("//hr");
            Assert.NotNull(hr);
            
            var input = doc.DocumentNode.SelectSingleNode("//input");
            Assert.NotNull(input);
            Assert.Equal("text", input.GetAttributeValue("type", ""));
        }

        [Theory]
        [InlineData(ParserType.Hybrid)]
        [InlineData(ParserType.Pure)]
        public void Attributes_Parsing(ParserType parserType)
        {
            var html = "<div class='container' id='main' data-value='123'></div>";
            var doc = new HtmlDocument();
            doc.LoadHtmlWithParser(html, parserType);

            var div = doc.DocumentNode.SelectSingleNode("//div");
            Assert.NotNull(div);
            Assert.Equal("container", div.GetAttributeValue("class", ""));
            Assert.Equal("main", div.GetAttributeValue("id", ""));
            Assert.Equal("123", div.GetAttributeValue("data-value", ""));
        }

        [Theory]
        [InlineData(ParserType.Hybrid)]
        [InlineData(ParserType.Pure)]
        public void Comments_Parsing(ParserType parserType)
        {
            var html = "<div><!-- This is a comment --><p>Text</p></div>";
            var doc = new HtmlDocument();
            doc.LoadHtmlWithParser(html, parserType);

            var div = doc.DocumentNode.SelectSingleNode("//div");
            Assert.NotNull(div);
            Assert.True(div.ChildNodes.Count >= 2); // Comment + p element
        }

        [Theory]
        [InlineData(ParserType.Hybrid)]
        [InlineData(ParserType.Pure)]
        public void Script_Tag_RawText(ParserType parserType)
        {
            var html = "<script>var x = '<div>not parsed</div>';</script>";
            var doc = new HtmlDocument();
            doc.LoadHtmlWithParser(html, parserType);

            var script = doc.DocumentNode.SelectSingleNode("//script");
            Assert.NotNull(script);
            Assert.Contains("var x =", script.InnerText);
            
            // Verify that the content is not parsed as HTML
            var innerDiv = script.SelectSingleNode("div");
            Assert.Null(innerDiv); // Should be null because it's raw text
        }

        [Theory]
        [InlineData(ParserType.Hybrid)]
        [InlineData(ParserType.Pure)]
        public void Style_Tag_RawText(ParserType parserType)
        {
            var html = "<style>div { content: '<p>not parsed</p>'; }</style>";
            var doc = new HtmlDocument();
            doc.LoadHtmlWithParser(html, parserType);

            var style = doc.DocumentNode.SelectSingleNode("//style");
            Assert.NotNull(style);
            Assert.Contains("content:", style.InnerText);
        }

        [Theory]
        [InlineData(ParserType.Hybrid)]
        [InlineData(ParserType.Pure)]
        public void Multiple_Root_Elements(ParserType parserType)
        {
            var html = "<div>First</div><div>Second</div><div>Third</div>";
            var doc = new HtmlDocument();
            doc.LoadHtmlWithParser(html, parserType);

            var divs = doc.DocumentNode.SelectNodes("//div");
            Assert.NotNull(divs);
            Assert.Equal(3, divs.Count);
            Assert.Equal("First", divs[0].InnerText);
            Assert.Equal("Second", divs[1].InnerText);
            Assert.Equal("Third", divs[2].InnerText);
        }

        [Theory]
        [InlineData(ParserType.Hybrid)]
        [InlineData(ParserType.Pure)]
        public void Empty_Elements(ParserType parserType)
        {
            var html = "<div></div><p></p><span></span>";
            var doc = new HtmlDocument();
            doc.LoadHtmlWithParser(html, parserType);

            var div = doc.DocumentNode.SelectSingleNode("//div");
            Assert.NotNull(div);
            Assert.Equal("", div.InnerText);
            
            var p = doc.DocumentNode.SelectSingleNode("//p");
            Assert.NotNull(p);
            
            var span = doc.DocumentNode.SelectSingleNode("//span");
            Assert.NotNull(span);
        }

        [Theory]
        [InlineData(ParserType.Hybrid)]
        [InlineData(ParserType.Pure)]
        public void Text_Content_Between_Tags(ParserType parserType)
        {
            var html = "<div>Before<span>Middle</span>After</div>";
            var doc = new HtmlDocument();
            doc.LoadHtmlWithParser(html, parserType);

            var div = doc.DocumentNode.SelectSingleNode("//div");
            Assert.NotNull(div);
            Assert.Contains("Before", div.InnerText);
            Assert.Contains("Middle", div.InnerText);
            Assert.Contains("After", div.InnerText);
        }

        [Theory]
        [InlineData(ParserType.Hybrid)]
        [InlineData(ParserType.Pure)]
        public void Mixed_Quote_Attributes(ParserType parserType)
        {
            var html = "<div single='value1' double=\"value2\" unquoted=value3></div>";
            var doc = new HtmlDocument();
            doc.LoadHtmlWithParser(html, parserType);

            var div = doc.DocumentNode.SelectSingleNode("//div");
            Assert.NotNull(div);
            Assert.Equal("value1", div.GetAttributeValue("single", ""));
            Assert.Equal("value2", div.GetAttributeValue("double", ""));
            Assert.Equal("value3", div.GetAttributeValue("unquoted", ""));
        }

        [Theory]
        [InlineData(ParserType.Hybrid)]
        [InlineData(ParserType.Pure)]
        public void Boolean_Attributes(ParserType parserType)
        {
            var html = "<input type='checkbox' checked disabled>";
            var doc = new HtmlDocument();
            doc.LoadHtmlWithParser(html, parserType);

            var input = doc.DocumentNode.SelectSingleNode("//input");
            Assert.NotNull(input);
            Assert.True(input.Attributes.Contains("checked"));
            Assert.True(input.Attributes.Contains("disabled"));
        }

        [Theory]
        [InlineData(ParserType.Hybrid)]
        [InlineData(ParserType.Pure)]
        public void Whitespace_Handling(ParserType parserType)
        {
            var html = "<div>  Text with   spaces  </div>";
            var doc = new HtmlDocument();
            doc.LoadHtmlWithParser(html, parserType);

            var div = doc.DocumentNode.SelectSingleNode("//div");
            Assert.NotNull(div);
            // The parser should preserve whitespace
            Assert.Contains("  ", div.InnerText);
        }
    }
}
