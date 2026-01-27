namespace HtmlAgilityPack.RegexParser
{
    /// <summary>
    /// Common interface for HTML parsers.
    /// Allows different parsing strategies to be used interchangeably.
    /// </summary>
    public interface IHtmlParser
    {
        /// <summary>
        /// Parses HTML and populates the given HtmlDocument.
        /// </summary>
        /// <param name="document">The HtmlDocument to populate with parsed content.</param>
        /// <param name="html">The HTML string to parse.</param>
        void Parse(HtmlDocument document, string html);
        
        /// <summary>
        /// Gets the name of this parser implementation.
        /// </summary>
        string ParserName { get; }
    }
}
