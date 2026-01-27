namespace HtmlAgilityPack.RegexParser
{
    /// <summary>
    /// Common interface for HTML parsers.
    /// This enables a dual parser architecture where different parsing strategies
    /// can be swapped and tested against each other.
    /// </summary>
    public interface IHtmlParser
    {
        /// <summary>
        /// Gets the name of the parser implementation.
        /// Used for identification in tests and diagnostics.
        /// </summary>
        string ParserName { get; }

        /// <summary>
        /// Parses HTML string and populates the HtmlDocument.
        /// </summary>
        /// <param name="document">The HtmlDocument to populate.</param>
        /// <param name="html">The HTML string to parse.</param>
        void Parse(HtmlDocument document, string html);
    }
}
