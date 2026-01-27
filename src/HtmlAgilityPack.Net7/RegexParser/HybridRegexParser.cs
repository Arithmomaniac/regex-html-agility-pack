namespace HtmlAgilityPack.RegexParser
{
    /// <summary>
    /// Multi-pass hybrid regex parser.
    /// Uses RegexTokenizer (pass 1-2) + RegexTreeBuilder (pass 3-4).
    /// 
    /// This is the current "production" parser that handles real-world HTML well.
    /// It combines regex tokenization with algorithmic tree building.
    /// </summary>
    public class HybridRegexParser : IHtmlParser
    {
        public string ParserName => "Hybrid (Multi-pass)";

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

            // Pass 1-2: Tokenize using regex
            var tokenizer = new RegexTokenizer();
            var tokens = tokenizer.TokenizeWithAttributes(html);

            // Pass 3-4: Build the tree
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
