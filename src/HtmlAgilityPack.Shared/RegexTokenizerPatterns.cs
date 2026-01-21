// Description: Regex-based HTML tokenization patterns for HtmlAgilityPack.
// Uses .NET's balancing groups feature for handling nested HTML structures.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace HtmlAgilityPack
{
    /// <summary>
    /// Contains all regex patterns used for HTML tokenization and parsing.
    /// Leverages .NET's balancing groups feature for handling nested HTML structures.
    /// </summary>
    internal static class RegexTokenizerPatterns
    {
        /// <summary>
        /// Timeout for regex operations to prevent catastrophic backtracking.
        /// </summary>
        public static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(10);

        /// <summary>
        /// HTML5 void elements that don't have closing tags.
        /// </summary>
        public static readonly HashSet<string> VoidElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "area", "base", "br", "col", "embed", "hr", "img", "input",
            "link", "meta", "param", "source", "track", "wbr",
            // Obsolete but still recognized
            "basefont", "bgsound", "frame", "isindex", "keygen", "spacer"
        };

        /// <summary>
        /// Elements whose content should be treated as raw text (not parsed).
        /// </summary>
        public static readonly HashSet<string> RawTextElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "script", "style", "textarea", "title", "noxhtml"
        };

        /// <summary>
        /// Pattern to match HTML comments: &lt;!-- ... --&gt;
        /// </summary>
        public static readonly Regex CommentPattern = new Regex(
            @"<!--(?<content>.*?)-->",
            RegexOptions.Singleline | RegexOptions.Compiled,
            RegexTimeout);

        /// <summary>
        /// Pattern to match DOCTYPE declarations.
        /// </summary>
        public static readonly Regex DoctypePattern = new Regex(
            @"<!DOCTYPE\s+(?<content>[^>]*)>",
            RegexOptions.IgnoreCase | RegexOptions.Compiled,
            RegexTimeout);

        /// <summary>
        /// Pattern to match CDATA sections: &lt;![CDATA[ ... ]]&gt;
        /// </summary>
        public static readonly Regex CDataPattern = new Regex(
            @"<!\[CDATA\[(?<content>.*?)\]\]>",
            RegexOptions.Singleline | RegexOptions.Compiled,
            RegexTimeout);

        /// <summary>
        /// Pattern to match self-closing tags: &lt;tag /&gt; or &lt;tag/&gt;
        /// </summary>
        public static readonly Regex SelfClosingTagPattern = new Regex(
            @"<(?<tagName>[a-zA-Z][a-zA-Z0-9:-]*)\s*(?<attrs>[^>]*?)\s*/>",
            RegexOptions.Compiled,
            RegexTimeout);

        /// <summary>
        /// Pattern to match opening tags with optional attributes.
        /// </summary>
        public static readonly Regex OpeningTagPattern = new Regex(
            @"<(?<tagName>[a-zA-Z][a-zA-Z0-9:-]*)\s*(?<attrs>[^>]*?)(?<!/)>",
            RegexOptions.Compiled,
            RegexTimeout);

        /// <summary>
        /// Pattern to match closing tags.
        /// </summary>
        public static readonly Regex ClosingTagPattern = new Regex(
            @"</(?<tagName>[a-zA-Z][a-zA-Z0-9:-]*)\s*>",
            RegexOptions.Compiled,
            RegexTimeout);

        /// <summary>
        /// Pattern to parse individual attributes.
        /// Handles: name="value", name='value', name=value, name (no value)
        /// </summary>
        public static readonly Regex AttributePattern = new Regex(
            @"(?<name>[^\s=""'<>/]+)(?:\s*=\s*(?:""(?<dqValue>[^""]*)""|'(?<sqValue>[^']*)'|(?<uqValue>[^\s""'=<>`]+)))?",
            RegexOptions.Compiled,
            RegexTimeout);

        /// <summary>
        /// Master tokenizer pattern that splits HTML into tokens.
        /// Uses alternation to match comments, doctypes, tags, and text in order of priority.
        /// </summary>
        public static readonly Regex TokenizerPattern = new Regex(
            @"(?<comment><!--.*?-->)" +
            @"|(?<doctype><!DOCTYPE\s+[^>]*>)" +
            @"|(?<cdata><!\[CDATA\[.*?\]\]>)" +
            @"|(?<selfClosing><[a-zA-Z][a-zA-Z0-9:-]*\s*[^>]*?/>)" +
            @"|(?<closing></[a-zA-Z][a-zA-Z0-9:-]*\s*>)" +
            @"|(?<opening><[a-zA-Z][a-zA-Z0-9:-]*(?:\s+[^>]*)?>)" +
            @"|(?<text>[^<]+)",
            RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled,
            RegexTimeout);

        /// <summary>
        /// Pattern to match raw text element content (for script, style, etc.).
        /// This pattern matches everything until the closing tag for the specified element.
        /// </summary>
        /// <param name="tagName">The tag name (e.g., "script", "style")</param>
        /// <returns>A regex that matches content until the closing tag</returns>
        public static Regex CreateRawTextContentPattern(string tagName)
        {
            // Match everything until we find the closing tag (non-greedy)
            return new Regex(
                string.Format(@"(?<content>.*?)</\s*{0}\s*>", Regex.Escape(tagName)),
                RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled,
                RegexTimeout);
        }

        /// <summary>
        /// Creates a pattern for matching balanced/nested elements using .NET's balancing groups.
        /// This is the "magic" that makes regex HTML parsing possible in .NET.
        /// </summary>
        /// <param name="tagName">The tag name to match balanced occurrences of</param>
        /// <returns>A regex that matches properly nested instances of the tag</returns>
        public static Regex CreateBalancedTagPattern(string tagName)
        {
            var escapedTag = Regex.Escape(tagName);
            
            // Balancing groups pattern for nested matching:
            // - (?<depth>) pushes to the depth stack when we see an opening tag
            // - (?<-depth>) pops from the depth stack when we see a closing tag
            // - (?(depth)(?!)) is a conditional that fails if the stack is not empty
            var pattern = string.Format(@"
                <{0}\b                          # Opening tag for the target element
                (?<attrs>[^>]*)>                         # Capture attributes
                (?<content>                              # Begin content capture
                    (?>                                  # Atomic group for efficiency
                        [^<]+                            # Non-tag content
                        |
                        <{0}\b[^>]*>            # Nested same-tag open: push
                        (?<depth>)                       # Push to depth stack
                        |
                        </{0}\s*>               # Nested same-tag close: pop  
                        (?<-depth>)                      # Pop from depth stack
                        |
                        <(?!/?{0}\b)[^>]*>      # Other tags (not our target)
                    )*
                )
                (?(depth)(?!))                           # Fail if depth stack not empty
                </{0}\s*>                       # Final closing tag
            ", escapedTag);
            
            return new Regex(
                pattern,
                RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase | RegexOptions.Compiled,
                RegexTimeout);
        }
    }
}
