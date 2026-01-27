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
    /// This implementation uses a SINGLE UNIFIED REGEX built via string composition
    /// that handles:
    /// - Nested balanced elements (the "impossible" case)
    /// - Raw text elements (script, style, textarea) - content not parsed as HTML
    /// - Implicit tag closing rules (p, li, dt, dd, etc.)
    /// - Void elements (br, img, input, etc.)
    /// - Self-closing syntax
    /// - Comments and DOCTYPE
    /// </summary>
    public class PureRegexParser : IHtmlParser
    {
        /// <inheritdoc />
        public string ParserName => "Pure Regex (Single-Pass Balancing Groups)";

        // ============================================================================
        // SINGLE UNIFIED REGEX - Built via string composition
        // ============================================================================
        // All patterns are composed into ONE regex at construction time.
        // This proves that pure regex (with .NET balancing groups) can parse HTML.
        // ============================================================================

        #region Pattern Components (Composed into single regex)

        // Void elements that don't need closing tags
        private const string VoidElements = "area|base|br|col|embed|hr|img|input|link|meta|param|source|track|wbr|basefont|bgsound|frame|isindex|keygen";
        
        // Raw text elements whose content is NOT parsed as HTML
        private const string RawTextElements = "script|style|textarea|title|xmp|plaintext|listing";
        
        // Tags that implicitly close themselves (p closes when another p/block starts, li closes on li, etc.)
        private const string ImplicitCloseTagsP = "p";
        private const string BlockElements = "address|article|aside|blockquote|canvas|dd|div|dl|dt|fieldset|figcaption|figure|footer|form|h[1-6]|header|hgroup|hr|li|main|nav|noscript|ol|p|pre|section|table|tfoot|ul|video";
        private const string ImplicitCloseTagsLi = "li";
        private const string ImplicitCloseTagsDt = "dt|dd";
        
        // ============================================================================
        // ATTRIBUTE PATTERN - Embedded directly into the main regex!
        // ============================================================================
        // This pattern captures INDIVIDUAL ATTRIBUTES using .NET's Captures collection.
        // When a named group is inside a quantifier, match.Groups["name"].Captures
        // gives you ALL the individual matches - no separate regex needed!
        // ============================================================================
        
        // Single attribute pattern - captures name and value in one match
        // Uses alternation for the three value types: double-quoted, single-quoted, unquoted
        private const string SingleAttribute = @"
            \s+                                  # Whitespace before attribute (required)
            (?<attrname>[^\s=/>""']+)            # Attribute name
            (?:
                \s*=\s*                          # = with optional whitespace
                (?:
                    ""(?<attrdqval>[^""]*)""     # Double-quoted value
                    |
                    '(?<attrsqval>[^']*)'        # Single-quoted value
                    |
                    (?<attruqval>[^\s>""']+)     # Unquoted value
                )
            )?                                   # Value is optional (boolean attrs)
            ";
        
        // Attribute section pattern - captures ALL attributes via repeated SingleAttribute
        // The magic: each capture of attrname/attrdqval/etc goes into Captures collection!
        private const string AttributeSection = @"(?:" + SingleAttribute + @")*";

        #endregion

        #region The SINGLE UNIFIED REGEX (No separate attribute regex!)

        private static readonly Regex _unifiedPattern;
        
        static PureRegexParser()
        {
            // Build THE SINGLE UNIFIED PATTERN via string composition
            // This is the heart of the "impossible" parser
            var pattern = $@"
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
                            <(?:{VoidElements})\b[^>]*>                     # Nested void elements
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
                            <(?:{VoidElements})\b[^>]*/?\s*>                # Void elements  
                            |
                            <[a-zA-Z][a-zA-Z0-9:-]*[^>]*/\s*>               # Self-closing
                            |
                            <(?!/?(?:{ImplicitCloseTagsP}|{BlockElements})\b)[^>]+>  # Other non-block tags
                        )*?
                    )
                    (?=<(?:{ImplicitCloseTagsP}|{BlockElements})\b|</|$)    # Lookahead: closes before next p/block
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
            ";

            _unifiedPattern = new Regex(pattern,
                RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase);
            
            // NO SEPARATE ATTRIBUTE REGEX!
            // Attributes are captured directly in the main pattern via attrname/attrdqval/attrsqval/attruqval groups
            // Using .NET's Captures collection to get all attribute matches
        }

        #endregion

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

            // Single-pass parsing using THE UNIFIED REGEX
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
                // THE SINGLE REGEX does all the work
                var match = _unifiedPattern.Match(html, currentPos);
                
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
                    var node = document.CreateNode(HtmlNodeType.Comment, basePosition + match.Index);
                    SetNodePositions(node, basePosition + match.Index, match.Length);
                    parent.AppendChild(node);
                    currentPos = match.Index + match.Length;
                }
                else if (match.Groups["comment"].Success)
                {
                    var node = document.CreateNode(HtmlNodeType.Comment, basePosition + match.Index);
                    SetNodePositions(node, basePosition + match.Index, match.Length);
                    parent.AppendChild(node);
                    currentPos = match.Index + match.Length;
                }
                else if (match.Groups["selfclose"].Success)
                {
                    ProcessSelfCloseTag(document, parent, match, basePosition);
                    currentPos = match.Index + match.Length;
                }
                else if (match.Groups["voidelem"].Success)
                {
                    ProcessVoidElement(document, parent, match, basePosition);
                    currentPos = match.Index + match.Length;
                }
                else if (match.Groups["rawtext"].Success)
                {
                    // RAW TEXT ELEMENTS - script, style, textarea
                    // Content is preserved as-is, not parsed as HTML
                    ProcessRawTextElement(document, parent, match, basePosition);
                    currentPos = match.Index + match.Length;
                }
                else if (match.Groups["implicit_p"].Success)
                {
                    // IMPLICIT CLOSE - <p> tags
                    ProcessImplicitElement(document, parent, match, basePosition, "ipname", "ipattrs", "ipcontent");
                    currentPos = match.Index + match.Length;
                }
                else if (match.Groups["implicit_li"].Success)
                {
                    // IMPLICIT CLOSE - <li> tags  
                    ProcessImplicitElement(document, parent, match, basePosition, "ilname", "ilattrs", "ilcontent");
                    currentPos = match.Index + match.Length;
                }
                else if (match.Groups["implicit_dt"].Success)
                {
                    // IMPLICIT CLOSE - <dt>/<dd> tags
                    ProcessImplicitElement(document, parent, match, basePosition, "idtname", "idtattrs", "idtcontent");
                    currentPos = match.Index + match.Length;
                }
                else if (match.Groups["balanced"].Success)
                {
                    ProcessBalancedElement(document, parent, match, basePosition);
                    currentPos = match.Index + match.Length;
                }
                else if (match.Groups["text"].Success)
                {
                    AddTextNode(document, parent, match.Value, basePosition + match.Index);
                    currentPos = match.Index + match.Length;
                }
                else if (match.Groups["orphanclose"].Success)
                {
                    // Orphan close tag - skip it
                    currentPos = match.Index + match.Length;
                }
                else
                {
                    currentPos = match.Index + match.Length;
                }
            }
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

        private void ProcessImplicitElement(HtmlDocument document, HtmlNode parent, Match match, int basePosition,
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
            
            // Recursively parse inner content
            if (!string.IsNullOrEmpty(innerContent))
            {
                ParseRecursive(document, node, innerContent, basePosition + match.Groups[contentGroup].Index);
            }
        }

        private void ProcessBalancedElement(HtmlDocument document, HtmlNode parent, Match match, int basePosition)
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
            
            // Recursively parse inner content
            if (!string.IsNullOrEmpty(innerContent))
            {
                ParseRecursive(document, node, innerContent, basePosition + innerStart);
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
