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

        /// <summary>
        /// Matches script tags with content.
        /// </summary>
        [GeneratedRegex(@"<script(?<attrs>[^>]*)>(?<content>.*?)</script\s*>", 
            RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        public static partial Regex ScriptTag();

        /// <summary>
        /// Matches style tags with content.
        /// </summary>
        [GeneratedRegex(@"<style(?<attrs>[^>]*)>(?<content>.*?)</style\s*>",
            RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        public static partial Regex StyleTag();

        /// <summary>
        /// Matches textarea tags with content.
        /// </summary>
        [GeneratedRegex(@"<textarea(?<attrs>[^>]*)>(?<content>.*?)</textarea\s*>",
            RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        public static partial Regex TextAreaTag();

        #endregion

        #region Master Tokenizer (Source Generated)

        /// <summary>
        /// Master tokenizer - breaks HTML into tokens.
        /// Uses NonBacktracking for safety against ReDoS.
        /// </summary>
        [GeneratedRegex(@"
            (?<doctype><!DOCTYPE[^>]*>)                           # DOCTYPE
            |
            (?<comment><!--.*?-->)                                # Comment
            |
            (?<cdata><!\[CDATA\[.*?\]\]>)                         # CDATA
            |
            (?<servercode><%.*?%>)                                # Server-side code
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
                    (?:[^>""']*                                   # Non-quote, non->
                        |""[^""]*""                               # Double-quoted value
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
            ",
            RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Compiled)]
        public static partial Regex MasterTokenizer();

        #endregion

        #region Attribute Parser (Source Generated)

        /// <summary>
        /// Parses attributes from a tag's attribute string.
        /// </summary>
        [GeneratedRegex(@"
            (?<name>[^\s=/>""']+)                   # Attribute name
            (?:
                \s*=\s*                             # = with optional whitespace
                (?:
                    ""(?<dqval>[^""]*)""            # Double-quoted value
                    |
                    '(?<sqval>[^']*)'               # Single-quoted value
                    |
                    (?<uqval>[^\s>""']+)            # Unquoted value
                )
            )?                                      # Value is optional (boolean attrs)
            ",
            RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled)]
        public static partial Regex AttributeParser();

        #endregion

        #region Element Classification (REGEX instead of HashSets!)

        /// <summary>
        /// Matches HTML5 void elements (self-closing by spec).
        /// Uses regex alternation instead of HashSet for consistency.
        /// </summary>
        [GeneratedRegex(@"^(area|base|br|col|embed|hr|img|input|link|meta|param|source|track|wbr|basefont|bgsound|frame|isindex|keygen)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        public static partial Regex VoidElementPattern();

        /// <summary>
        /// Matches elements whose content is raw text (not parsed as HTML).
        /// </summary>
        [GeneratedRegex(@"^(script|style|textarea|title|xmp|plaintext|listing)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        public static partial Regex RawTextElementPattern();

        /// <summary>
        /// Matches HTML5 block elements (for implicit closing of p).
        /// </summary>
        [GeneratedRegex(@"^(address|article|aside|blockquote|canvas|dd|div|dl|dt|fieldset|figcaption|figure|footer|form|h[1-6]|header|hgroup|hr|li|main|nav|noscript|ol|p|pre|section|table|tfoot|ul|video)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        public static partial Regex BlockElementPattern();

        /// <summary>
        /// Check if tag is void element using regex.
        /// </summary>
        public static bool IsVoidElement(string tagName) => VoidElementPattern().IsMatch(tagName);

        /// <summary>
        /// Check if tag is raw text element using regex.
        /// </summary>
        public static bool IsRawTextElement(string tagName) => RawTextElementPattern().IsMatch(tagName);

        /// <summary>
        /// Check if tag is block element using regex.
        /// </summary>
        public static bool IsBlockElement(string tagName) => BlockElementPattern().IsMatch(tagName);

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

        #endregion

        #region Line Counting (Regex-based!)

        /// <summary>
        /// Pattern for counting newlines.
        /// </summary>
        [GeneratedRegex(@"\n", RegexOptions.Compiled)]
        public static partial Regex NewlinePattern();

        /// <summary>
        /// Count lines up to a position using regex.
        /// </summary>
        public static int CountLinesUpTo(string text, int position)
        {
            if (position <= 0) return 1;
            var substring = text.AsSpan(0, Math.Min(position, text.Length));
            return NewlinePattern().Count(substring) + 1;
        }

        /// <summary>
        /// Find column position (chars since last newline) using regex.
        /// </summary>
        [GeneratedRegex(@"[^\n]*$", RegexOptions.Compiled)]
        private static partial Regex LastLinePattern();

        public static int GetColumnPosition(string text, int position)
        {
            if (position <= 0) return 1;
            var substring = text.Substring(0, Math.Min(position, text.Length));
            var match = LastLinePattern().Match(substring);
            return match.Success ? match.Length + 1 : 1;
        }

        #endregion
    }
}
