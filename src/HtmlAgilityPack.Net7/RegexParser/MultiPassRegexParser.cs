namespace HtmlAgilityPack.RegexParser
{
    /// <summary>
    /// Multi-pass HTML parser implementation that uses:
    /// - Pass 1: RegexTokenizer for flat tokenization
    /// - Pass 2: RegexTreeBuilder for DOM tree construction
    /// 
    /// This is the "tried and true" approach that handles edge cases like
    /// implicit closing, malformed HTML, and raw text elements (script/style).
    /// </summary>
    public class MultiPassRegexParser : IHtmlParser
    {
        /// <inheritdoc />
        public string ParserName => "MultiPass (Tokenizer + TreeBuilder)";

        /// <inheritdoc />
        public void Parse(HtmlDocument document, string html)
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

            // Pass 1: Tokenize using regex
            var tokenizer = new RegexTokenizer();
            var tokens = tokenizer.TokenizeWithAttributes(html);

            // Pass 2: Build the tree
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
    }
}
