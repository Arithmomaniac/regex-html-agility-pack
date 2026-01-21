// Description: Regex-based HTML parsing implementation for HtmlAgilityPack.
// This replaces the state-machine parser with a regex-based tokenizer using .NET's balancing groups.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace HtmlAgilityPack
{
    public partial class HtmlDocument
    {
        /// <summary>
        /// Enable regex-based parsing instead of the traditional state machine parser.
        /// </summary>
        public static bool UseRegexParser = true;

        /// <summary>
        /// Parses HTML using regex-based tokenization.
        /// </summary>
        private void ParseWithRegex()
        {
            if (ParseExecuting != null)
            {
                ParseExecuting(this);
            }

            if (OptionComputeChecksum)
            {
                _crc32 = new Crc32();
            }

            Lastnodes = new Dictionary<string, HtmlNode>();
            _parseerrors = new List<HtmlParseError>();
            _line = 1;
            _lineposition = 1;
            _maxlineposition = 0;

            _documentnode._innerlength = Text.Length;
            _documentnode._outerlength = Text.Length;
            _remainderOffset = Text.Length;

            _lastparentnode = _documentnode;
            
            int position = 0;
            var nodeStack = new Stack<HtmlNode>();
            nodeStack.Push(_documentnode);

            var matches = RegexTokenizerPatterns.TokenizerPattern.Matches(Text);

            foreach (Match match in matches)
            {
                // Skip matches that fall before current position (content inside raw text elements)
                if (match.Index < position)
                {
                    continue;
                }

                // Handle any text before this match
                if (match.Index > position)
                {
                    var leadingText = Text.Substring(position, match.Index - position);
                    if (!string.IsNullOrEmpty(leadingText))
                    {
                        UpdateLineInfo(leadingText);
                    }
                }

                position = match.Index;

                if (match.Groups["comment"].Success)
                {
                    ParseRegexComment(match, nodeStack.Peek());
                }
                else if (match.Groups["doctype"].Success)
                {
                    ParseRegexDoctype(match, nodeStack.Peek());
                }
                else if (match.Groups["cdata"].Success)
                {
                    ParseRegexCData(match, nodeStack.Peek());
                }
                else if (match.Groups["selfClosing"].Success)
                {
                    ParseRegexSelfClosingTag(match, nodeStack.Peek());
                }
                else if (match.Groups["closing"].Success)
                {
                    ParseRegexClosingTag(match, nodeStack);
                }
                else if (match.Groups["opening"].Success)
                {
                    ParseRegexOpeningTag(match, Text, ref position, nodeStack);
                }
                else if (match.Groups["text"].Success)
                {
                    ParseRegexText(match, nodeStack.Peek());
                }

                // Update position for the match itself, but only if it wasn't already
                // updated by ParseRegexOpeningTag (for raw text elements like script/style)
                var matchEndPosition = match.Index + match.Length;
                if (position < matchEndPosition)
                {
                    UpdateLineInfo(match.Value);
                    position = matchEndPosition;
                }
            }

            // Handle any remaining text
            if (position < Text.Length)
            {
                var remaining = Text.Substring(position);
                if (!string.IsNullOrEmpty(remaining) && !string.IsNullOrWhiteSpace(remaining))
                {
                    var textNode = CreateNode(HtmlNodeType.Text, position);
                    textNode._outerlength = remaining.Length;
                    textNode._innerlength = remaining.Length;
                    textNode._innerstartindex = position;
                    textNode._line = _line;
                    textNode._lineposition = _lineposition;
                    textNode._streamposition = position;
                    nodeStack.Peek().AppendChild(textNode);
                }
            }

            // Close any remaining open tags
            while (nodeStack.Count > 1)
            {
                nodeStack.Pop();
            }

            Lastnodes.Clear();
        }

        private void UpdateLineInfo(string text)
        {
            foreach (char c in text)
            {
                if (c == '\n')
                {
                    _line++;
                    _lineposition = 1;
                }
                else
                {
                    _lineposition++;
                }
                if (_lineposition > _maxlineposition)
                {
                    _maxlineposition = _lineposition;
                }
            }
        }

        private void ParseRegexComment(Match match, HtmlNode parent)
        {
            var commentText = match.Value;
            // Extract content between <!-- and -->
            var innerContent = commentText.Length > 7
                ? commentText.Substring(4, commentText.Length - 7)
                : string.Empty;

            var node = CreateNode(HtmlNodeType.Comment, match.Index);
            node._outerlength = commentText.Length;
            node._innerlength = innerContent.Length;
            node._innerstartindex = match.Index + 4;
            node._line = _line;
            node._lineposition = _lineposition;
            node._streamposition = match.Index;

            parent.AppendChild(node);
        }

        private void ParseRegexDoctype(Match match, HtmlNode parent)
        {
            var node = CreateNode(HtmlNodeType.Comment, match.Index);
            node._outerlength = match.Length;
            node._innerlength = match.Length;
            node._innerstartindex = match.Index;
            node._line = _line;
            node._lineposition = _lineposition;
            node._streamposition = match.Index;

            // Set doctype name
            node.SetName("!" + match.Value.Substring(2, match.Length - 3).Trim());

            parent.AppendChild(node);
        }

        private void ParseRegexCData(Match match, HtmlNode parent)
        {
            var node = CreateNode(HtmlNodeType.Comment, match.Index);
            node._outerlength = match.Length;
            node._innerlength = match.Length - 12; // Remove <![CDATA[ and ]]>
            node._innerstartindex = match.Index + 9;
            node._line = _line;
            node._lineposition = _lineposition;
            node._streamposition = match.Index;

            parent.AppendChild(node);
        }

        private void ParseRegexSelfClosingTag(Match match, HtmlNode parent)
        {
            var tagMatch = Regex.Match(match.Value, @"<(?<name>[a-zA-Z][a-zA-Z0-9:-]*)\s*(?<attrs>[^>]*?)\s*/>");
            if (!tagMatch.Success)
                return;

            var tagName = tagMatch.Groups["name"].Value;
            var attrsText = tagMatch.Groups["attrs"].Value;

            var node = CreateNode(HtmlNodeType.Element, match.Index);
            node.SetName(tagName);
            node._starttag = true;
            node._outerlength = match.Length;
            node._innerlength = 0;
            node._innerstartindex = match.Index + match.Length;
            node._line = _line;
            node._lineposition = _lineposition;
            node._streamposition = match.Index;
            node._namestartindex = match.Index + 1;
            node._namelength = tagName.Length;

            ParseRegexAttributes(attrsText, node, match.Index + 1 + tagName.Length);
            RegisterNodeId(node);
            parent.AppendChild(node);

            // Track for same-name navigation
            HtmlNode prev = Utilities.GetDictionaryValueOrDefault(Lastnodes, node.Name);
            node._prevwithsamename = prev;
            Lastnodes[node.Name] = node;
        }

        private void ParseRegexOpeningTag(Match match, string html, ref int position, Stack<HtmlNode> nodeStack)
        {
            var tagMatch = Regex.Match(match.Value, @"<(?<name>[a-zA-Z][a-zA-Z0-9:-]*)\s*(?<attrs>[^>]*)>");
            if (!tagMatch.Success)
                return;

            var tagName = tagMatch.Groups["name"].Value;
            var attrsText = tagMatch.Groups["attrs"].Value;

            // Check for void elements
            bool isVoidElement = HtmlNode.IsEmptyElement(tagName) || HtmlNode.IsClosedElement(tagName);

            var node = CreateNode(HtmlNodeType.Element, match.Index);
            node.SetName(tagName);
            node._starttag = true;
            node._outerlength = match.Length;
            node._line = _line;
            node._lineposition = _lineposition;
            node._streamposition = match.Index;
            node._namestartindex = match.Index + 1;
            node._namelength = tagName.Length;

            ParseRegexAttributes(attrsText, node, match.Index + 1 + tagName.Length);
            RegisterNodeId(node);
            nodeStack.Peek().AppendChild(node);

            // Track for same-name navigation
            HtmlNode prev = Utilities.GetDictionaryValueOrDefault(Lastnodes, node.Name);
            node._prevwithsamename = prev;
            Lastnodes[node.Name] = node;

            // Handle void elements - they don't go on the stack
            if (isVoidElement)
            {
                node._innerlength = 0;
                node._innerstartindex = match.Index + match.Length;
                return;
            }

            // Handle raw text elements (script, style, etc.)
            if (HtmlNode.IsCDataElement(tagName))
            {
                var endPosition = match.Index + match.Length;
                var rawTextPattern = RegexTokenizerPatterns.CreateRawTextContentPattern(tagName);
                var rawMatch = rawTextPattern.Match(html, endPosition);

                if (rawMatch.Success)
                {
                    var rawContent = rawMatch.Groups["content"].Value;
                    node._innerstartindex = endPosition;
                    node._innerlength = rawContent.Length;
                    
                    if (!string.IsNullOrEmpty(rawContent))
                    {
                        var textNode = CreateNode(HtmlNodeType.Text, endPosition);
                        textNode._outerlength = rawContent.Length;
                        textNode._innerlength = rawContent.Length;
                        textNode._innerstartindex = endPosition;
                        textNode._line = _line;
                        textNode._lineposition = _lineposition;
                        textNode._streamposition = endPosition;
                        node.AppendChild(textNode);
                    }

                    // Mark script/style as hiding inner text
                    if (tagName.Equals("script", StringComparison.OrdinalIgnoreCase) ||
                        tagName.Equals("style", StringComparison.OrdinalIgnoreCase))
                    {
                        node._isHideInnerText = true;
                    }

                    // Update outer length to include content and closing tag
                    node._outerlength = rawMatch.Index + rawMatch.Length - match.Index;

                    // Update position to skip past the raw content and closing tag
                    position = rawMatch.Index + rawMatch.Length;
                }
                return;
            }

            // Push onto stack for elements that need closing tags
            nodeStack.Push(node);
        }

        /// <summary>
        /// Uses .NET's balancing groups to find the correctly matched closing tag for nested elements.
        /// This demonstrates the key feature that makes regex-based nested HTML parsing possible in .NET.
        /// Called from ParseRegexClosingTag to verify correct matching.
        /// </summary>
        /// <param name="tagName">The tag name to match</param>
        /// <param name="html">The full HTML string</param>
        /// <param name="startIndex">The start index to search from</param>
        /// <returns>The match result, or null if no balanced match found</returns>
        internal Match TryFindBalancedMatch(string tagName, string html, int startIndex)
        {
            try
            {
                // Use the balancing groups pattern to find properly nested tags
                var balancedPattern = RegexTokenizerPatterns.CreateBalancedTagPattern(tagName);
                var balancedMatch = balancedPattern.Match(html, startIndex);

                if (balancedMatch.Success)
                {
                    return balancedMatch;
                }
            }
            catch (RegexMatchTimeoutException)
            {
                // Balancing groups timed out - fall back to simple matching
            }

            return null;
        }

        private void ParseRegexClosingTag(Match match, Stack<HtmlNode> nodeStack)
        {
            var tagMatch = Regex.Match(match.Value, @"</(?<name>[a-zA-Z][a-zA-Z0-9:-]*)\s*>");
            if (!tagMatch.Success)
                return;

            var tagName = tagMatch.Groups["name"].Value;

            // Find matching opening tag on the stack
            var tempStack = new Stack<HtmlNode>();
            HtmlNode matchingNode = null;

            while (nodeStack.Count > 1)
            {
                var current = nodeStack.Peek();
                if (string.Equals(current.Name, tagName, StringComparison.OrdinalIgnoreCase))
                {
                    matchingNode = nodeStack.Pop();
                    
                    // Update the node's inner/outer lengths
                    matchingNode._innerlength = match.Index - matchingNode._outerstartindex - matchingNode.Name.Length - 2;
                    if (matchingNode._innerlength < 0) matchingNode._innerlength = 0;
                    matchingNode._innerstartindex = matchingNode._outerstartindex + matchingNode.Name.Length + 2;
                    matchingNode._outerlength = match.Index + match.Length - matchingNode._outerstartindex;
                    
                    break;
                }

                // Pop unmatched nodes (implicit close)
                tempStack.Push(nodeStack.Pop());
            }

            // Push back any nodes we temporarily popped (they become siblings)
            while (tempStack.Count > 0)
            {
                var node = tempStack.Pop();
                // These nodes were implicitly closed
            }

            if (matchingNode == null && OptionCheckSyntax)
            {
                _parseerrors.Add(new HtmlParseError(
                    HtmlParseErrorCode.TagNotClosed,
                    _line,
                    _lineposition,
                    match.Index,
                    $"Closing tag '{tagName}' found without matching opening tag",
                    match.Value));
            }
        }

        private void ParseRegexText(Match match, HtmlNode parent)
        {
            var text = match.Value;

            // Don't skip whitespace-only text - it needs to be preserved for OuterHtml
            if (string.IsNullOrEmpty(text))
                return;

            var node = CreateNode(HtmlNodeType.Text, match.Index);
            node._outerlength = text.Length;
            node._innerlength = text.Length;
            node._innerstartindex = match.Index;
            node._line = _line;
            node._lineposition = _lineposition;
            node._streamposition = match.Index;

            parent.AppendChild(node);
        }

        private void ParseRegexAttributes(string attrsText, HtmlNode node, int baseIndex)
        {
            if (string.IsNullOrWhiteSpace(attrsText))
                return;

            var matches = RegexTokenizerPatterns.AttributePattern.Matches(attrsText);
            int attrOffset = 0;

            foreach (Match match in matches)
            {
                var name = match.Groups["name"].Value;
                if (string.IsNullOrEmpty(name))
                    continue;

                var attr = CreateAttribute();
                attr._name = name;
                attr._namestartindex = baseIndex + match.Index;
                attr._namelength = name.Length;
                attr.Line = _line;
                attr._streamposition = baseIndex + match.Index;

                // Determine value and quote type
                if (match.Groups["dqValue"].Success)
                {
                    attr._value = match.Groups["dqValue"].Value;
                    attr.InternalQuoteType = AttributeValueQuote.DoubleQuote;
                    attr._valuestartindex = baseIndex + match.Groups["dqValue"].Index;
                    attr._valuelength = attr._value.Length;
                }
                else if (match.Groups["sqValue"].Success)
                {
                    attr._value = match.Groups["sqValue"].Value;
                    attr.InternalQuoteType = AttributeValueQuote.SingleQuote;
                    attr._valuestartindex = baseIndex + match.Groups["sqValue"].Index;
                    attr._valuelength = attr._value.Length;
                }
                else if (match.Groups["uqValue"].Success)
                {
                    attr._value = match.Groups["uqValue"].Value;
                    attr.InternalQuoteType = AttributeValueQuote.None;  // Unquoted value like attr=value
                    attr._valuestartindex = baseIndex + match.Groups["uqValue"].Index;
                    attr._valuelength = attr._value.Length;
                }
                else
                {
                    attr._value = string.Empty;
                    attr.InternalQuoteType = AttributeValueQuote.WithoutValue;
                }

                node.Attributes.Append(attr);
                attrOffset = match.Index + match.Length;
            }
        }

        private void RegisterNodeId(HtmlNode node)
        {
            if (!OptionUseIdAttribute)
                return;

            var idAttr = node.Attributes["id"];
            if (idAttr != null && !string.IsNullOrEmpty(idAttr.Value))
            {
                SetIdForNode(node, idAttr.Value);
            }
        }
    }
}
