using HtmlAgilityPack.RegexParser;

namespace HtmlAgilityPack
{
    /// <summary>
    /// Extension methods to enable regex-based parsing on HtmlDocument.
    /// </summary>
    public static class HtmlDocumentRegexExtensions
    {
        // Default parser instance (multi-pass for better compatibility)
        private static readonly IHtmlParser DefaultParser = new MultiPassRegexParser();
        
        // Pure regex parser instance (single-pass using balancing groups)
        private static readonly IHtmlParser PureParser = new PureRegexParser();

        /// <summary>
        /// Loads HTML using the multi-pass regex parser (default).
        /// This parser uses RegexTokenizer + RegexTreeBuilder and handles
        /// edge cases like implicit closing and malformed HTML.
        /// </summary>
        /// <param name="document">The HtmlDocument to load into.</param>
        /// <param name="html">The HTML string to parse.</param>
        public static void LoadHtmlWithRegex(this HtmlDocument document, string html)
        {
            DefaultParser.Parse(document, html);
        }

        /// <summary>
        /// Loads HTML using the pure single-pass regex parser.
        /// This parser uses .NET balancing groups to parse nested HTML in a single pass.
        /// Note: Works best with well-formed HTML.
        /// </summary>
        /// <param name="document">The HtmlDocument to load into.</param>
        /// <param name="html">The HTML string to parse.</param>
        public static void LoadHtmlWithPureRegex(this HtmlDocument document, string html)
        {
            PureParser.Parse(document, html);
        }

        /// <summary>
        /// Loads HTML using a specific parser implementation.
        /// </summary>
        /// <param name="document">The HtmlDocument to load into.</param>
        /// <param name="html">The HTML string to parse.</param>
        /// <param name="parser">The parser implementation to use.</param>
        public static void LoadHtmlWithParser(this HtmlDocument document, string html, IHtmlParser parser)
        {
            parser.Parse(document, html);
        }

        /// <summary>
        /// Gets the default multi-pass parser instance.
        /// </summary>
        public static IHtmlParser GetMultiPassParser() => DefaultParser;

        /// <summary>
        /// Gets the pure single-pass regex parser instance.
        /// </summary>
        public static IHtmlParser GetPureRegexParser() => PureParser;

        /// <summary>
        /// Loads HTML from a stream using the regex-based parser.
        /// </summary>
        public static void LoadWithRegex(this HtmlDocument document, Stream stream)
        {
            using var reader = new StreamReader(stream);
            LoadHtmlWithRegex(document, reader.ReadToEnd());
        }

        /// <summary>
        /// Loads HTML from a TextReader using the regex-based parser.
        /// </summary>
        public static void LoadWithRegex(this HtmlDocument document, TextReader reader)
        {
            LoadHtmlWithRegex(document, reader.ReadToEnd());
        }
    }
}
