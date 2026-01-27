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
        /// Uses a single regex with capturing groups to extract content and closing tag,
        /// avoiding substring operations.
        /// </summary>
        private (Token? ContentToken, Token? CloseToken, int EndPosition) ExtractRawTextContent(
            string html, int startPos, string tagName, LineTracker lineInfo)
        {
            // Use a single regex with capturing groups to match content + closing tag
            // Group 1 (content): everything before the closing tag
            // Group 2 (closetag): the closing tag itself
            var pattern = new Regex(
                $@"(?<content>.*?)(?<closetag></{Regex.Escape(tagName)}\s*>)|(?<rest>.+)$",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            var match = pattern.Match(html, startPos);

            if (!match.Success)
            {
                // Empty remaining content - shouldn't typically happen
                return (null, null, html.Length);
            }

            // Check if we matched the closing tag pattern or the "rest of document" pattern
            var closetag = match.Groups["closetag"];
            if (closetag.Success)
            {
                // Found closing tag - extract content and closing tag from capturing groups
                var contentGroup = match.Groups["content"];
                Token? contentToken = null;
                
                if (contentGroup.Success && contentGroup.Length > 0)
                {
                    var (line, col) = lineInfo.GetLineAndColumn(startPos);
                    contentToken = new Token
                    {
                        Type = TokenType.Text,
                        Content = contentGroup.Value,
                        RawText = contentGroup.Value,
                        Position = startPos,
                        Line = line,
                        LinePosition = col,
                        Length = contentGroup.Length
                    };
                }

                var (closeLine, closeCol) = lineInfo.GetLineAndColumn(closetag.Index);
                var closeToken = new Token
                {
                    Type = TokenType.CloseTag,
                    Name = tagName.ToLowerInvariant(),
                    OriginalName = tagName, // We don't know original case from the match
                    RawText = closetag.Value,
                    Position = closetag.Index,
                    Line = closeLine,
                    LinePosition = closeCol,
                    Length = closetag.Length
                };

                return (contentToken, closeToken, closetag.Index + closetag.Length);
            }
            else
            {
                // No closing tag - treat rest of document as raw content via the "rest" group
                var restGroup = match.Groups["rest"];
                var (line, col) = lineInfo.GetLineAndColumn(startPos);
                var contentToken = new Token
                {
                    Type = TokenType.Text,
                    Content = restGroup.Value,
                    RawText = restGroup.Value,
                    Position = startPos,
                    Line = line,
                    LinePosition = col,
                    Length = restGroup.Length
                };
                return (contentToken, null, html.Length);
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
                // Extract content directly from nested capturing group - no re-parsing needed!
                var contentGroup = match.Groups["commentcontent"];
                token.Content = contentGroup.Success ? contentGroup.Value : match.Value;
            }
            else if (match.Groups["cdata"].Success)
            {
                token.Type = TokenType.CData;
                // Extract content directly from nested capturing group - no re-parsing needed!
                var contentGroup = match.Groups["cdatacontent"];
                token.Content = contentGroup.Success ? contentGroup.Value : match.Value;
            }
            else if (match.Groups["servercode"].Success)
            {
                token.Type = TokenType.ServerSideCode;
                // Extract content directly from nested capturing group - no re-parsing needed!
                var contentGroup = match.Groups["servercodecontent"];
                token.Content = contentGroup.Success ? contentGroup.Value : match.Value;
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
    /// NOW USES REGEX for line detection!
    /// </summary>
    internal class LineTracker
    {
        private readonly string _source;
        private readonly List<int> _lineStarts;

        public LineTracker(string source)
        {
            _source = source;
            _lineStarts = new List<int> { 0 };
            
            // Use regex to find all newline positions
            foreach (var match in HtmlPatterns.NewlinePattern().EnumerateMatches(source))
            {
                _lineStarts.Add(match.Index + 1);
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
