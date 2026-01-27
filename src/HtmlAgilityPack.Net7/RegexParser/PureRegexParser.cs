using System.Text.RegularExpressions;

namespace HtmlAgilityPack.RegexParser
{
    /// <summary>
    /// Single-pass HTML parser using .NET's balancing groups.
    /// 
    /// This is the "impossible" parser - using regex alone to parse nested HTML.
    /// It leverages .NET's balancing groups to track tag nesting depth, proving
    /// that .NET regex can handle context-free grammars.
    /// 
    /// Limitations:
    /// - Requires well-formed HTML (properly closed tags)
    /// - Does not handle HTML5 implicit closing rules
    /// - May struggle with malformed HTML
    /// </summary>
    public partial class PureRegexParser : IHtmlParser
    {
        /// <inheritdoc />
        public string ParserName => "Pure Regex (Single-Pass Balancing Groups)";

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

            // Single-pass parsing using balancing groups regex
            ParseRecursive(document, docNode, html, 0);
            
            // Register IDs if tracking is enabled
            if (document.OptionUseIdAttribute && document.Nodesid != null)
            {
                RegisterIds(document, document.DocumentNode);
            }
        }

        private void ParseRecursive(HtmlDocument document, HtmlNode parent, string html, int basePosition)
        {
            int currentPos = 0;
            
            while (currentPos < html.Length)
            {
                // Try to match different HTML constructs at current position
                var match = MasterPattern().Match(html, currentPos);
                
                if (!match.Success || match.Index > currentPos)
                {
                    // Gap before match - treat as text
                    if (match.Success && match.Index > currentPos)
                    {
                        var textContent = html.Substring(currentPos, match.Index - currentPos);
                        AddTextNode(document, parent, textContent, basePosition + currentPos);
                        currentPos = match.Index;
                        continue;
                    }
                    break;
                }

                if (match.Groups["doctype"].Success)
                {
                    // DOCTYPE - treat as comment-like node
                    var node = document.CreateNode(HtmlNodeType.Comment, basePosition + match.Index);
                    SetNodePositions(node, basePosition + match.Index, match.Length);
                    parent.AppendChild(node);
                    currentPos = match.Index + match.Length;
                }
                else if (match.Groups["comment"].Success)
                {
                    // HTML comment
                    var node = document.CreateNode(HtmlNodeType.Comment, basePosition + match.Index);
                    SetNodePositions(node, basePosition + match.Index, match.Length);
                    parent.AppendChild(node);
                    currentPos = match.Index + match.Length;
                }
                else if (match.Groups["selfclose"].Success)
                {
                    // Self-closing tag
                    var tagName = match.Groups["scname"].Value;
                    var attrsStr = match.Groups["scattrs"].Value;
                    
                    var node = document.CreateNode(HtmlNodeType.Element, basePosition + match.Index);
                    node.SetName(tagName.ToLowerInvariant());
                    SetNodePositions(node, basePosition + match.Index, match.Length);
                    node._endnode = node;
                    node._innerlength = 0;
                    
                    ParseAttributes(document, node, attrsStr, basePosition + match.Index);
                    parent.AppendChild(node);
                    currentPos = match.Index + match.Length;
                }
                else if (match.Groups["voidelem"].Success)
                {
                    // Void element (br, hr, img, etc.)
                    var tagName = match.Groups["vename"].Value;
                    var attrsStr = match.Groups["veattrs"].Value;
                    
                    var node = document.CreateNode(HtmlNodeType.Element, basePosition + match.Index);
                    node.SetName(tagName.ToLowerInvariant());
                    SetNodePositions(node, basePosition + match.Index, match.Length);
                    node._endnode = node;
                    node._innerlength = 0;
                    
                    ParseAttributes(document, node, attrsStr, basePosition + match.Index);
                    parent.AppendChild(node);
                    currentPos = match.Index + match.Length;
                }
                else if (match.Groups["balanced"].Success)
                {
                    // Balanced element (using balancing groups!)
                    var tagName = match.Groups["tagname"].Value;
                    var attrsStr = match.Groups["attrs"].Value;
                    var innerContent = match.Groups["content"].Value;
                    var openTagLength = match.Groups["opentag"].Length;
                    
                    var node = document.CreateNode(HtmlNodeType.Element, basePosition + match.Index);
                    node.SetName(tagName.ToLowerInvariant());
                    SetNodePositions(node, basePosition + match.Index, match.Length);
                    
                    // Set inner positions
                    var innerStart = match.Index + openTagLength;
                    node._innerstartindex = basePosition + innerStart;
                    node._innerlength = innerContent.Length;
                    
                    // Create end node reference
                    var closeTagStart = match.Index + match.Length - match.Groups["closetag"].Length;
                    node._endnode = document.CreateNode(HtmlNodeType.Element, basePosition + closeTagStart);
                    node._endnode.SetName(tagName.ToLowerInvariant());
                    node._endnode._outerstartindex = basePosition + closeTagStart;
                    node._endnode._outerlength = match.Groups["closetag"].Length;
                    
                    ParseAttributes(document, node, attrsStr, basePosition + match.Index);
                    parent.AppendChild(node);
                    
                    // Recursively parse inner content
                    if (!string.IsNullOrEmpty(innerContent))
                    {
                        ParseRecursive(document, node, innerContent, basePosition + innerStart);
                    }
                    
                    currentPos = match.Index + match.Length;
                }
                else if (match.Groups["text"].Success)
                {
                    // Text content
                    AddTextNode(document, parent, match.Value, basePosition + match.Index);
                    currentPos = match.Index + match.Length;
                }
                else if (match.Groups["orphanclose"].Success)
                {
                    // Orphan close tag - skip it (common in malformed HTML)
                    currentPos = match.Index + match.Length;
                }
                else
                {
                    // Unknown - advance past it
                    currentPos = match.Index + match.Length;
                }
            }
        }

        private void AddTextNode(HtmlDocument document, HtmlNode parent, string content, int position)
        {
            if (string.IsNullOrEmpty(content))
                return;
                
            var node = document.CreateNode(HtmlNodeType.Text, position);
            SetNodePositions(node, position, content.Length);
            node._innerstartindex = position;
            node._innerlength = content.Length;
            parent.AppendChild(node);
        }

        private void SetNodePositions(HtmlNode node, int position, int length)
        {
            node._outerstartindex = position;
            node._outerlength = length;
            node._line = 1; // Simplified - would need LineTracker for accurate values
            node._lineposition = 1;
            node._streamposition = position;
        }

        private void ParseAttributes(HtmlDocument document, HtmlNode node, string attrsStr, int basePosition)
        {
            if (string.IsNullOrWhiteSpace(attrsStr))
                return;

            var matches = AttributePattern().Matches(attrsStr);
            foreach (Match match in matches)
            {
                var nameGroup = match.Groups["name"];
                if (!nameGroup.Success || string.IsNullOrWhiteSpace(nameGroup.Value))
                    continue;

                string? value = null;
                var quoteType = AttributeValueQuote.WithoutValue;

                var dqGroup = match.Groups["dqval"];
                var sqGroup = match.Groups["sqval"];
                var uqGroup = match.Groups["uqval"];

                if (dqGroup.Success)
                {
                    value = dqGroup.Value;
                    quoteType = AttributeValueQuote.DoubleQuote;
                }
                else if (sqGroup.Success)
                {
                    value = sqGroup.Value;
                    quoteType = AttributeValueQuote.SingleQuote;
                }
                else if (uqGroup.Success)
                {
                    value = uqGroup.Value;
                    quoteType = AttributeValueQuote.None;
                }

                var attr = document.CreateAttribute(nameGroup.Value, value ?? "");
                attr.QuoteType = quoteType;
                node.Attributes.Append(attr);
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

        #region Source-Generated Regex Patterns

        /// <summary>
        /// Master pattern using balancing groups for nested elements.
        /// This is the "impossible" pattern that proves regex CAN parse nested HTML.
        /// </summary>
        [GeneratedRegex(@"
            (?<doctype><!DOCTYPE[^>]*>)                                  # DOCTYPE
            |
            (?<comment><!--.*?-->)                                       # Comment
            |
            (?<selfclose>                                                # Self-closing tag
                <(?<scname>[a-zA-Z][a-zA-Z0-9:-]*)
                (?<scattrs>[^>]*)
                \s*/\s*>
            )
            |
            (?<voidelem>                                                 # Void elements (don't need closing tag)
                <(?<vename>area|base|br|col|embed|hr|img|input|link|meta|param|source|track|wbr|basefont|bgsound|frame|isindex|keygen)
                (?<veattrs>
                    (?:[^>""']*
                        |""[^""]*""
                        |'[^']*'
                    )*
                )
                \s*/?\s*>
            )
            |
            (?<balanced>                                                 # BALANCED ELEMENT with nesting!
                (?<opentag>
                    <(?<tagname>[a-zA-Z][a-zA-Z0-9:-]*)                   # Tag name
                    (?<attrs>                                            # Attributes
                        (?:[^>""']*
                            |""[^""]*""
                            |'[^']*'
                        )*
                    )
                    \s*>
                )
                (?<content>                                              # Inner content with balancing
                    (?>                                                  # Atomic group
                        [^<]+                                            # Text
                        |
                        <!--.*?-->                                       # Nested comments
                        |
                        <(?<DEPTH>)\k<tagname>\b[^>]*>                   # Same-tag open: PUSH
                        |
                        </\k<tagname>\s*>(?<-DEPTH>)                     # Same-tag close: POP
                        |
                        <[a-zA-Z][a-zA-Z0-9:-]*[^>]*/\s*>                # Nested self-closing
                        |
                        <(?:area|base|br|col|embed|hr|img|input|link|meta|param|source|track|wbr)\b[^>]*>  # Nested void
                        |
                        <(?!/?\k<tagname>\b)[^>]+>                       # Other nested tags
                    )*
                )
                (?(DEPTH)(?!))                                           # FAIL if depth not zero
                (?<closetag></\k<tagname>\s*>)                           # Closing tag
            )
            |
            (?<orphanclose></[a-zA-Z][a-zA-Z0-9:-]*\s*>)                  # Orphan close tag
            |
            (?<text>[^<]+)                                               # Text content
            ",
            RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase)]
        private static partial Regex MasterPattern();

        /// <summary>
        /// Attribute parser.
        /// </summary>
        [GeneratedRegex(@"
            (?<name>[^\s=/>""']+)
            (?:
                \s*=\s*
                (?:
                    ""(?<dqval>[^""]*)""
                    |
                    '(?<sqval>[^']*)'
                    |
                    (?<uqval>[^\s>""']+)
                )
            )?
            ",
            RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled)]
        private static partial Regex AttributePattern();

        #endregion
    }
}
