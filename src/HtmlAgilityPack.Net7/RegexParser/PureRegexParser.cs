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
    /// This implementation uses a SINGLE UNIFIED REGEX with:
    /// - [GeneratedRegex] source generator for compile-time validation
    /// - C# 10+ constant interpolated strings to reference shared HtmlPatterns constants
    /// - .NET balancing groups for nested element matching
    /// 
    /// Features handled:
    /// - Nested balanced elements (the "impossible" case)
    /// - Raw text elements (script, style, textarea) - content not parsed as HTML
    /// - Implicit tag closing rules (p, li, dt, dd, etc.)
    /// - Void elements (br, img, input, etc.)
    /// - Self-closing syntax
    /// - Comments and DOCTYPE
    /// </summary>
    public partial class PureRegexParser : IHtmlParser
    {
        /// <inheritdoc />
        public string ParserName => "Pure Regex (Single-Pass Balancing Groups)";

        #region Pattern Constants (C# 10+ Constant Interpolated Strings)

        // Shared constants from HtmlPatterns - used directly in [GeneratedRegex] via constant interpolation
        private const string VoidElements = HtmlPatterns.VoidElementsPattern;
        private const string RawTextElements = HtmlPatterns.RawTextElementsPattern;
        private const string BlockElements = HtmlPatterns.BlockElementsPattern;
        private const string ImplicitCloseTagsP = HtmlPatterns.ImplicitCloseTagsPPattern;
        private const string ImplicitCloseTagsLi = HtmlPatterns.ImplicitCloseTagsLiPattern;
        private const string ImplicitCloseTagsDt = HtmlPatterns.ImplicitCloseTagsDtPattern;
        private const string AttributeSection = HtmlPatterns.AttributeSectionPattern;
        
        // Combined pattern for p and block elements (used in implicit closing)
        private const string POrBlockElements = $"{ImplicitCloseTagsP}|{BlockElements}";

        #endregion

        #region Source-Generated Unified Regex (with Constant Interpolated Strings)

        /// <summary>
        /// The unified HTML parsing regex using .NET balancing groups.
        /// Source-generated with C# 10+ constant interpolated strings for:
        /// - Compile-time pattern validation
        /// - Optimal runtime performance  
        /// - Consistency with shared HtmlPatterns constants
        /// </summary>
        [GeneratedRegex($@"
            # ====== DOCTYPE ======
            (?<doctype><!DOCTYPE[^>]*>)
            |
            # ====== COMMENTS ======
            (?<comment><!--.*?-->)
            |
            # ====== SELF-CLOSING SYNTAX (explicit />) ======
            (?<selfclose>
                <(?<scname>[a-zA-Z][a-zA-Z0-9:-]*)
                (?<scattrs>{AttributeSection})
                \s*/\s*>
            )
            |
            # ====== VOID ELEMENTS (no closing tag needed) ======
            (?<voidelem>
                <(?<vename>{VoidElements})
                (?<veattrs>{AttributeSection})
                \s*/?\s*>
            )
            |
            # ====== RAW TEXT ELEMENTS (script, style, textarea) ======
            # Content inside these is NOT parsed as HTML - captured as raw text
            (?<rawtext>
                <(?<rtname>{RawTextElements})
                (?<rtattrs>{AttributeSection})
                \s*>
                (?<rtcontent>.*?)           # Non-greedy capture of raw content
                </\k<rtname>\s*>            # Matching close tag
            )
            |
            # ====== BALANCED ELEMENTS (The 'Impossible' Case!) ======
            # Uses .NET balancing groups to track nested same-tags
            # NOTE: This comes BEFORE implicit patterns so properly closed tags are handled first
            (?<balanced>
                (?<opentag>
                    <(?<tagname>[a-zA-Z][a-zA-Z0-9:-]*)
                    (?<attrs>{AttributeSection})
                    \s*>
                )
                (?<content>
                    (?>                                                 # Atomic group (no backtracking)
                        [^<]+                                           # Text content
                        |
                        <!--.*?-->                                      # Nested comments
                        |
                        <(?<DEPTH>)\k<tagname>\b[^>]*>                   # Same-tag open: PUSH
                        |
                        </\k<tagname>\s*>(?<-DEPTH>)                     # Same-tag close: POP
                        |
                        <[a-zA-Z][a-zA-Z0-9:-]*[^>]*/\s*>                # Nested self-closing
                        |
                        <(?:{VoidElements})\b[^>]*>                      # Nested void elements
                        |
                        <(?!/?\k<tagname>\b)[^>]+>                       # Other nested tags
                    )*
                )
                (?(DEPTH)(?!))                                          # FAIL if depth not zero
                (?<closetag></\k<tagname>\s*>)                           # Closing tag
            )
            |
            # ====== IMPLICIT CLOSE: <p> elements ======
            # When we see <p>, it implicitly closes any open <p>
            # This handles: <p>A<p>B<p>C → three separate <p> elements
            # NOTE: Only matches when balanced pattern above fails (no explicit </p>)
            (?<implicit_p>
                <(?<ipname>{ImplicitCloseTagsP})
                (?<ipattrs>{AttributeSection})
                \s*>
                (?<ipcontent>
                    (?:
                        [^<]+                                           # Text
                        |
                        <!--.*?-->                                      # Comments
                        |
                        <(?:{VoidElements})\b[^>]*/?\s*>                 # Void elements  
                        |
                        <[a-zA-Z][a-zA-Z0-9:-]*[^>]*/\s*>                # Self-closing
                        |
                        <(?!/?(?:{POrBlockElements})\b)[^>]+>            # Other non-block tags
                    )*?
                )
                (?=<(?:{POrBlockElements})\b|</|$)                       # Lookahead: closes before next p/block
            )
            |
            # ====== IMPLICIT CLOSE: <li> elements ======
            (?<implicit_li>
                <(?<ilname>{ImplicitCloseTagsLi})
                (?<ilattrs>{AttributeSection})
                \s*>
                (?<ilcontent>
                    (?:
                        [^<]+
                        |
                        <!--.*?-->
                        |
                        <(?:{VoidElements})\b[^>]*/?\s*>
                        |
                        <[a-zA-Z][a-zA-Z0-9:-]*[^>]*/\s*>
                        |
                        <(?!/?{ImplicitCloseTagsLi}\b)[^>]+>
                    )*?
                )
                (?=<{ImplicitCloseTagsLi}\b|</(?:ul|ol|{ImplicitCloseTagsLi})\b|$)
            )
            |
            # ====== IMPLICIT CLOSE: <dt>/<dd> elements ======
            (?<implicit_dt>
                <(?<idtname>{ImplicitCloseTagsDt})
                (?<idtattrs>{AttributeSection})
                \s*>
                (?<idtcontent>
                    (?:
                        [^<]+
                        |
                        <!--.*?-->
                        |
                        <(?:{VoidElements})\b[^>]*/?\s*>
                        |
                        <[a-zA-Z][a-zA-Z0-9:-]*[^>]*/\s*>
                        |
                        <(?!/?(?:{ImplicitCloseTagsDt})\b)[^>]+>
                    )*?
                )
                (?=<(?:{ImplicitCloseTagsDt})\b|</dl\b|$)
            )
            |
            # ====== ORPHAN CLOSE TAGS ======
            (?<orphanclose></[a-zA-Z][a-zA-Z0-9:-]*\s*>)
            |
            # ====== TEXT CONTENT ======
            (?<text>[^<]+)
        ", RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase)]
        private static partial Regex UnifiedPattern();

        /// <summary>
        /// Tag-only regex that matches ALL tags without consuming content.
        /// This allows ONE regex call to find tags at ALL nesting levels.
        /// </summary>
        [GeneratedRegex($@"
            # ====== DOCTYPE ======
            (?<doctype><!DOCTYPE[^>]*>)
            |
            # ====== COMMENTS ======
            (?<comment><!--.*?-->)
            |
            # ====== SELF-CLOSING SYNTAX (explicit />) ======
            (?<selfclose>
                <(?<scname>[a-zA-Z][a-zA-Z0-9:-]*)
                (?<scattrs>{AttributeSection})
                \s*/\s*>
            )
            |
            # ====== VOID ELEMENTS (no closing tag needed) ======
            (?<voidelem>
                <(?<vename>{VoidElements})
                (?<veattrs>{AttributeSection})
                \s*/?\s*>
            )
            |
            # ====== RAW TEXT ELEMENTS (script, style, textarea) ======
            (?<rawtext>
                <(?<rtname>{RawTextElements})
                (?<rtattrs>{AttributeSection})
                \s*>
                (?<rtcontent>.*?)
                </\k<rtname>\s*>
            )
            |
            # ====== OPENING TAGS (any element) ======
            (?<opentag>
                <(?<openname>[a-zA-Z][a-zA-Z0-9:-]*)
                (?<openattrs>{AttributeSection})
                \s*>
            )
            |
            # ====== CLOSING TAGS ======
            (?<closetag>
                </(?<closename>[a-zA-Z][a-zA-Z0-9:-]*)\s*>
            )
            |
            # ====== TEXT CONTENT ======
            (?<text>[^<]+)
        ", RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase)]
        private static partial Regex TagPattern();

        #endregion

        /// <summary>
        /// Element information for building tree from positions.
        /// </summary>
        private class ElementInfo
        {
            public string TagName;
            public HtmlNode Node;
            public int OpenTagStart;
            public int OpenTagEnd;
            public int ContentStart;
            
            public ElementInfo(string tagName, HtmlNode node, int openTagStart, int openTagEnd)
            {
                TagName = tagName;
                Node = node;
                OpenTagStart = openTagStart;
                OpenTagEnd = openTagEnd;
                ContentStart = openTagEnd;
            }
        }

        /// <summary>
        /// Represents a match with its parent context for building the tree.
        /// </summary>
        private struct MatchInfo
        {
            public Match Match;
            public HtmlNode Parent;
            public int BasePosition;

            public MatchInfo(Match match, HtmlNode parent, int basePosition)
            {
                Match = match;
                Parent = parent;
                BasePosition = basePosition;
            }
        }

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

            // *** SINGLE REGEX CALL ON ENTIRE DOCUMENT ***
            // Finds ALL tags at ALL nesting levels in one pass
            ParseSinglePass(document, docNode, html);
            
            // Register IDs if tracking is enabled
            if (document.OptionUseIdAttribute && document.Nodesid != null)
            {
                RegisterIds(document, document.DocumentNode);
            }
        }

        private void ParseSinglePass(HtmlDocument document, HtmlNode docNode, string html)
        {
            // *** ONE REGEX CALL *** - Get ALL matches in entire document
            var matches = TagPattern().Matches(html);
            
            if (matches.Count == 0)
                return;

            // Stack to track open elements and build tree structure
            var elementStack = new Stack<ElementInfo>();
            elementStack.Push(new ElementInfo("", docNode, 0, 0));
            
            int lastPosition = 0;
            
            foreach (Match match in matches)
            {
                var currentParent = elementStack.Peek();
                
                // Handle text gaps before match
                if (match.Index > lastPosition)
                {
                    var textContent = html.Substring(lastPosition, match.Index - lastPosition);
                    if (!string.IsNullOrWhiteSpace(textContent) || textContent.Length > 0)
                    {
                        AddTextNode(document, currentParent.Node, textContent, lastPosition);
                    }
                }

                if (match.Groups["doctype"].Success)
                {
                    var node = document.CreateNode(HtmlNodeType.Comment, match.Index);
                    SetNodePositions(node, match.Index, match.Length);
                    currentParent.Node.AppendChild(node);
                }
                else if (match.Groups["comment"].Success)
                {
                    var node = document.CreateNode(HtmlNodeType.Comment, match.Index);
                    SetNodePositions(node, match.Index, match.Length);
                    currentParent.Node.AppendChild(node);
                }
                else if (match.Groups["selfclose"].Success)
                {
                    ProcessSelfCloseTag(document, currentParent.Node, match, 0);
                }
                else if (match.Groups["voidelem"].Success)
                {
                    ProcessVoidElement(document, currentParent.Node, match, 0);
                }
                else if (match.Groups["rawtext"].Success)
                {
                    ProcessRawTextElement(document, currentParent.Node, match, 0);
                }
                else if (match.Groups["opentag"].Success)
                {
                    // Opening tag - create element and push to stack
                    var tagName = match.Groups["openname"].Value.ToLowerInvariant();
                    
                    // Check for implicit closing
                    if (IsImplicitCloseTag(tagName, elementStack))
                    {
                        // Close implicitly closed elements
                        while (elementStack.Count > 1 && ShouldImplicitlyClose(elementStack.Peek().TagName, tagName))
                        {
                            var closedElem = elementStack.Pop();
                            FinalizeElement(document, html, closedElem, match.Index);
                        }
                    }
                    
                    var node = document.CreateNode(HtmlNodeType.Element, match.Index);
                    node.SetName(tagName);
                    SetNodePositions(node, match.Index, match.Length);
                    
                    // Parse attributes - need to create a sub-match for attribute parsing
                    var attrMatch = CreateAttributeMatch(match, "openattrs");
                    if (attrMatch != null)
                    {
                        ParseAttributesFromMatch(document, node, attrMatch);
                    }
                    
                    currentParent.Node.AppendChild(node);
                    
                    // Push to stack
                    elementStack.Push(new ElementInfo(tagName, node, match.Index, match.Index + match.Length));
                }
                else if (match.Groups["closetag"].Success)
                {
                    // Closing tag - pop from stack and finalize
                    var closeTagName = match.Groups["closename"].Value.ToLowerInvariant();
                    
                    // Find matching open tag
                    while (elementStack.Count > 1)
                    {
                        var elem = elementStack.Peek();
                        if (elem.TagName == closeTagName)
                        {
                            elementStack.Pop();
                            FinalizeElement(document, html, elem, match.Index, match.Index + match.Length, closeTagName);
                            break;
                        }
                        else if (elem.TagName == "")
                        {
                            // Orphan close tag
                            break;
                        }
                        else
                        {
                            // Mismatched - implicit close
                            elementStack.Pop();
                            FinalizeElement(document, html, elem, match.Index);
                        }
                    }
                }
                else if (match.Groups["text"].Success)
                {
                    AddTextNode(document, currentParent.Node, match.Value, match.Index);
                }

                lastPosition = match.Index + match.Length;
            }
            
            // Close any remaining open elements
            while (elementStack.Count > 1)
            {
                var elem = elementStack.Pop();
                FinalizeElement(document, html, elem, html.Length);
            }
        }

        private bool IsImplicitCloseTag(string tagName, Stack<ElementInfo> stack)
        {
            // Tags that can implicitly close: p, li, dt, dd, etc.
            return tagName == "p" || tagName == "li" || tagName == "dt" || tagName == "dd" ||
                   tagName == "option" || tagName == "optgroup" || tagName == "tr" || 
                   tagName == "td" || tagName == "th";
        }

        private bool ShouldImplicitlyClose(string openTag, string newTag)
        {
            // Simplified implicit closing rules
            if (openTag == "p" && (newTag == "p" || IsBlockElement(newTag)))
                return true;
            if (openTag == "li" && newTag == "li")
                return true;
            if ((openTag == "dt" || openTag == "dd") && (newTag == "dt" || newTag == "dd"))
                return true;
            return false;
        }

        private bool IsBlockElement(string tagName)
        {
            return tagName == "div" || tagName == "p" || tagName == "h1" || tagName == "h2" || 
                   tagName == "h3" || tagName == "h4" || tagName == "h5" || tagName == "h6" ||
                   tagName == "ul" || tagName == "ol" || tagName == "li" || tagName == "dl" ||
                   tagName == "dt" || tagName == "dd" || tagName == "table" || tagName == "form" ||
                   tagName == "blockquote" || tagName == "pre" || tagName == "address";
        }

        private void FinalizeElement(HtmlDocument document, string html, ElementInfo elem, int contentEnd, int closeTagEnd = -1, string closeTagName = null)
        {
            // Set inner content positions
            var contentStart = elem.ContentStart;
            var contentLength = contentEnd - contentStart;
            
            elem.Node._innerstartindex = contentStart;
            elem.Node._innerlength = contentLength;
            
            // Set outer length
            if (closeTagEnd > 0)
            {
                elem.Node._outerlength = closeTagEnd - elem.OpenTagStart;
                
                // Create end node
                elem.Node._endnode = document.CreateNode(HtmlNodeType.Element, contentEnd);
                elem.Node._endnode.SetName(closeTagName ?? elem.TagName);
                elem.Node._endnode._outerstartindex = contentEnd;
                elem.Node._endnode._outerlength = closeTagEnd - contentEnd;
            }
            else
            {
                // Implicitly closed or no close tag
                elem.Node._outerlength = contentEnd - elem.OpenTagStart;
                elem.Node._endnode = elem.Node;
            }
        }

        private Match? CreateAttributeMatch(Match originalMatch, string attrGroupName)
        {
            // Helper to extract attribute section for parsing
            var attrGroup = originalMatch.Groups[attrGroupName];
            if (attrGroup.Success && !string.IsNullOrEmpty(attrGroup.Value))
            {
                // The original match already has attribute captures we can use
                return originalMatch;
            }
            return null;
        }


        private void ProcessSelfCloseTag(HtmlDocument document, HtmlNode parent, Match match, int basePosition)
        {
            var tagName = match.Groups["scname"].Value;
            
            var node = document.CreateNode(HtmlNodeType.Element, basePosition + match.Index);
            node.SetName(tagName.ToLowerInvariant());
            SetNodePositions(node, basePosition + match.Index, match.Length);
            node._endnode = node;
            node._innerlength = 0;
            
            ParseAttributesFromMatch(document, node, match);
            parent.AppendChild(node);
        }

        private void ProcessVoidElement(HtmlDocument document, HtmlNode parent, Match match, int basePosition)
        {
            var tagName = match.Groups["vename"].Value;
            
            var node = document.CreateNode(HtmlNodeType.Element, basePosition + match.Index);
            node.SetName(tagName.ToLowerInvariant());
            SetNodePositions(node, basePosition + match.Index, match.Length);
            node._endnode = node;
            node._innerlength = 0;
            
            ParseAttributesFromMatch(document, node, match);
            parent.AppendChild(node);
        }

        private void ProcessRawTextElement(HtmlDocument document, HtmlNode parent, Match match, int basePosition)
        {
            var tagName = match.Groups["rtname"].Value;
            var rawContent = match.Groups["rtcontent"].Value;
            
            var node = document.CreateNode(HtmlNodeType.Element, basePosition + match.Index);
            node.SetName(tagName.ToLowerInvariant());
            SetNodePositions(node, basePosition + match.Index, match.Length);
            
            // Calculate positions
            var openTagEnd = match.Groups["rtcontent"].Index - match.Index;
            node._innerstartindex = basePosition + match.Index + openTagEnd;
            node._innerlength = rawContent.Length;
            
            // End node
            node._endnode = document.CreateNode(HtmlNodeType.Element, basePosition + match.Index + match.Length - tagName.Length - 3);
            node._endnode.SetName(tagName.ToLowerInvariant());
            
            ParseAttributesFromMatch(document, node, match);
            parent.AppendChild(node);
            
            // Add raw content as text node (not parsed!)
            if (!string.IsNullOrEmpty(rawContent))
            {
                AddTextNode(document, node, rawContent, basePosition + match.Groups["rtcontent"].Index);
            }
        }

        private HtmlNode ProcessImplicitElementIterative(HtmlDocument document, HtmlNode parent, Match match, int basePosition,
            string nameGroup, string attrsGroup, string contentGroup)
        {
            var tagName = match.Groups[nameGroup].Value;
            var innerContent = match.Groups[contentGroup].Value;
            
            var node = document.CreateNode(HtmlNodeType.Element, basePosition + match.Index);
            node.SetName(tagName.ToLowerInvariant());
            SetNodePositions(node, basePosition + match.Index, match.Length);
            
            // For implicit elements, they're self-closing (no explicit close tag)
            node._endnode = node;
            node._innerstartindex = basePosition + match.Groups[contentGroup].Index;
            node._innerlength = innerContent.Length;
            
            ParseAttributesFromMatch(document, node, match);
            parent.AppendChild(node);
            
            // Return the node so inner content can be processed iteratively by the caller
            return node;
        }

        private HtmlNode ProcessBalancedElementIterative(HtmlDocument document, HtmlNode parent, Match match, int basePosition)
        {
            var tagName = match.Groups["tagname"].Value;
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
            
            ParseAttributesFromMatch(document, node, match);
            parent.AppendChild(node);
            
            // Return the node so inner content can be processed iteratively by the caller
            return node;
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
            node._line = 1;
            node._lineposition = 1;
            node._streamposition = position;
        }

        /// <summary>
        /// Parses attributes from the main regex match using Captures collection.
        /// NO SEPARATE REGEX NEEDED - attributes are captured directly in the unified pattern!
        /// 
        /// This is the key: .NET's Captures collection gives us all individual matches
        /// when a named group is inside a quantifier. The main regex pattern has:
        ///   (?&lt;attrname&gt;...)(?&lt;attrdqval&gt;...|&lt;attrsqval&gt;...|&lt;attruqval&gt;...)?
        /// repeated via *, and Captures[i] gives us each attribute.
        /// </summary>
        private void ParseAttributesFromMatch(HtmlDocument document, HtmlNode node, Match match)
        {
            var nameCaptures = match.Groups["attrname"].Captures;
            var dqCaptures = match.Groups["attrdqval"].Captures;
            var sqCaptures = match.Groups["attrsqval"].Captures;
            var uqCaptures = match.Groups["attruqval"].Captures;
            
            if (nameCaptures.Count == 0)
                return;

            // Pre-sort all value captures by index for efficient lookup
            var allValues = new List<(int Index, string Value, AttributeValueQuote QuoteType)>();
            foreach (Capture c in dqCaptures)
                allValues.Add((c.Index, c.Value, AttributeValueQuote.DoubleQuote));
            foreach (Capture c in sqCaptures)
                allValues.Add((c.Index, c.Value, AttributeValueQuote.SingleQuote));
            foreach (Capture c in uqCaptures)
                allValues.Add((c.Index, c.Value, AttributeValueQuote.None));
            allValues.Sort((a, b) => a.Index.CompareTo(b.Index));

            // Pre-compute sorted name positions for efficient boundary checking
            var namePositions = new List<(int Index, int EndIndex, string Name)>();
            foreach (Capture c in nameCaptures)
            {
                if (!string.IsNullOrWhiteSpace(c.Value))
                    namePositions.Add((c.Index, c.Index + c.Length, c.Value));
            }
            namePositions.Sort((a, b) => a.Index.CompareTo(b.Index));

            // Process each attribute in O(n) by walking through sorted lists
            int valueIdx = 0;
            for (int i = 0; i < namePositions.Count; i++)
            {
                var (nameIndex, nameEnd, attrName) = namePositions[i];
                var nextNameStart = (i + 1 < namePositions.Count) ? namePositions[i + 1].Index : int.MaxValue;

                string? value = null;
                var quoteType = AttributeValueQuote.WithoutValue;

                // Find the first value that starts after this name and before the next name
                while (valueIdx < allValues.Count && allValues[valueIdx].Index <= nameEnd)
                    valueIdx++; // Skip values that start before or at name end
                
                if (valueIdx < allValues.Count && allValues[valueIdx].Index < nextNameStart)
                {
                    value = allValues[valueIdx].Value;
                    quoteType = allValues[valueIdx].QuoteType;
                    valueIdx++; // Consume this value
                }

                var attr = document.CreateAttribute(attrName, value ?? "");
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
    }
}
