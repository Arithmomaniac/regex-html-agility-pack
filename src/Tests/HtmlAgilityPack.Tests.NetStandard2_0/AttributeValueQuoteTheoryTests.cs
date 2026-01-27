using System;
using HtmlAgilityPack.RegexParser;
using Xunit;

namespace HtmlAgilityPack.Tests.NetStandard2_0
{
    /// <summary>
    /// Theory-based tests for attribute value quote handling.
    /// Tests run against both Hybrid and Pure parsers.
    /// </summary>
    public class AttributeValueQuoteTheoryTests
    {
        public static string GlobalHtml1 = "<div singlequote='value' doublequote=\"value\" none=value withoutvalue></div>";

        [Theory]
        [InlineData(ParserType.Hybrid)]
        [InlineData(ParserType.Pure)]
        public void GlobalAttributeValueQuote_DoubleQuote(ParserType parserType)
        {
            var doc = new HtmlDocument();
            doc.GlobalAttributeValueQuote = AttributeValueQuote.DoubleQuote;
            doc.LoadHtmlWithParser(GlobalHtml1, parserType);

            Assert.Equal("<div singlequote=\"value\" doublequote=\"value\" none=\"value\" withoutvalue=\"\"></div>", doc.DocumentNode.OuterHtml);
        }

        [Theory]
        [InlineData(ParserType.Hybrid)]
        [InlineData(ParserType.Pure)]
        public void GlobalAttributeValueQuote_SingleQuote(ParserType parserType)
        {
            var doc = new HtmlDocument();
            doc.GlobalAttributeValueQuote = AttributeValueQuote.SingleQuote;
            doc.LoadHtmlWithParser(GlobalHtml1, parserType);

            Assert.Equal("<div singlequote='value' doublequote='value' none='value' withoutvalue=''></div>", doc.DocumentNode.OuterHtml);
        }

        [Theory]
        [InlineData(ParserType.Hybrid)]
        [InlineData(ParserType.Pure)]
        public void GlobalAttributeValueQuote_WithoutValue(ParserType parserType)
        {
            var doc = new HtmlDocument();
            doc.GlobalAttributeValueQuote = AttributeValueQuote.WithoutValue;
            doc.LoadHtmlWithParser(GlobalHtml1, parserType);

            Assert.Equal("<div singlequote doublequote none withoutvalue></div>", doc.DocumentNode.OuterHtml);
        }

        [Theory]
        [InlineData(ParserType.Hybrid)]
        [InlineData(ParserType.Pure)]
        public void GlobalAttributeValueQuote_Initial(ParserType parserType)
        {
            var doc = new HtmlDocument();
            doc.GlobalAttributeValueQuote = AttributeValueQuote.Initial;
            doc.LoadHtmlWithParser(GlobalHtml1, parserType);

            Assert.Equal("<div singlequote='value' doublequote=\"value\" none=value withoutvalue></div>", doc.DocumentNode.OuterHtml);
        }

        [Theory]
        [InlineData(ParserType.Hybrid)]
        [InlineData(ParserType.Pure)]
        public void GlobalAttributeValueQuote_InitialExceptWithoutValue(ParserType parserType)
        {
            var doc = new HtmlDocument();
            doc.GlobalAttributeValueQuote = AttributeValueQuote.InitialExceptWithoutValue;
            doc.LoadHtmlWithParser(GlobalHtml1, parserType);

            Assert.Equal("<div singlequote='value' doublequote=\"value\" none=value withoutvalue=\"\"></div>", doc.DocumentNode.OuterHtml);
        }

        [Theory]
        [InlineData(ParserType.Hybrid)]
        [InlineData(ParserType.Pure)]
        public void GlobalAttributeValueQuote_None(ParserType parserType)
        {
            var doc = new HtmlDocument();
            doc.GlobalAttributeValueQuote = AttributeValueQuote.None;
            doc.LoadHtmlWithParser(GlobalHtml1, parserType);

            Assert.Equal("<div singlequote=value doublequote=value none=value withoutvalue=></div>", doc.DocumentNode.OuterHtml);
        }

        [Theory]
        [InlineData(ParserType.Hybrid)]
        [InlineData(ParserType.Pure)]
        public void GlobalAttributeValueQuote_DoubleQuote_OutputAsXml(ParserType parserType)
        {
            var doc = new HtmlDocument();
            doc.GlobalAttributeValueQuote = AttributeValueQuote.DoubleQuote;
            doc.OptionOutputAsXml = true;
            doc.LoadHtmlWithParser(GlobalHtml1, parserType);

            Assert.Equal("<?xml version=\"1.0\" encoding=\"utf-8\"?><div singlequote=\"value\" doublequote=\"value\" none=\"value\" withoutvalue=\"\"></div>", doc.DocumentNode.OuterHtml);
        }

        [Theory]
        [InlineData(ParserType.Hybrid)]
        [InlineData(ParserType.Pure)]
        public void GlobalAttributeValueQuote_SingleQuote_OutputAsXml(ParserType parserType)
        {
            var doc = new HtmlDocument();
            doc.GlobalAttributeValueQuote = AttributeValueQuote.SingleQuote;
            doc.OptionOutputAsXml = true;
            doc.LoadHtmlWithParser(GlobalHtml1, parserType);

            Assert.Equal("<?xml version=\"1.0\" encoding=\"utf-8\"?><div singlequote='value' doublequote='value' none='value' withoutvalue=''></div>", doc.DocumentNode.OuterHtml);
        }

        [Theory]
        [InlineData(ParserType.Hybrid)]
        [InlineData(ParserType.Pure)]
        public void GlobalAttributeValueQuote_WithoutValue_OutputAsXml(ParserType parserType)
        {
            var doc = new HtmlDocument();
            doc.GlobalAttributeValueQuote = AttributeValueQuote.WithoutValue;
            doc.OptionOutputAsXml = true;
            doc.LoadHtmlWithParser(GlobalHtml1, parserType);

            Assert.Equal("<?xml version=\"1.0\" encoding=\"utf-8\"?><div singlequote=\"value\" doublequote=\"value\" none=\"value\" withoutvalue=\"\"></div>", doc.DocumentNode.OuterHtml);
        }

        [Theory]
        [InlineData(ParserType.Hybrid)]
        [InlineData(ParserType.Pure)]
        public void GlobalAttributeValueQuote_Initial_OutputAsXml(ParserType parserType)
        {
            var doc = new HtmlDocument();
            doc.GlobalAttributeValueQuote = AttributeValueQuote.Initial;
            doc.OptionOutputAsXml = true;
            doc.LoadHtmlWithParser(GlobalHtml1, parserType);

            Assert.Equal("<?xml version=\"1.0\" encoding=\"utf-8\"?><div singlequote='value' doublequote=\"value\" none=\"value\" withoutvalue=\"\"></div>", doc.DocumentNode.OuterHtml);
        }

        [Theory]
        [InlineData(ParserType.Hybrid)]
        [InlineData(ParserType.Pure)]
        public void GlobalAttributeValueQuote_InitialExceptWithoutValue_OutputAsXml(ParserType parserType)
        {
            var doc = new HtmlDocument();
            doc.GlobalAttributeValueQuote = AttributeValueQuote.InitialExceptWithoutValue;
            doc.OptionOutputAsXml = true;
            doc.LoadHtmlWithParser(GlobalHtml1, parserType);

            Assert.Equal("<?xml version=\"1.0\" encoding=\"utf-8\"?><div singlequote='value' doublequote=\"value\" none=\"value\" withoutvalue=\"\"></div>", doc.DocumentNode.OuterHtml);
        }

        [Theory]
        [InlineData(ParserType.Hybrid)]
        [InlineData(ParserType.Pure)]
        public void GlobalAttributeValueQuote_None_OutputAsXml(ParserType parserType)
        {
            var doc = new HtmlDocument();
            doc.GlobalAttributeValueQuote = AttributeValueQuote.None;
            doc.OptionOutputAsXml = true;
            doc.LoadHtmlWithParser(GlobalHtml1, parserType);

            Assert.Equal("<?xml version=\"1.0\" encoding=\"utf-8\"?><div singlequote=\"value\" doublequote=\"value\" none=\"value\" withoutvalue=\"\"></div>", doc.DocumentNode.OuterHtml);
        }
    }
}
