using System.Text.RegularExpressions;

namespace HtmlAgilityPack.RegexParser
{
    /// <summary>
    /// All regex patterns used for HTML parsing.
    /// Uses .NET 7+ source generators for compile-time pattern generation.
    /// 
    /// This is where the regex magic happens. Each pattern is documented with
    /// what it matches and any .NET-specific features it uses.
    /// </summary>
    public static partial class HtmlPatterns
    {
        #region Element Pattern Constants (Shared across all parsers)
        
        /// <summary>
        /// Pattern string for void elements that don't need closing tags.
        /// Used in both source-generated and runtime-composed regex patterns.
        /// </summary>
        public const string VoidElementsPattern = "area|base|br|col|embed|hr|img|input|link|meta|param|source|track|wbr|basefont|bgsound|frame|isindex|keygen";
        
        /// <summary>
        /// Pattern string for raw text elements whose content is NOT parsed as HTML.
        /// Used in both source-generated and runtime-composed regex patterns.
        /// </summary>
        public const string RawTextElementsPattern = "script|style|textarea|title|xmp|plaintext|listing";
        
        /// <summary>
        /// Pattern string for block elements (for implicit closing of p).
        /// Used in both source-generated and runtime-composed regex patterns.
        /// </summary>
        public const string BlockElementsPattern = "address|article|aside|blockquote|canvas|dd|div|dl|dt|fieldset|figcaption|figure|footer|form|h[1-6]|header|hgroup|hr|li|main|nav|noscript|ol|p|pre|section|table|tfoot|ul|video";
        
        /// <summary>
        /// Pattern string for implicit closing tags (p element).
        /// </summary>
        public const string ImplicitCloseTagsPPattern = "p";
        
        /// <summary>
        /// Pattern string for implicit closing tags (li element).
        /// </summary>
        public const string ImplicitCloseTagsLiPattern = "li";
        
        /// <summary>
        /// Pattern string for implicit closing tags (dt/dd elements).
        /// </summary>
        public const string ImplicitCloseTagsDtPattern = "dt|dd";
        
        /// <summary>
        /// Pattern string for attribute matching (single attribute with name and optional value).
        /// Captures attrname, attrdqval (double-quoted), attrsqval (single-quoted), attruqval (unquoted).
        /// 
        /// Note: Uses prefixed group names (attrname, attrdqval, etc.) to avoid collisions when
        /// embedded in larger patterns like PureRegexParser's unified regex. The standalone
        /// AttributeParser() uses shorter names (name, dqval, etc.) since it operates independently.
        /// </summary>
        public const string SingleAttributePattern = """
            \s+                                  # Whitespace before attribute (required)
            (?<attrname>[^\s=/>"']+)            # Attribute name
            (?:
                \s*=\s*                          # = with optional whitespace
                (?:
                    "(?<attrdqval>[^"]*)"     # Double-quoted value
                    |
                    '(?<attrsqval>[^']*)'        # Single-quoted value
                    |
                    (?<attruqval>[^\s>"']+)     # Unquoted value
                )
            )?                                   # Value is optional (boolean attrs)
            """;
        
        /// <summary>
        /// Pattern string for attribute section - captures ALL attributes via repeated SingleAttribute.
        /// </summary>
        public const string AttributeSectionPattern = """(?:\s+(?<attrname>[^\s=/>"']+)(?:\s*=\s*(?:"(?<attrdqval>[^"]*)"|'(?<attrsqval>[^']*)'|(?<attruqval>[^\s>"']+)))?)*""";

        #endregion

        #region Source-Generated Patterns (Compile-time validated!)

        /// <summary>
        /// Matches HTML comments: &lt;!-- ... --&gt;
        /// </summary>
        [GeneratedRegex(@"<!--(?<content>.*?)-->", RegexOptions.Singleline | RegexOptions.Compiled)]
        public static partial Regex Comment();

        /// <summary>
        /// Matches CDATA sections: &lt;![CDATA[ ... ]]&gt;
        /// </summary>
        [GeneratedRegex(@"<!\[CDATA\[(?<content>.*?)\]\]>", RegexOptions.Singleline | RegexOptions.Compiled)]
        public static partial Regex CData();

        /// <summary>
        /// Matches server-side code blocks: &lt;% ... %&gt;
        /// </summary>
        [GeneratedRegex(@"<%(?<content>.*?)%>", RegexOptions.Singleline | RegexOptions.Compiled)]
        public static partial Regex ServerSideCode();

        #endregion

        #region Master Tokenizer (Source Generated)

        /// <summary>
        /// Master tokenizer - breaks HTML into tokens.
        /// Uses NonBacktracking for safety against ReDoS.
        /// Uses nested capturing groups to extract content directly, avoiding double-parsing.
        /// </summary>
        [GeneratedRegex("""
            (?<doctype><!DOCTYPE[^>]*>)                           # DOCTYPE
            |
            (?<comment><!--(?<commentcontent>.*?)-->)             # Comment with nested content capture
            |
            (?<cdata><!\[CDATA\[(?<cdatacontent>.*?)\]\]>)        # CDATA with nested content capture
            |
            (?<servercode><%(?<servercodecontent>.*?)%>)          # Server-side code with nested content capture
            |
            (?<selfclose>
                <(?<scname>[a-zA-Z][a-zA-Z0-9:-]*)                 # Tag name
                (?<scattrs>[^>]*)                                 # Attributes
                \s*/\s*>                                          # Self-closing />
            )
            |
            (?<opentag>
                <(?<otname>[a-zA-Z][a-zA-Z0-9:-]*)                 # Tag name
                (?<otattrs>                                       # Attributes section
                    (?:[^>"']*                                   # Non-quote, non->
                        |"[^"]*"                               # Double-quoted value
                        |'[^']*'                                  # Single-quoted value
                    )*
                )
                \s*>                                              # Closing >
            )
            |
            (?<closetag>
                </(?<ctname>[a-zA-Z][a-zA-Z0-9:-]*)\s*>            # Closing tag
            )
            |
            (?<text>[^<]+)                                        # Text content
            """,
            RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Compiled)]
        public static partial Regex MasterTokenizer();

        #endregion

        #region Attribute Parser (Source Generated)

        /// <summary>
        /// Parses attributes from a tag's attribute string.
        /// </summary>
        [GeneratedRegex("""
            (?<name>[^\s=/>"']+)                   # Attribute name
            (?:
                \s*=\s*                             # = with optional whitespace
                (?:
                    "(?<dqval>[^"]*)"            # Double-quoted value
                    |
                    '(?<sqval>[^']*)'               # Single-quoted value
                    |
                    (?<uqval>[^\s>"']+)            # Unquoted value
                )
            )?                                      # Value is optional (boolean attrs)
            """,
            RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled)]
        public static partial Regex AttributeParser();

        #endregion

        #region Element Classification (REGEX instead of HashSets!)

        /// <summary>
        /// Combined element classifier using nested capturing groups.
        /// Classifies an element as void, raw text, or block in a single regex match.
        /// Group structure:
        /// - "void": matches void elements
        /// - "rawtext": matches raw text elements  
        /// - "block": matches block elements
        /// </summary>
        [GeneratedRegex("""
            ^(?:
            (?<void>area|base|br|col|embed|hr|img|input|link|meta|param|source|track|wbr|basefont|bgsound|frame|isindex|keygen)
            |
            (?<rawtext>script|style|textarea|title|xmp|plaintext|listing)
            |
            (?<block>address|article|aside|blockquote|canvas|dd|div|dl|dt|fieldset|figcaption|figure|footer|form|h[1-6]|header|hgroup|hr|li|main|nav|noscript|ol|p|pre|section|table|tfoot|ul|video)
            )$
            """,
            RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        public static partial Regex ElementClassifier();

        /// <summary>
        /// Classification result for an element.
        /// </summary>
        public enum ElementClass
        {
            /// <summary>Not a special element type.</summary>
            None,
            /// <summary>Void element (self-closing by spec).</summary>
            Void,
            /// <summary>Raw text element (content not parsed as HTML).</summary>
            RawText,
            /// <summary>Block element (for implicit closing of p).</summary>
            Block
        }

        /// <summary>
        /// Classifies an element using a single regex match with nested capturing groups.
        /// More efficient than calling three separate IsXxx methods.
        /// </summary>
        public static ElementClass ClassifyElement(string tagName)
        {
            var match = ElementClassifier().Match(tagName);
            if (!match.Success) return ElementClass.None;
            
            if (match.Groups["void"].Success) return ElementClass.Void;
            if (match.Groups["rawtext"].Success) return ElementClass.RawText;
            if (match.Groups["block"].Success) return ElementClass.Block;
            
            return ElementClass.None;
        }

        /// <summary>
        /// Check if tag is void element using regex.
        /// </summary>
        public static bool IsVoidElement(string tagName) => 
            ClassifyElement(tagName) == ElementClass.Void;

        /// <summary>
        /// Check if tag is raw text element using regex.
        /// </summary>
        public static bool IsRawTextElement(string tagName) => 
            ClassifyElement(tagName) == ElementClass.RawText;

        /// <summary>
        /// Check if tag is block element using regex.
        /// </summary>
        public static bool IsBlockElement(string tagName) => 
            ClassifyElement(tagName) == ElementClass.Block;

        #endregion

        #region Implicit Closing Rules (REGEX for HTML5 spec!)

        /// <summary>
        /// Pattern to detect implicit closing scenarios.
        /// Format: "currentTag:newTag" - if matches, currentTag should close.
        /// </summary>
        [GeneratedRegex(@"^(
            p:(address|article|aside|blockquote|div|dl|fieldset|footer|form|h[1-6]|header|hgroup|hr|main|nav|ol|p|pre|section|table|ul)|
            li:li|
            dt:(dt|dd)|
            dd:(dt|dd)|
            td:(td|th|tr)|
            th:(td|th|tr)|
            tr:tr|
            option:option|
            optgroup:optgroup|
            rb:(rb|rt|rtc|rp)|
            rt:(rb|rt|rtc|rp)|
            rtc:(rb|rtc|rp)|
            rp:(rb|rt|rtc|rp)
        )$", RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        public static partial Regex ImplicitClosePattern();

        /// <summary>
        /// Check if newTag should implicitly close currentTag.
        /// Uses regex instead of a maze of if-statements!
        /// </summary>
        public static bool ShouldImplicitlyClose(string currentTag, string newTag)
        {
            return ImplicitClosePattern().IsMatch($"{currentTag}:{newTag}");
        }

        #endregion

        #region Balancing Groups Pattern Factory

        /// <summary>
        /// Creates a pattern that matches a balanced tag with its content.
        /// Uses .NET balancing groups to track nesting depth.
        /// 
        /// THIS IS THE "IMPOSSIBLE" PATTERN - matching nested same-tags.
        /// </summary>
        public static Regex CreateBalancedTagPattern(string tagName)
        {
            var escaped = Regex.Escape(tagName);
            var pattern = $@"
                <{escaped}(?<attrs>[^>]*)>              # Opening tag with attributes
                (?<content>                             # Capture content
                  (?>                                   # Atomic group (no backtracking)
                    [^<]+                               # Text content
                    |
                    <{escaped}\b[^>]*> (?<DEPTH>)       # Nested same tag: PUSH
                    |
                    </{escaped}\s*> (?<-DEPTH>)         # Closing same tag: POP
                    |
                    <(?!/?{escaped}\b)[^>]*>            # Other tags: pass through
                  )*
                )
                (?(DEPTH)(?!))                          # FAIL if stack not empty
                </{escaped}\s*>                         # Final closing tag
            ";
            return new Regex(pattern, 
                RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Creates a pattern that extracts raw text content using balancing groups for quote tracking.
        /// This handles edge cases where the closing tag pattern appears inside quoted strings.
        /// 
        /// Used by RegexTokenizer.ExtractRawTextContent to safely extract script/style content
        /// even when it contains strings like: var x = '&lt;/script&gt;';
        /// </summary>
        /// <param name="tagName">The raw text element tag name (e.g., "script", "style")</param>
        /// <returns>A regex that extracts content and closing tag, respecting quoted strings</returns>
        public static Regex CreateRawTextContentPattern(string tagName)
        {
            var escaped = Regex.Escape(tagName);
            
            // Pattern uses balancing groups to track quote context:
            // - DQ stack tracks double quotes
            // - SQ stack tracks single quotes
            // The closing tag only matches when both stacks are empty (outside quotes)
            var pattern = $"""
                (?<content>
                  (?>
                    # Double-quoted string - use balancing group to track
                    "(?<DQ>)                                           # Start double quote: PUSH
                    (?:[^"\\]|\\.)*                                    # String content (with escapes)
                    "(?<-DQ>)                                          # End double quote: POP
                    |
                    # Single-quoted string - use balancing group to track
                    '(?<SQ>)                                            # Start single quote: PUSH
                    (?:[^'\\]|\\.)*                                     # String content (with escapes)
                    '(?<-SQ>)                                           # End single quote: POP
                    |
                    # Regular content (not quote, not potential closing tag start)
                    [^"'<]+                                            # Text without quotes or <
                    |
                    # < that's not the start of our closing tag
                    <(?!/{escaped}\s*>)
                  )*
                )
                (?(DQ)(?!))                                             # FAIL if DQ stack not empty
                (?(SQ)(?!))                                             # FAIL if SQ stack not empty
                (?<closetag></{escaped}\s*>)                            # Closing tag (only when outside quotes)
            """;
            return new Regex(pattern, 
                RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        #endregion

        #region Line Counting (Regex-based!)

        /// <summary>
        /// Pattern for finding newlines. Used by LineTracker.
        /// </summary>
        [GeneratedRegex(@"\n", RegexOptions.Compiled)]
        public static partial Regex NewlinePattern();

        #endregion
    }
}
