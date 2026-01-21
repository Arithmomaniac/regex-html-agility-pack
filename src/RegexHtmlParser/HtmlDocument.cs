// Description: Regex Html Parser - A regex-powered HTML parser using .NET balancing groups.
// This is the main document class that uses regex patterns for HTML parsing.

using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using RegexHtmlParser.Patterns;

namespace RegexHtmlParser;

/// <summary>
/// Represents a complete HTML document parsed using regex patterns with .NET balancing groups.
/// </summary>
public class HtmlDocument
{
    #region Fields

    private HtmlNode? _documentNode;
    private readonly Dictionary<string, HtmlNode> _nodesById = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<HtmlParseError> _parseErrors = new();
    private string? _text;
    private Encoding _declaredEncoding = Encoding.UTF8;
    private readonly XmlNameTable _nameTable = new NameTable();

    #endregion

    #region Properties

    /// <summary>
    /// Gets the document's root node.
    /// </summary>
    public HtmlNode DocumentNode
    {
        get
        {
            _documentNode ??= new HtmlNode(HtmlNodeType.Document, this, 0);
            return _documentNode;
        }
    }

    /// <summary>
    /// Gets the original text that was parsed.
    /// </summary>
    public string Text => _text ?? string.Empty;

    /// <summary>
    /// Gets the parsed text.
    /// </summary>
    public string ParsedText => _text ?? string.Empty;

    /// <summary>
    /// Gets the collection of parse errors.
    /// </summary>
    public IReadOnlyList<HtmlParseError> ParseErrors => _parseErrors;

    /// <summary>
    /// Gets the encoding declared in the document.
    /// </summary>
    public Encoding DeclaredEncoding => _declaredEncoding;

    /// <summary>
    /// Gets or sets a value indicating whether to check syntax during parsing.
    /// </summary>
    public bool OptionCheckSyntax { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to auto-close unclosed tags at the end.
    /// </summary>
    public bool OptionAutoCloseOnEnd { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to return empty collections instead of null.
    /// </summary>
    public bool OptionEmptyCollection { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use the original name (preserving case).
    /// </summary>
    public bool OptionDefaultUseOriginalName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to preserve whitespace.
    /// </summary>
    public bool OptionPreserveWhitespace { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to extract error source text.
    /// </summary>
    public bool OptionExtractErrorSourceText { get; set; }

    /// <summary>
    /// Gets or sets the maximum error source text length.
    /// </summary>
    public int OptionExtractErrorSourceTextMaxLength { get; set; } = 100;

    /// <summary>
    /// Gets or sets a value indicating whether to compute a checksum during parsing.
    /// </summary>
    public bool OptionComputeChecksum { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to add debugging attributes.
    /// </summary>
    public bool OptionAddDebuggingAttributes { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to treat CDATA blocks as comments.
    /// </summary>
    public bool OptionTreatCDataBlockAsComment { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to disable server-side code.
    /// </summary>
    public bool DisableServerSideCode { get; set; }

    /// <summary>
    /// Gets or sets the default stream encoding.
    /// </summary>
    public Encoding? OptionDefaultStreamEncoding { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to force original comment in XML output.
    /// </summary>
    public bool OptionXmlForceOriginalComment { get; set; }

    /// <summary>
    /// Gets or sets a value for backward compatibility.
    /// </summary>
    public bool BackwardCompatibility { get; set; } = true;

    /// <summary>
    /// Gets the XmlNameTable for this document.
    /// </summary>
    internal XmlNameTable NameTable => _nameTable;

    /// <summary>
    /// Default builder action.
    /// </summary>
    public static Action<HtmlDocument>? DefaultBuilder { get; set; }

    /// <summary>
    /// Action to execute before parsing.
    /// </summary>
    public Action<HtmlDocument>? ParseExecuting { get; set; }

    /// <summary>
    /// Gets or sets whether to disable the behavior tag P.
    /// </summary>
    public static bool DisableBehaviorTagP { get; set; } = true;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the HtmlDocument class.
    /// </summary>
    public HtmlDocument()
    {
        DefaultBuilder?.Invoke(this);
    }

    #endregion

    #region Load Methods

    /// <summary>
    /// Loads HTML from a string.
    /// </summary>
    public void LoadHtml(string html)
    {
        if (html == null)
            throw new ArgumentNullException(nameof(html));

        _text = html;
        _parseErrors.Clear();
        _nodesById.Clear();
        _documentNode = new HtmlNode(HtmlNodeType.Document, this, 0);

        ParseExecuting?.Invoke(this);
        Parse(html);
    }

    /// <summary>
    /// Loads HTML from a stream.
    /// </summary>
    public void Load(Stream stream)
    {
        Load(stream, Encoding.UTF8, true);
    }

    /// <summary>
    /// Loads HTML from a stream with the specified encoding.
    /// </summary>
    public void Load(Stream stream, Encoding encoding)
    {
        Load(stream, encoding, true);
    }

    /// <summary>
    /// Loads HTML from a stream with the specified encoding and detection options.
    /// </summary>
    public void Load(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks)
    {
        using var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks);
        LoadHtml(reader.ReadToEnd());
    }

    /// <summary>
    /// Loads HTML from a TextReader.
    /// </summary>
    public void Load(TextReader reader)
    {
        LoadHtml(reader.ReadToEnd());
    }

    /// <summary>
    /// Loads HTML from a file.
    /// </summary>
    public void Load(string path)
    {
        LoadHtml(File.ReadAllText(path));
    }

    /// <summary>
    /// Loads HTML from a file with the specified encoding.
    /// </summary>
    public void Load(string path, Encoding encoding)
    {
        LoadHtml(File.ReadAllText(path, encoding));
    }

    #endregion

    #region Parsing Methods

    private void Parse(string html)
    {
        if (string.IsNullOrEmpty(html))
            return;

        var nodeStack = new Stack<HtmlNode>();
        nodeStack.Push(DocumentNode);

        int line = 1;
        int linePosition = 1;
        int position = 0;

        var matches = TokenizerPatterns.TokenizerPattern.Matches(html);

        foreach (Match match in matches)
        {
            // Update position tracking
            var leadingText = html.Substring(position, match.Index - position);
            UpdateLineInfo(leadingText, ref line, ref linePosition);
            position = match.Index;

            if (match.Groups["comment"].Success)
            {
                ParseComment(match, nodeStack.Peek(), line, linePosition);
            }
            else if (match.Groups["doctype"].Success)
            {
                ParseDoctype(match, nodeStack.Peek(), line, linePosition);
            }
            else if (match.Groups["cdata"].Success)
            {
                ParseCData(match, nodeStack.Peek(), line, linePosition);
            }
            else if (match.Groups["selfClosing"].Success)
            {
                ParseSelfClosingTag(match, nodeStack.Peek(), line, linePosition);
            }
            else if (match.Groups["closing"].Success)
            {
                ParseClosingTag(match, nodeStack, line, linePosition);
            }
            else if (match.Groups["opening"].Success)
            {
                ParseOpeningTag(match, html, ref position, nodeStack, line, linePosition);
            }
            else if (match.Groups["text"].Success)
            {
                ParseText(match, nodeStack.Peek(), line, linePosition);
            }

            // Update position for the match itself
            UpdateLineInfo(match.Value, ref line, ref linePosition);
            position = match.Index + match.Length;
        }

        // Handle any remaining text
        if (position < html.Length)
        {
            var remaining = html.Substring(position);
            if (!string.IsNullOrEmpty(remaining) && (OptionPreserveWhitespace || !string.IsNullOrWhiteSpace(remaining)))
            {
                var textNode = new HtmlNode(HtmlNodeType.Text, this, position)
                {
                    _innerHtml = remaining,
                    _line = line,
                    _linePosition = linePosition,
                    _streamPosition = position
                };
                nodeStack.Peek().ChildNodes.Add(textNode);
            }
        }

        // Auto-close any unclosed tags if option is set
        if (OptionAutoCloseOnEnd)
        {
            while (nodeStack.Count > 1)
            {
                var unclosed = nodeStack.Pop();
                unclosed._isImplicitEnd = true;
            }
        }
    }

    private void ParseComment(Match match, HtmlNode parent, int line, int linePosition)
    {
        var commentText = match.Value;
        // Remove <!-- and --> from the comment
        var innerContent = commentText.Length > 7 
            ? commentText.Substring(4, commentText.Length - 7) 
            : string.Empty;

        var node = new HtmlNode(HtmlNodeType.Comment, this, match.Index)
        {
            _innerHtml = innerContent,
            _outerHtml = commentText,
            _line = line,
            _linePosition = linePosition,
            _streamPosition = match.Index
        };

        parent.ChildNodes.Add(node);
    }

    private void ParseDoctype(Match match, HtmlNode parent, int line, int linePosition)
    {
        // Treat DOCTYPE as a special comment
        var node = new HtmlNode(HtmlNodeType.Comment, this, match.Index)
        {
            _innerHtml = match.Value,
            _outerHtml = match.Value,
            _line = line,
            _linePosition = linePosition,
            _streamPosition = match.Index
        };

        parent.ChildNodes.Add(node);
    }

    private void ParseCData(Match match, HtmlNode parent, int line, int linePosition)
    {
        var cdataText = match.Value;
        // Remove <![CDATA[ and ]]> from the content
        var innerContent = cdataText.Length > 12 
            ? cdataText.Substring(9, cdataText.Length - 12) 
            : string.Empty;

        if (OptionTreatCDataBlockAsComment)
        {
            var node = new HtmlNode(HtmlNodeType.Comment, this, match.Index)
            {
                _innerHtml = innerContent,
                _outerHtml = cdataText,
                _line = line,
                _linePosition = linePosition,
                _streamPosition = match.Index
            };
            parent.ChildNodes.Add(node);
        }
        else
        {
            var node = new HtmlNode(HtmlNodeType.Text, this, match.Index)
            {
                _innerHtml = innerContent,
                _line = line,
                _linePosition = linePosition,
                _streamPosition = match.Index
            };
            parent.ChildNodes.Add(node);
        }
    }

    private void ParseSelfClosingTag(Match match, HtmlNode parent, int line, int linePosition)
    {
        var tagMatch = Regex.Match(match.Value, @"<(?<name>[a-zA-Z][a-zA-Z0-9:-]*)\s*(?<attrs>[^>]*?)/>", RegexOptions.Singleline);
        if (!tagMatch.Success)
            return;

        var tagName = tagMatch.Groups["name"].Value;
        var attrsText = tagMatch.Groups["attrs"].Value;

        var node = new HtmlNode(HtmlNodeType.Element, this, match.Index)
        {
            _name = tagName,
            _outerHtml = match.Value,
            _line = line,
            _linePosition = linePosition,
            _streamPosition = match.Index
        };

        ParseAttributes(attrsText, node);
        RegisterNodeId(node);
        parent.ChildNodes.Add(node);
    }

    private void ParseClosingTag(Match match, Stack<HtmlNode> nodeStack, int line, int linePosition)
    {
        var tagMatch = TokenizerPatterns.ClosingTagPattern.Match(match.Value);
        if (!tagMatch.Success)
            return;

        var tagName = tagMatch.Groups["tagName"].Value;

        // Find the matching opening tag in the stack
        var tempStack = new Stack<HtmlNode>();
        HtmlNode? matchingNode = null;

        while (nodeStack.Count > 1)
        {
            var current = nodeStack.Peek();
            if (string.Equals(current.OriginalName, tagName, StringComparison.OrdinalIgnoreCase))
            {
                matchingNode = nodeStack.Pop();
                break;
            }

            // Check if we should implicitly close this tag
            if (ShouldImplicitlyClose(current.Name, tagName))
            {
                current._isImplicitEnd = true;
                tempStack.Push(nodeStack.Pop());
            }
            else
            {
                break;
            }
        }

        // Push back any nodes we temporarily popped (they become siblings)
        while (tempStack.Count > 0)
        {
            var node = tempStack.Pop();
            // Re-parent to current stack top
            if (nodeStack.Count > 0)
            {
                var newParent = nodeStack.Peek();
                newParent.ChildNodes.Add(node);
            }
        }

        if (matchingNode == null && OptionCheckSyntax)
        {
            _parseErrors.Add(new HtmlParseError(
                HtmlParseErrorCode.TagNotClosed,
                line,
                linePosition,
                match.Index,
                $"Closing tag '{tagName}' found without matching opening tag",
                match.Value));
        }
    }

    private void ParseOpeningTag(Match match, string html, ref int position, Stack<HtmlNode> nodeStack, int line, int linePosition)
    {
        var tagMatch = Regex.Match(match.Value, @"<(?<name>[a-zA-Z][a-zA-Z0-9:-]*)\s*(?<attrs>[^>]*)>", RegexOptions.Singleline);
        if (!tagMatch.Success)
            return;

        var tagName = tagMatch.Groups["name"].Value;
        var attrsText = tagMatch.Groups["attrs"].Value;

        // Check for void elements
        bool isVoidElement = TokenizerPatterns.VoidElements.Contains(tagName);

        var node = new HtmlNode(HtmlNodeType.Element, this, match.Index)
        {
            _name = tagName,
            _line = line,
            _linePosition = linePosition,
            _streamPosition = match.Index
        };

        ParseAttributes(attrsText, node);
        RegisterNodeId(node);
        nodeStack.Peek().ChildNodes.Add(node);

        // Handle void elements - they don't go on the stack
        if (isVoidElement)
        {
            node._outerHtml = match.Value;
            return;
        }

        // Handle raw text elements (script, style, etc.)
        if (TokenizerPatterns.RawTextElements.Contains(tagName))
        {
            var endPosition = match.Index + match.Length;
            var rawTextPattern = TokenizerPatterns.CreateRawTextContentPattern(tagName);
            var rawMatch = rawTextPattern.Match(html, endPosition);

            if (rawMatch.Success)
            {
                var rawContent = rawMatch.Groups["content"].Value;
                if (!string.IsNullOrEmpty(rawContent))
                {
                    var textNode = new HtmlNode(HtmlNodeType.Text, this, endPosition)
                    {
                        _innerHtml = rawContent,
                        _line = line,
                        _linePosition = linePosition,
                        _streamPosition = endPosition
                    };
                    node.ChildNodes.Add(textNode);
                }

                // Update position to skip past the raw content and closing tag
                position = rawMatch.Index + rawMatch.Length;
            }
            return;
        }

        // Push onto stack for elements that need closing tags
        nodeStack.Push(node);
    }

    private void ParseText(Match match, HtmlNode parent, int line, int linePosition)
    {
        var text = match.Value;

        // Skip empty text nodes unless preserving whitespace
        if (!OptionPreserveWhitespace && string.IsNullOrWhiteSpace(text))
            return;

        var node = new HtmlNode(HtmlNodeType.Text, this, match.Index)
        {
            _innerHtml = text,
            _line = line,
            _linePosition = linePosition,
            _streamPosition = match.Index
        };

        parent.ChildNodes.Add(node);
    }

    private void ParseAttributes(string attrsText, HtmlNode node)
    {
        if (string.IsNullOrWhiteSpace(attrsText))
            return;

        var matches = TokenizerPatterns.AttributePattern.Matches(attrsText);
        foreach (Match match in matches)
        {
            var name = match.Groups["name"].Value;
            string? value = null;
            var quoteType = AttributeValueQuote.DoubleQuote;

            if (match.Groups["dqValue"].Success)
            {
                value = match.Groups["dqValue"].Value;
                quoteType = AttributeValueQuote.DoubleQuote;
            }
            else if (match.Groups["sqValue"].Success)
            {
                value = match.Groups["sqValue"].Value;
                quoteType = AttributeValueQuote.SingleQuote;
            }
            else if (match.Groups["uqValue"].Success)
            {
                value = match.Groups["uqValue"].Value;
                quoteType = AttributeValueQuote.None;
            }

            var attr = new HtmlAttribute(name, value)
            {
                _ownerDocument = this,
                _ownerNode = node,
                _quoteType = quoteType
            };

            node.Attributes.Add(attr);
        }
    }

    private void RegisterNodeId(HtmlNode node)
    {
        var id = node.GetAttributeValue("id", string.Empty);
        if (!string.IsNullOrEmpty(id))
        {
            _nodesById[id] = node;
        }
    }

    private bool ShouldImplicitlyClose(string currentTag, string closingTag)
    {
        // Define rules for implicit closing
        // For example, <p> is implicitly closed by another block-level element
        var blockElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "address", "article", "aside", "blockquote", "canvas", "dd", "div",
            "dl", "dt", "fieldset", "figcaption", "figure", "footer", "form",
            "h1", "h2", "h3", "h4", "h5", "h6", "header", "hgroup", "hr",
            "li", "main", "nav", "noscript", "ol", "output", "p", "pre",
            "section", "table", "tfoot", "ul", "video"
        };

        if (string.Equals(currentTag, "p", StringComparison.OrdinalIgnoreCase))
        {
            return blockElements.Contains(closingTag);
        }

        if (string.Equals(currentTag, "li", StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(closingTag, "li", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(closingTag, "ul", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(closingTag, "ol", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static void UpdateLineInfo(string text, ref int line, ref int linePosition)
    {
        foreach (var c in text)
        {
            if (c == '\n')
            {
                line++;
                linePosition = 1;
            }
            else
            {
                linePosition++;
            }
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets an element by its id attribute.
    /// </summary>
    public HtmlNode? GetElementById(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        if (_nodesById.TryGetValue(id, out var node))
            return node;

        return null;
    }

    /// <summary>
    /// Gets the element with the specified id (alias for GetElementById).
    /// </summary>
    public HtmlNode? GetElementbyId(string id) => GetElementById(id);

    /// <summary>
    /// Creates a new attribute for this document.
    /// </summary>
    public HtmlAttribute CreateAttribute(string name)
    {
        return new HtmlAttribute(name) { _ownerDocument = this };
    }

    /// <summary>
    /// Creates a new attribute with a value for this document.
    /// </summary>
    public HtmlAttribute CreateAttribute(string name, string value)
    {
        return new HtmlAttribute(name, value) { _ownerDocument = this };
    }

    /// <summary>
    /// Creates a new element node.
    /// </summary>
    public HtmlNode CreateElement(string name)
    {
        return new HtmlNode(HtmlNodeType.Element, this, 0) { _name = name };
    }

    /// <summary>
    /// Creates a new text node.
    /// </summary>
    public HtmlNode CreateTextNode(string text)
    {
        return new HtmlNode(HtmlNodeType.Text, this, 0) { _innerHtml = text };
    }

    /// <summary>
    /// Creates a new comment node.
    /// </summary>
    public HtmlNode CreateComment(string comment)
    {
        return new HtmlNode(HtmlNodeType.Comment, this, 0) { _innerHtml = comment };
    }

    /// <summary>
    /// Saves the document to a string.
    /// </summary>
    public string Save()
    {
        return DocumentNode.OuterHtml;
    }

    /// <summary>
    /// Saves the document to a TextWriter.
    /// </summary>
    public void Save(TextWriter writer)
    {
        writer.Write(Save());
    }

    /// <summary>
    /// Saves the document to a stream.
    /// </summary>
    public void Save(Stream stream)
    {
        Save(stream, Encoding.UTF8);
    }

    /// <summary>
    /// Saves the document to a stream with the specified encoding.
    /// </summary>
    public void Save(Stream stream, Encoding encoding)
    {
        using var writer = new StreamWriter(stream, encoding, leaveOpen: true);
        Save(writer);
    }

    /// <summary>
    /// Saves the document to a file.
    /// </summary>
    public void Save(string filename)
    {
        File.WriteAllText(filename, Save());
    }

    /// <summary>
    /// Saves the document to a file with the specified encoding.
    /// </summary>
    public void Save(string filename, Encoding encoding)
    {
        File.WriteAllText(filename, Save(), encoding);
    }

    /// <summary>
    /// Saves the document to an XmlWriter.
    /// </summary>
    public void Save(XmlWriter writer)
    {
        DocumentNode.WriteTo(writer);
    }

    #endregion
}
