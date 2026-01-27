using System.Text.RegularExpressions;

namespace HtmlAgilityPack.RegexParser
{
    /// <summary>
    /// All regex patterns used for HTML parsing.
    /// Patterns are compiled for performance.
    /// 
    /// This is where the regex magic happens. Each pattern is documented with
    /// what it matches and any .NET-specific features it uses.
    /// </summary>
    public static class HtmlPatterns
    {
        private const RegexOptions DefaultOptions = 
            RegexOptions.Compiled | 
            RegexOptions.IgnoreCase | 
            RegexOptions.CultureInvariant;

        private const RegexOptions MultilineOptions = 
            DefaultOptions | 
            RegexOptions.Singleline;  // . matches newlines

        private const RegexOptions VerboseOptions = 
            DefaultOptions | 
            RegexOptions.IgnorePatternWhitespace;

        #region Pass 1: Pre-extraction patterns (comments, CDATA, script/style)

        /// <summary>
        /// Matches HTML comments: &lt;!-- ... --&gt;
        /// Group "content" captures the comment body.
        /// </summary>
        public static readonly Regex Comment = new Regex(
            @"<!--(?<content>.*?)-->",
            MultilineOptions);

        /// <summary>
        /// Matches CDATA sections: &lt;![CDATA[ ... ]]&gt;
        /// Group "content" captures the CDATA body.
        /// </summary>
        public static readonly Regex CData = new Regex(
            @"<!\[CDATA\[(?<content>.*?)\]\]>",
            MultilineOptions);

        /// <summary>
        /// Matches server-side code blocks: &lt;% ... %&gt; or &lt;%= ... %&gt;
        /// Group "content" captures the code.
        /// </summary>
        public static readonly Regex ServerSideCode = new Regex(
            @"<%(?<content>.*?)%>",
            MultilineOptions);

        /// <summary>
        /// Matches script tags with their content.
        /// Uses balancing groups? No - script content is not recursive.
        /// Just captures everything until &lt;/script&gt;.
        /// Groups: "attrs" for attributes, "content" for script body.
        /// </summary>
        public static readonly Regex ScriptTag = new Regex(
            @"<script(?<attrs>[^>]*)>(?<content>.*?)</script\s*>",
            MultilineOptions);

        /// <summary>
        /// Matches style tags with their content.
        /// Groups: "attrs" for attributes, "content" for style body.
        /// </summary>
        public static readonly Regex StyleTag = new Regex(
            @"<style(?<attrs>[^>]*)>(?<content>.*?)</style\s*>",
            MultilineOptions);

        /// <summary>
        /// Matches textarea tags with their content (content is raw text).
        /// Groups: "attrs" for attributes, "content" for textarea body.
        /// </summary>
        public static readonly Regex TextAreaTag = new Regex(
            @"<textarea(?<attrs>[^>]*)>(?<content>.*?)</textarea\s*>",
            MultilineOptions);

        #endregion

        #region Pass 2: Master tokenizer

        /// <summary>
        /// Master tokenizer pattern - breaks HTML into tokens.
        /// This is a large alternation that matches different HTML constructs.
        /// 
        /// Order matters! More specific patterns must come before general ones.
        /// 
        /// Groups:
        /// - doctype: DOCTYPE declaration
        /// - comment: HTML comment
        /// - cdata: CDATA section
        /// - servercode: Server-side code
        /// - selfclose, scname, scattrs: Self-closing tag
        /// - opentag, otname, otattrs: Opening tag
        /// - closetag, ctname: Closing tag
        /// - text: Text content
        /// </summary>
        public static readonly Regex MasterTokenizer = new Regex(
            @"
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
            VerboseOptions | RegexOptions.Singleline);

        #endregion

        #region Pass 3: Attribute parser

        /// <summary>
        /// Parses attributes from a tag's attribute string.
        /// Handles:
        /// - name="value" (double-quoted)
        /// - name='value' (single-quoted)  
        /// - name=value (unquoted)
        /// - name (boolean/empty attribute)
        /// 
        /// Groups:
        /// - name: Attribute name
        /// - dqval: Double-quoted value
        /// - sqval: Single-quoted value
        /// - uqval: Unquoted value
        /// </summary>
        public static readonly Regex AttributeParser = new Regex(
            @"
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
            VerboseOptions);

        #endregion

        #region Pass 4: Balancing group patterns for nested content

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
            return new Regex(pattern, VerboseOptions | RegexOptions.Singleline);
        }

        #endregion

        #region HTML5 void elements (self-closing by spec)

        /// <summary>
        /// HTML5 void elements - these have no closing tag.
        /// </summary>
        public static readonly HashSet<string> VoidElements = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase)
        {
            "area", "base", "br", "col", "embed", "hr", "img", "input",
            "link", "meta", "param", "source", "track", "wbr",
            // Obsolete but still void
            "basefont", "bgsound", "frame", "isindex", "keygen"
        };

        /// <summary>
        /// Elements whose content is raw text (not parsed as HTML).
        /// </summary>
        public static readonly HashSet<string> RawTextElements = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase)
        {
            "script", "style", "textarea", "title", "xmp", "plaintext", "listing"
        };

        /// <summary>
        /// Checks if a tag name is a void element.
        /// </summary>
        public static bool IsVoidElement(string tagName)
        {
            return VoidElements.Contains(tagName);
        }

        /// <summary>
        /// Checks if a tag name is a raw text element.
        /// </summary>
        public static bool IsRawTextElement(string tagName)
        {
            return RawTextElements.Contains(tagName);
        }

        #endregion
    }
}
