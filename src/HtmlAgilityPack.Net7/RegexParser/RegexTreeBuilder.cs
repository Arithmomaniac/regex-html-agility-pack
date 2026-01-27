using System.Text.RegularExpressions;
using HtmlAgilityPack.RegexParser;

namespace HtmlAgilityPack
{
    /// <summary>
    /// Builds an HtmlNode tree from tokens produced by RegexTokenizer.
    /// This is PASS 4 of the multi-pass parsing strategy.
    /// 
    /// The tree builder takes the flat token list and constructs the hierarchical
    /// DOM structure, handling:
    /// - Parent/child relationships
    /// - Void elements (self-closing)
    /// - Implicit tag closing (HTML5 rules)
    /// - Unclosed tags
    /// </summary>
    public class RegexTreeBuilder
    {
        private HtmlDocument _document;
        private HtmlNode _currentNode;
        private Stack<HtmlNode> _nodeStack;
        private string _sourceText;

        /// <summary>
        /// Builds the DOM tree from tokens into the given document.
        /// </summary>
        /// <param name="document">The HtmlDocument to populate.</param>
        /// <param name="tokens">Tokens from RegexTokenizer.</param>
        /// <param name="sourceText">Original HTML source text.</param>
        public void BuildTree(HtmlDocument document, List<Token> tokens, string sourceText)
        {
            _document = document;
            _sourceText = sourceText;
            _nodeStack = new Stack<HtmlNode>();

            // Start with the document node as current parent
            _currentNode = document.DocumentNode;
            _nodeStack.Push(_currentNode);

            foreach (var token in tokens)
            {
                ProcessToken(token);
            }

            // Close any remaining open tags
            while (_nodeStack.Count > 1)
            {
                _nodeStack.Pop();
            }

            // Set document lengths
            document.DocumentNode._innerlength = sourceText.Length;
            document.DocumentNode._outerlength = sourceText.Length;
        }

        private void ProcessToken(Token token)
        {
            switch (token.Type)
            {
                case TokenType.OpenTag:
                    ProcessOpenTag(token);
                    break;

                case TokenType.CloseTag:
                    ProcessCloseTag(token);
                    break;

                case TokenType.SelfCloseTag:
                    ProcessSelfCloseTag(token);
                    break;

                case TokenType.Text:
                    ProcessText(token);
                    break;

                case TokenType.Comment:
                    ProcessComment(token);
                    break;

                case TokenType.DocType:
                    ProcessDocType(token);
                    break;

                case TokenType.CData:
                    ProcessCData(token);
                    break;

                case TokenType.ServerSideCode:
                    ProcessServerSideCode(token);
                    break;
            }
        }

        private void ProcessOpenTag(Token token)
        {
            // Check for implicit close of current element
            CheckImplicitClose(token.Name!);

            // Create the element node
            var node = _document.CreateNode(HtmlNodeType.Element, token.Position);
            SetNodeName(node, token.Name!, token.OriginalName!);
            SetNodePositions(node, token);

            // Add attributes
            if (token.Attributes != null)
            {
                foreach (var attr in token.Attributes)
                {
                    AddAttribute(node, attr);
                }
            }
            else if (!string.IsNullOrWhiteSpace(token.RawAttributes))
            {
                // Parse attributes if not already parsed
                var tokenizer = new RegexTokenizer();
                var attrs = tokenizer.ParseAttributes(token.RawAttributes, token.Position);
                foreach (var attr in attrs)
                {
                    AddAttribute(node, attr);
                }
            }

            // Add to current parent
            var parent = _nodeStack.Peek();
            parent.AppendChild(node);

            // Push onto stack (will be popped when we hit closing tag)
            _nodeStack.Push(node);
            _currentNode = node;
        }

        private void ProcessCloseTag(Token token)
        {
            var tagName = token.Name!;

            // Find the matching open tag in the stack
            var found = false;
            var tempStack = new Stack<HtmlNode>();

            while (_nodeStack.Count > 1)
            {
                var node = _nodeStack.Peek();
                if (string.Equals(node.Name, tagName, StringComparison.OrdinalIgnoreCase))
                {
                    // Found matching open tag
                    found = true;

                    // Set end positions
                    node._endnode = _document.CreateNode(HtmlNodeType.Element, token.Position);
                    SetNodeName(node._endnode, tagName, token.OriginalName!);
                    node._endnode._outerstartindex = token.Position;
                    node._endnode._outerlength = token.Length;

                    // Calculate inner length
                    node._innerstartindex = node._outerstartindex + node._outerlength;
                    if (token.Position > node._innerstartindex)
                    {
                        node._innerlength = token.Position - node._innerstartindex;
                    }

                    // Update outer length to include closing tag
                    node._outerlength = (token.Position + token.Length) - node._outerstartindex;

                    _nodeStack.Pop();
                    break;
                }
                else
                {
                    // Implicitly close this unmatched tag
                    tempStack.Push(_nodeStack.Pop());
                }
            }

            if (!found)
            {
                // No matching open tag - this is an orphan close tag
                // HAP typically ignores these or treats them as text
                // For now, we'll restore the stack and ignore
                while (tempStack.Count > 0)
                {
                    _nodeStack.Push(tempStack.Pop());
                }
            }
            else
            {
                // Don't restore - those tags are implicitly closed
            }

            _currentNode = _nodeStack.Peek();
        }

        private void ProcessSelfCloseTag(Token token)
        {
            // Check for implicit close
            CheckImplicitClose(token.Name!);

            // Create the element node
            var node = _document.CreateNode(HtmlNodeType.Element, token.Position);
            SetNodeName(node, token.Name!, token.OriginalName!);
            SetNodePositions(node, token);

            // Self-closing nodes are their own end node
            node._endnode = node;
            node._innerlength = 0;

            // Add attributes
            if (token.Attributes != null)
            {
                foreach (var attr in token.Attributes)
                {
                    AddAttribute(node, attr);
                }
            }
            else if (!string.IsNullOrWhiteSpace(token.RawAttributes))
            {
                var tokenizer = new RegexTokenizer();
                var attrs = tokenizer.ParseAttributes(token.RawAttributes, token.Position);
                foreach (var attr in attrs)
                {
                    AddAttribute(node, attr);
                }
            }

            // Add to current parent (don't push to stack since it's self-closing)
            var parent = _nodeStack.Peek();
            parent.AppendChild(node);
        }

        private void ProcessText(Token token)
        {
            if (string.IsNullOrEmpty(token.Content))
                return;

            var node = _document.CreateNode(HtmlNodeType.Text, token.Position);
            SetNodePositions(node, token);

            // For text nodes, the inner and outer are the same
            node._innerstartindex = token.Position;
            node._innerlength = token.Length;

            // Add to current parent
            var parent = _nodeStack.Peek();
            parent.AppendChild(node);
        }

        private void ProcessComment(Token token)
        {
            var node = _document.CreateNode(HtmlNodeType.Comment, token.Position);
            SetNodePositions(node, token);

            // Add to current parent
            var parent = _nodeStack.Peek();
            parent.AppendChild(node);
        }

        private void ProcessDocType(Token token)
        {
            // DOCTYPE is typically treated as a special comment-like node
            // In HAP, it's often treated as a processing instruction or special element
            var node = _document.CreateNode(HtmlNodeType.Comment, token.Position);
            SetNodePositions(node, token);

            var parent = _nodeStack.Peek();
            parent.AppendChild(node);
        }

        private void ProcessCData(Token token)
        {
            // CDATA is treated as text
            var node = _document.CreateNode(HtmlNodeType.Text, token.Position);
            SetNodePositions(node, token);

            var parent = _nodeStack.Peek();
            parent.AppendChild(node);
        }

        private void ProcessServerSideCode(Token token)
        {
            // Server-side code blocks are preserved as-is
            // Could be treated as comments or special nodes
            var node = _document.CreateNode(HtmlNodeType.Comment, token.Position);
            SetNodePositions(node, token);

            var parent = _nodeStack.Peek();
            parent.AppendChild(node);
        }

        private void SetNodeName(HtmlNode node, string name, string originalName)
        {
            // Use internal SetName method
            node.SetName(name.ToLowerInvariant());
            node._namestartindex = -1; // Indicates name is stored in _name field
            node._namelength = 0;
        }

        private void SetNodePositions(HtmlNode node, Token token)
        {
            node._outerstartindex = token.Position;
            node._outerlength = token.Length;
            node._line = token.Line;
            node._lineposition = token.LinePosition;
            node._streamposition = token.Position;
        }

        private void AddAttribute(HtmlNode node, TokenAttribute tokenAttr)
        {
            var attr = _document.CreateAttribute(tokenAttr.OriginalName, tokenAttr.Value ?? "");
            
            // Set positions - use internal fields since properties may be read-only
            attr.Line = 0; // Would need to calculate from token
            attr._lineposition = 0;
            attr._streamposition = tokenAttr.Position;
            attr._namestartindex = tokenAttr.Position;
            attr._namelength = tokenAttr.OriginalName.Length;
            
            if (tokenAttr.Value != null)
            {
                attr._valuestartindex = tokenAttr.ValuePosition;
                attr._valuelength = tokenAttr.Value.Length;
            }

            // Set quote type
            attr.QuoteType = tokenAttr.QuoteChar switch
            {
                '"' => AttributeValueQuote.DoubleQuote,
                '\'' => AttributeValueQuote.SingleQuote,
                _ => tokenAttr.Value == null ? AttributeValueQuote.WithoutValue : AttributeValueQuote.None
            };

            node.Attributes.Append(attr);
        }

        /// <summary>
        /// Checks if the current tag should implicitly close parent tags.
        /// NOW USES REGEX for HTML5 implicit closing rules!
        /// </summary>
        private void CheckImplicitClose(string newTagName)
        {
            var current = _nodeStack.Count > 1 ? _nodeStack.Peek() : null;
            if (current == null || current.NodeType != HtmlNodeType.Element)
                return;

            var currentName = current.Name;
            if (string.IsNullOrEmpty(currentName))
                return;

            // Use regex pattern to check if implicit close is needed
            // Pattern matches "currentTag:newTag" combinations that trigger close
            if (HtmlPatterns.ShouldImplicitlyClose(currentName, newTagName))
            {
                _nodeStack.Pop();
                current._endnode = current; // Self-close
                _currentNode = _nodeStack.Peek();
                // Recurse in case we need to close more
                CheckImplicitClose(newTagName);
                return;
            }

            // Special case: P is closed by any block element (handled by regex pattern)
            // but also add explicit check for block elements not in the implicit pattern
            if (currentName.Equals("p", StringComparison.OrdinalIgnoreCase) && 
                HtmlPatterns.IsBlockElement(newTagName))
            {
                _nodeStack.Pop();
                current._endnode = current;
                _currentNode = _nodeStack.Peek();
                CheckImplicitClose(newTagName);
            }
        }

        // IsBlockElement now uses regex - see HtmlPatterns.IsBlockElement()
    }
}
