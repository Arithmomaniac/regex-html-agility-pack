using HtmlAgilityPack.RegexParser;

namespace HtmlAgilityPack
{
    /// <summary>
    /// Extension methods to enable regex-based parsing on HtmlDocument.
    /// </summary>
    public static class HtmlDocumentRegexExtensions
    {
        /// <summary>
        /// Loads HTML using the regex-based parser instead of the state machine parser.
        /// This is the "impossible" parser that uses .NET balancing groups.
        /// </summary>
        /// <param name="document">The HtmlDocument to load into.</param>
        /// <param name="html">The HTML string to parse.</param>
        public static void LoadHtmlWithRegex(this HtmlDocument document, string html)
        {
            if (html == null)
                throw new ArgumentNullException(nameof(html));

            // Set the source text
            document.Text = html;
            
            // Initialize ID tracking if enabled
            if (document.OptionUseIdAttribute)
            {
                document.Nodesid = new Dictionary<string, HtmlNode>(StringComparer.OrdinalIgnoreCase);
            }
            
            // Initialize document node
            var docNode = document.DocumentNode;
            docNode._innerlength = html.Length;
            docNode._outerlength = html.Length;

            // Tokenize using regex
            var tokenizer = new RegexTokenizer();
            var tokens = tokenizer.TokenizeWithAttributes(html);

            // Build the tree
            var treeBuilder = new RegexTreeBuilder();
            treeBuilder.BuildTree(document, tokens, html);
            
            // Register IDs if tracking is enabled
            if (document.OptionUseIdAttribute && document.Nodesid != null)
            {
                RegisterIds(document, document.DocumentNode);
            }
        }

        private static void RegisterIds(HtmlDocument document, HtmlNode node)
        {
            foreach (var child in node.ChildNodes)
            {
                if (child.NodeType == HtmlNodeType.Element)
                {
                    var id = child.GetAttributeValue("id", null);
                    if (id != null && !document.Nodesid.ContainsKey(id))
                    {
                        document.Nodesid[id] = child;
                    }
                    
                    if (child.HasChildNodes)
                    {
                        RegisterIds(document, child);
                    }
                }
            }
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
