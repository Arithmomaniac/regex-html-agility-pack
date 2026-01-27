using System.Text.RegularExpressions;

namespace HtmlAgilityPack.RegexParser
{
    /// <summary>
    /// Tokenizes HTML into a flat list of tokens using regex.
    /// This is PASS 2 of the multi-pass parsing strategy.
    /// 
    /// The tokenizer does NOT build a tree - it just breaks HTML into pieces.
    /// Tree building happens in RegexTreeBuilder.
    /// </summary>
    public class RegexTokenizer
    {
        /// <summary>
        /// Tokenizes HTML source into a list of tokens.
        /// Handles raw text elements (script, style) specially.
        /// </summary>
        /// <param name="html">The HTML source string.</param>
        /// <returns>List of tokens in document order.</returns>
        public List<Token> Tokenize(string html)
        {
            if (string.IsNullOrEmpty(html))
                return new List<Token>();

            var tokens = new List<Token>();
            var lineInfo = new LineTracker(html);
            int currentPos = 0;

            while (currentPos < html.Length)
            {
                // Try to match at current position using source-generated regex
                var match = HtmlPatterns.MasterTokenizer().Match(html, currentPos);
                
                if (!match.Success || match.Index > currentPos)
                {
                    // No match or gap - shouldn't happen with our pattern, but handle it
                    break;
                }

                var token = CreateTokenFromMatch(match, lineInfo);
                if (token != null)
                {
                    tokens.Add(token);
                    
                    // If this is an opening tag for a raw text element, handle specially
                    if (token.Type == TokenType.OpenTag && HtmlPatterns.IsRawTextElement(token.Name!))
                    {
                        currentPos = match.Index + match.Length;
                        var rawResult = ExtractRawTextContent(html, currentPos, token.Name!, lineInfo);
                        if (rawResult.ContentToken != null)
                        {
                            tokens.Add(rawResult.ContentToken);
                        }
                        if (rawResult.CloseToken != null)
                        {
                            tokens.Add(rawResult.CloseToken);
                        }
                        currentPos = rawResult.EndPosition;
                        continue;
                    }
                }

                currentPos = match.Index + match.Length;
            }

            return tokens;
        }

        /// <summary>
        /// Extracts raw text content and closing tag for script/style/etc.
        /// </summary>
        private (Token? ContentToken, Token? CloseToken, int EndPosition) ExtractRawTextContent(
            string html, int startPos, string tagName, LineTracker lineInfo)
        {
            // Find the closing tag (case-insensitive)
            var closePattern = new Regex($@"</{Regex.Escape(tagName)}\s*>", RegexOptions.IgnoreCase);
            var closeMatch = closePattern.Match(html, startPos);

            if (!closeMatch.Success)
            {
                // No closing tag - treat rest of document as raw content
                var content = html.Substring(startPos);
                var (line, col) = lineInfo.GetLineAndColumn(startPos);
                var contentToken = new Token
                {
                    Type = TokenType.Text,
                    Content = content,
                    RawText = content,
                    Position = startPos,
                    Line = line,
                    LinePosition = col,
                    Length = content.Length
                };
                return (contentToken, null, html.Length);
            }
            else
            {
                Token? contentToken = null;
                if (closeMatch.Index > startPos)
                {
                    var content = html.Substring(startPos, closeMatch.Index - startPos);
                    var (line, col) = lineInfo.GetLineAndColumn(startPos);
                    contentToken = new Token
                    {
                        Type = TokenType.Text,
                        Content = content,
                        RawText = content,
                        Position = startPos,
                        Line = line,
                        LinePosition = col,
                        Length = content.Length
                    };
                }

                var (closeLine, closeCol) = lineInfo.GetLineAndColumn(closeMatch.Index);
                var closeToken = new Token
                {
                    Type = TokenType.CloseTag,
                    Name = tagName.ToLowerInvariant(),
                    OriginalName = tagName, // We don't know original case
                    RawText = closeMatch.Value,
                    Position = closeMatch.Index,
                    Line = closeLine,
                    LinePosition = closeCol,
                    Length = closeMatch.Length
                };

                return (contentToken, closeToken, closeMatch.Index + closeMatch.Length);
            }
        }

        /// <summary>
        /// Tokenizes and also parses attributes on each tag token.
        /// </summary>
        public List<Token> TokenizeWithAttributes(string html)
        {
            var tokens = Tokenize(html);
            
            foreach (var token in tokens)
            {
                if ((token.Type == TokenType.OpenTag || token.Type == TokenType.SelfCloseTag)
                    && !string.IsNullOrWhiteSpace(token.RawAttributes))
                {
                    token.Attributes = ParseAttributes(token.RawAttributes, token.Position);
                }
            }

            return tokens;
        }

        /// <summary>
        /// Parses attribute string into list of TokenAttribute.
        /// </summary>
        public List<TokenAttribute> ParseAttributes(string attributeString, int basePosition)
        {
            var attributes = new List<TokenAttribute>();
            
            if (string.IsNullOrWhiteSpace(attributeString))
                return attributes;

            var matches = HtmlPatterns.AttributeParser().Matches(attributeString);

            foreach (Match match in matches)
            {
                var nameGroup = match.Groups["name"];
                if (!nameGroup.Success || string.IsNullOrWhiteSpace(nameGroup.Value))
                    continue;

                var attr = new TokenAttribute
                {
                    OriginalName = nameGroup.Value,
                    Name = nameGroup.Value.ToLowerInvariant(),
                    Position = basePosition + nameGroup.Index
                };

                // Check which value group matched
                var dqGroup = match.Groups["dqval"];
                var sqGroup = match.Groups["sqval"];
                var uqGroup = match.Groups["uqval"];

                if (dqGroup.Success)
                {
                    attr.Value = dqGroup.Value;
                    attr.QuoteChar = '"';
                    attr.ValuePosition = basePosition + dqGroup.Index;
                }
                else if (sqGroup.Success)
                {
                    attr.Value = sqGroup.Value;
                    attr.QuoteChar = '\'';
                    attr.ValuePosition = basePosition + sqGroup.Index;
                }
                else if (uqGroup.Success)
                {
                    attr.Value = uqGroup.Value;
                    attr.QuoteChar = '\0';
                    attr.ValuePosition = basePosition + uqGroup.Index;
                }
                else
                {
                    // Boolean attribute (no value)
                    attr.Value = null;
                    attr.QuoteChar = '\0';
                    attr.ValuePosition = -1;
                }

                attributes.Add(attr);
            }

            return attributes;
        }

        private Token? CreateTokenFromMatch(Match match, LineTracker lineInfo)
        {
            var token = new Token
            {
                Position = match.Index,
                Length = match.Length,
                RawText = match.Value
            };

            // Calculate line info
            var (line, col) = lineInfo.GetLineAndColumn(match.Index);
            token.Line = line;
            token.LinePosition = col;

            // Determine token type based on which group matched
            if (match.Groups["doctype"].Success)
            {
                token.Type = TokenType.DocType;
                token.Content = match.Groups["doctype"].Value;
            }
            else if (match.Groups["comment"].Success)
            {
                token.Type = TokenType.Comment;
                // Extract just the comment content (without <!-- -->) using source-gen regex
                var commentMatch = HtmlPatterns.Comment().Match(match.Value);
                token.Content = commentMatch.Success 
                    ? commentMatch.Groups["content"].Value 
                    : match.Value;
            }
            else if (match.Groups["cdata"].Success)
            {
                token.Type = TokenType.CData;
                var cdataMatch = HtmlPatterns.CData().Match(match.Value);
                token.Content = cdataMatch.Success
                    ? cdataMatch.Groups["content"].Value
                    : match.Value;
            }
            else if (match.Groups["servercode"].Success)
            {
                token.Type = TokenType.ServerSideCode;
                var serverMatch = HtmlPatterns.ServerSideCode().Match(match.Value);
                token.Content = serverMatch.Success
                    ? serverMatch.Groups["content"].Value
                    : match.Value;
            }
            else if (match.Groups["selfclose"].Success)
            {
                token.Type = TokenType.SelfCloseTag;
                token.OriginalName = match.Groups["scname"].Value;
                token.Name = token.OriginalName.ToLowerInvariant();
                token.RawAttributes = match.Groups["scattrs"].Value.Trim();
            }
            else if (match.Groups["opentag"].Success)
            {
                var tagName = match.Groups["otname"].Value;
                
                // Check if this is a void element (treat as self-closing)
                if (HtmlPatterns.IsVoidElement(tagName))
                {
                    token.Type = TokenType.SelfCloseTag;
                }
                else
                {
                    token.Type = TokenType.OpenTag;
                }
                
                token.OriginalName = tagName;
                token.Name = tagName.ToLowerInvariant();
                token.RawAttributes = match.Groups["otattrs"].Value.Trim();
            }
            else if (match.Groups["closetag"].Success)
            {
                token.Type = TokenType.CloseTag;
                token.OriginalName = match.Groups["ctname"].Value;
                token.Name = token.OriginalName.ToLowerInvariant();
            }
            else if (match.Groups["text"].Success)
            {
                token.Type = TokenType.Text;
                token.Content = match.Groups["text"].Value;
            }
            else
            {
                // Unknown match - shouldn't happen
                return null;
            }

            return token;
        }
    }

    /// <summary>
    /// Tracks line numbers and column positions for a source string.
    /// </summary>
    internal class LineTracker
    {
        private readonly List<int> _lineStarts;

        public LineTracker(string source)
        {
            _lineStarts = new List<int> { 0 };
            
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] == '\n')
                {
                    _lineStarts.Add(i + 1);
                }
            }
        }

        /// <summary>
        /// Gets the 1-based line number and column for a character position.
        /// </summary>
        public (int Line, int Column) GetLineAndColumn(int position)
        {
            // Binary search for the line
            int line = BinarySearchLine(position);
            int column = position - _lineStarts[line] + 1;
            return (line + 1, column);  // Convert to 1-based
        }

        private int BinarySearchLine(int position)
        {
            int low = 0;
            int high = _lineStarts.Count - 1;

            while (low < high)
            {
                int mid = (low + high + 1) / 2;
                if (_lineStarts[mid] <= position)
                    low = mid;
                else
                    high = mid - 1;
            }

            return low;
        }
    }
}
