namespace HtmlAgilityPack.RegexParser
{
    /// <summary>
    /// Types of tokens that can be extracted from HTML.
    /// </summary>
    public enum TokenType
    {
        /// <summary>Opening tag like &lt;div&gt; or &lt;div class="foo"&gt;</summary>
        OpenTag,
        
        /// <summary>Closing tag like &lt;/div&gt;</summary>
        CloseTag,
        
        /// <summary>Self-closing tag like &lt;br/&gt; or &lt;img src="x"/&gt;</summary>
        SelfCloseTag,
        
        /// <summary>Text content between tags</summary>
        Text,
        
        /// <summary>HTML comment &lt;!-- ... --&gt;</summary>
        Comment,
        
        /// <summary>DOCTYPE declaration &lt;!DOCTYPE ...&gt;</summary>
        DocType,
        
        /// <summary>CDATA section &lt;![CDATA[ ... ]]&gt;</summary>
        CData,
        
        /// <summary>Server-side code block &lt;% ... %&gt; or &lt;%= ... %&gt;</summary>
        ServerSideCode
    }

    /// <summary>
    /// Represents a single token extracted from HTML source.
    /// </summary>
    public class Token
    {
        /// <summary>The type of this token.</summary>
        public TokenType Type { get; set; }
        
        /// <summary>
        /// Tag name for OpenTag, CloseTag, SelfCloseTag (lowercase).
        /// Null for other token types.
        /// </summary>
        public string? Name { get; set; }
        
        /// <summary>
        /// Original tag name preserving case.
        /// </summary>
        public string? OriginalName { get; set; }
        
        /// <summary>
        /// Raw attribute string for tags (unparsed).
        /// e.g., for &lt;div class="foo" id="bar"&gt; this would be: class="foo" id="bar"
        /// </summary>
        public string? RawAttributes { get; set; }
        
        /// <summary>
        /// Parsed attributes (populated by second pass).
        /// </summary>
        public List<TokenAttribute>? Attributes { get; set; }
        
        /// <summary>
        /// Content for Text, Comment, CData, ServerSideCode tokens.
        /// For tags, this is null (content is in child tokens).
        /// </summary>
        public string? Content { get; set; }
        
        /// <summary>
        /// The full matched text from the source HTML.
        /// </summary>
        public string? RawText { get; set; }
        
        /// <summary>Position in the source string (0-based character index).</summary>
        public int Position { get; set; }
        
        /// <summary>Line number in source (1-based).</summary>
        public int Line { get; set; }
        
        /// <summary>Column position in line (1-based).</summary>
        public int LinePosition { get; set; }
        
        /// <summary>Length of the raw text.</summary>
        public int Length { get; set; }

        public override string ToString()
        {
            return Type switch
            {
                TokenType.OpenTag => $"<{Name}{(string.IsNullOrEmpty(RawAttributes) ? "" : " " + RawAttributes)}>",
                TokenType.CloseTag => $"</{Name}>",
                TokenType.SelfCloseTag => $"<{Name}{(string.IsNullOrEmpty(RawAttributes) ? "" : " " + RawAttributes)}/>",
                TokenType.Text => $"TEXT[{Truncate(Content, 20)}]",
                TokenType.Comment => $"COMMENT[{Truncate(Content, 20)}]",
                TokenType.DocType => $"DOCTYPE[{Truncate(Content, 20)}]",
                TokenType.CData => $"CDATA[{Truncate(Content, 20)}]",
                TokenType.ServerSideCode => $"SERVER[{Truncate(Content, 20)}]",
                _ => $"UNKNOWN"
            };
        }

        private static string? Truncate(string? s, int maxLength)
        {
            if (s == null) return null;
            if (s.Length <= maxLength) return s;
            return s.Substring(0, maxLength) + "...";
        }
    }

    /// <summary>
    /// Represents a single HTML attribute.
    /// </summary>
    public class TokenAttribute
    {
        /// <summary>Attribute name (lowercase).</summary>
        public string Name { get; set; } = "";
        
        /// <summary>Original attribute name preserving case.</summary>
        public string OriginalName { get; set; } = "";
        
        /// <summary>Attribute value (may be null for boolean attributes).</summary>
        public string? Value { get; set; }
        
        /// <summary>Quote character used: '"', '\'', or '\0' for unquoted/none.</summary>
        public char QuoteChar { get; set; }
        
        /// <summary>Position of attribute name in source.</summary>
        public int Position { get; set; }
        
        /// <summary>Position of attribute value in source (or -1 if no value).</summary>
        public int ValuePosition { get; set; } = -1;

        public override string ToString()
        {
            if (Value == null) return OriginalName;
            var quote = QuoteChar == '\0' ? "" : QuoteChar.ToString();
            return $"{OriginalName}={quote}{Value}{quote}";
        }
    }
}
