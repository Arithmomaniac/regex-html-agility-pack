using System.Text.RegularExpressions;

namespace HtmlAgilityPack.RegexParser
{
    /// <summary>
    /// Single-pass pure regex parser using .NET balancing groups.
    /// 
    /// This parser attempts to parse HTML in a single regex operation using
    /// balancing groups to handle nesting. It's a proof-of-concept that demonstrates
    /// the theoretical possibility, though it struggles with:
    /// - HTML5 implicit closing rules
    /// - Malformed/real-world HTML
    /// - Complex edge cases
    /// 
    /// Pattern structure: ((looseContent|selfClosingTags)*(closedHtmlBlock))*(looseContent|selfClosingTags)*
    /// </summary>
    public partial class PureRegexParser : IHtmlParser
    {
        public string ParserName => "Pure Regex (Single-pass)";

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

            // Parse using single-pass regex
            ParseRecursive(document, docNode, html, 0, html.Length);
            
            // Register IDs if tracking is enabled
            if (document.OptionUseIdAttribute && document.Nodesid != null)
            {
                RegisterIds(document, document.DocumentNode);
            }
        }

        private void ParseRecursive(HtmlDocument document, HtmlNode parentNode, string html, int startIndex, int endIndex)
        {
            if (startIndex >= endIndex || startIndex < 0 || endIndex > html.Length)
                return;

            string section = html.Substring(startIndex, endIndex - startIndex);
            
            // Use a simpler approach: sequential matching
            int currentPos = 0;
            
            while (currentPos < section.Length)
            {
                // Try to match at current position
                var match = HtmlPatterns.MasterTokenizer().Match(section, currentPos);
                
                if (!match.Success || match.Index > currentPos)
                {
                    // No match or gap - treat gap as text
                    if (match.Success && match.Index > currentPos)
                    {
                        var textContent = section.Substring(currentPos, match.Index - currentPos);
                        AddTextNode(document, parentNode, textContent, startIndex + currentPos);
                        currentPos = match.Index;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    ProcessMatch(document, parentNode, match, html, startIndex + match.Index);
                    currentPos = match.Index + match.Length;
                }
            }
        }

        private void ProcessMatch(HtmlDocument document, HtmlNode parentNode, Match match, string html, int absolutePosition)
        {
            // Determine what was matched
            if (match.Groups["doctype"].Success)
            {
                AddDocTypeNode(document, parentNode, match.Value, absolutePosition);
            }
            else if (match.Groups["comment"].Success)
            {
                AddCommentNode(document, parentNode, match.Value, absolutePosition);
            }
            else if (match.Groups["cdata"].Success)
            {
                AddCDataNode(document, parentNode, match.Value, absolutePosition);
            }
            else if (match.Groups["servercode"].Success)
            {
                AddServerCodeNode(document, parentNode, match.Value, absolutePosition);
            }
            else if (match.Groups["selfclose"].Success)
            {
                AddSelfClosingTag(document, parentNode, match, absolutePosition);
            }
            else if (match.Groups["opentag"].Success)
            {
                // For balanced tags, we need to handle them specially
                AddElementTag(document, parentNode, match, html, absolutePosition);
            }
            else if (match.Groups["text"].Success)
            {
                AddTextNode(document, parentNode, match.Groups["text"].Value, absolutePosition);
            }
        }

        private void AddTextNode(HtmlDocument document, HtmlNode parentNode, string text, int position)
        {
            if (string.IsNullOrEmpty(text))
                return;

            var node = document.CreateNode(HtmlNodeType.Text, position);
            node._outerstartindex = position;
            node._outerlength = text.Length;
            node._innerstartindex = position;
            node._innerlength = text.Length;
            
            parentNode.AppendChild(node);
        }

        private void AddCommentNode(HtmlDocument document, HtmlNode parentNode, string rawText, int position)
        {
            var node = document.CreateNode(HtmlNodeType.Comment, position);
            node._outerstartindex = position;
            node._outerlength = rawText.Length;
            node._innerstartindex = position;
            node._innerlength = rawText.Length;
            
            parentNode.AppendChild(node);
        }

        private void AddDocTypeNode(HtmlDocument document, HtmlNode parentNode, string rawText, int position)
        {
            var node = document.CreateNode(HtmlNodeType.Comment, position);
            node._outerstartindex = position;
            node._outerlength = rawText.Length;
            node._innerstartindex = position;
            node._innerlength = rawText.Length;
            
            parentNode.AppendChild(node);
        }

        private void AddCDataNode(HtmlDocument document, HtmlNode parentNode, string rawText, int position)
        {
            var node = document.CreateNode(HtmlNodeType.Text, position);
            node._outerstartindex = position;
            node._outerlength = rawText.Length;
            node._innerstartindex = position;
            node._innerlength = rawText.Length;
            
            parentNode.AppendChild(node);
        }

        private void AddServerCodeNode(HtmlDocument document, HtmlNode parentNode, string rawText, int position)
        {
            var node = document.CreateNode(HtmlNodeType.Comment, position);
            node._outerstartindex = position;
            node._outerlength = rawText.Length;
            node._innerstartindex = position;
            node._innerlength = rawText.Length;
            
            parentNode.AppendChild(node);
        }

        private void AddSelfClosingTag(HtmlDocument document, HtmlNode parentNode, Match match, int position)
        {
            var tagName = match.Groups["scname"].Value;
            var rawAttrs = match.Groups["scattrs"].Value.Trim();

            var node = document.CreateNode(HtmlNodeType.Element, position);
            node.SetName(tagName.ToLowerInvariant());
            node._outerstartindex = position;
            node._outerlength = match.Length;
            node._innerstartindex = position + match.Length;
            node._innerlength = 0;
            node._endnode = node;

            // Parse attributes if present
            if (!string.IsNullOrWhiteSpace(rawAttrs))
            {
                var tokenizer = new RegexTokenizer();
                var attrs = tokenizer.ParseAttributes(rawAttrs, position);
                foreach (var attr in attrs)
                {
                    AddAttribute(document, node, attr);
                }
            }

            parentNode.AppendChild(node);
        }

        private void AddElementTag(HtmlDocument document, HtmlNode parentNode, Match match, string html, int position)
        {
            var tagName = match.Groups["otname"].Value;
            var rawAttrs = match.Groups["otattrs"].Value.Trim();

            // Check if this is actually a void element (self-closing by spec)
            if (HtmlPatterns.IsVoidElement(tagName))
            {
                AddSelfClosingTag(document, parentNode, match, position);
                return;
            }

            // For raw text elements (script, style), we need special handling
            if (HtmlPatterns.IsRawTextElement(tagName))
            {
                AddRawTextElement(document, parentNode, tagName, rawAttrs, html, position, match.Length);
                return;
            }

            // Create the element node
            var node = document.CreateNode(HtmlNodeType.Element, position);
            node.SetName(tagName.ToLowerInvariant());
            node._outerstartindex = position;
            node._outerlength = match.Length;
            
            // Parse attributes
            if (!string.IsNullOrWhiteSpace(rawAttrs))
            {
                var tokenizer = new RegexTokenizer();
                var attrs = tokenizer.ParseAttributes(rawAttrs, position);
                foreach (var attr in attrs)
                {
                    AddAttribute(document, node, attr);
                }
            }

            parentNode.AppendChild(node);

            // Find the matching closing tag
            var closePattern = new Regex($@"</{Regex.Escape(tagName)}\s*>", RegexOptions.IgnoreCase);
            var closeMatch = closePattern.Match(html, position + match.Length);

            if (closeMatch.Success)
            {
                // Parse content between open and close tags recursively
                int contentStart = position + match.Length;
                int contentEnd = closeMatch.Index;
                
                if (contentEnd > contentStart)
                {
                    ParseRecursive(document, node, html, contentStart, contentEnd);
                }

                // Set inner and outer lengths
                node._innerstartindex = contentStart;
                node._innerlength = contentEnd - contentStart;
                node._outerlength = (closeMatch.Index + closeMatch.Length) - position;

                // Create end node
                node._endnode = document.CreateNode(HtmlNodeType.Element, closeMatch.Index);
                node._endnode.SetName(tagName.ToLowerInvariant());
                node._endnode._outerstartindex = closeMatch.Index;
                node._endnode._outerlength = closeMatch.Length;
            }
            else
            {
                // No closing tag - treat as self-closing
                node._innerstartindex = position + match.Length;
                node._innerlength = 0;
                node._endnode = node;
            }
        }

        private void AddRawTextElement(HtmlDocument document, HtmlNode parentNode, string tagName, 
            string rawAttrs, string html, int position, int openTagLength)
        {
            var node = document.CreateNode(HtmlNodeType.Element, position);
            node.SetName(tagName.ToLowerInvariant());
            node._outerstartindex = position;
            
            // Parse attributes
            if (!string.IsNullOrWhiteSpace(rawAttrs))
            {
                var tokenizer = new RegexTokenizer();
                var attrs = tokenizer.ParseAttributes(rawAttrs, position);
                foreach (var attr in attrs)
                {
                    AddAttribute(document, node, attr);
                }
            }

            parentNode.AppendChild(node);

            // Find closing tag for raw text element
            var closePattern = new Regex($@"</{Regex.Escape(tagName)}\s*>", RegexOptions.IgnoreCase);
            var closeMatch = closePattern.Match(html, position + openTagLength);

            if (closeMatch.Success)
            {
                // Add raw text content as a single text node
                int contentStart = position + openTagLength;
                int contentEnd = closeMatch.Index;
                
                if (contentEnd > contentStart)
                {
                    var content = html.Substring(contentStart, contentEnd - contentStart);
                    AddTextNode(document, node, content, contentStart);
                }

                node._innerstartindex = contentStart;
                node._innerlength = contentEnd - contentStart;
                node._outerlength = (closeMatch.Index + closeMatch.Length) - position;

                node._endnode = document.CreateNode(HtmlNodeType.Element, closeMatch.Index);
                node._endnode.SetName(tagName.ToLowerInvariant());
                node._endnode._outerstartindex = closeMatch.Index;
                node._endnode._outerlength = closeMatch.Length;
            }
            else
            {
                // No closing tag - treat rest as content
                var content = html.Substring(position + openTagLength);
                AddTextNode(document, node, content, position + openTagLength);
                
                node._innerstartindex = position + openTagLength;
                node._innerlength = content.Length;
                node._outerlength = openTagLength + content.Length;
                node._endnode = node;
            }
        }

        private void AddAttribute(HtmlDocument document, HtmlNode node, TokenAttribute tokenAttr)
        {
            var attr = document.CreateAttribute(tokenAttr.OriginalName, tokenAttr.Value ?? "");
            
            attr.Line = 0;
            attr._lineposition = 0;
            attr._streamposition = tokenAttr.Position;
            attr._namestartindex = tokenAttr.Position;
            attr._namelength = tokenAttr.OriginalName.Length;
            
            if (tokenAttr.Value != null)
            {
                attr._valuestartindex = tokenAttr.ValuePosition;
                attr._valuelength = tokenAttr.Value.Length;
            }

            attr.QuoteType = tokenAttr.QuoteChar switch
            {
                '"' => AttributeValueQuote.DoubleQuote,
                '\'' => AttributeValueQuote.SingleQuote,
                _ => tokenAttr.Value == null ? AttributeValueQuote.WithoutValue : AttributeValueQuote.None
            };

            node.Attributes.Append(attr);
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
