using HtmlAgilityPack.RegexParser;

namespace HtmlAgilityPack
{
    /// <summary>
    /// Extension methods to enable regex-based parsing on HtmlDocument.
    /// </summary>
    public static class HtmlDocumentRegexExtensions
    {
        /// <summary>
        /// Loads HTML using the hybrid regex-based parser (tokenizer + tree builder).
        /// This is the default multi-pass parser.
        /// </summary>
        /// <param name="document">The HtmlDocument to load into.</param>
        /// <param name="html">The HTML string to parse.</param>
        public static void LoadHtmlWithRegex(this HtmlDocument document, string html)
        {
            var parser = new HybridRegexParser();
            parser.Parse(document, html);
        }

        /// <summary>
        /// Loads HTML using the specified parser implementation.
        /// </summary>
        /// <param name="document">The HtmlDocument to load into.</param>
        /// <param name="html">The HTML string to parse.</param>
        /// <param name="parser">The parser implementation to use.</param>
        public static void LoadHtmlWithRegex(this HtmlDocument document, string html, IHtmlParser parser)
        {
            if (parser == null)
                throw new ArgumentNullException(nameof(parser));
                
            parser.Parse(document, html);
        }

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
