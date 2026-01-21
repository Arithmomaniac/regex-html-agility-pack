// Description: Regex Html Parser - A regex-powered HTML parser using .NET balancing groups.

using System.Diagnostics;

namespace RegexHtmlParser;

/// <summary>
/// Specifies the type of quote used for attribute values.
/// </summary>
public enum AttributeValueQuote
{
    /// <summary>
    /// Double quotes: name="value"
    /// </summary>
    DoubleQuote,
    
    /// <summary>
    /// Single quotes: name='value'
    /// </summary>
    SingleQuote,
    
    /// <summary>
    /// No quotes (for simple values without spaces)
    /// </summary>
    None
}

/// <summary>
/// Represents an HTML attribute.
/// </summary>
[DebuggerDisplay("Name: {OriginalName}, Value: {Value}")]
public class HtmlAttribute : IComparable
{
    internal string _name;
    internal string? _value;
    internal HtmlDocument? _ownerDocument;
    internal HtmlNode? _ownerNode;
    internal int _line;
    internal int _linePosition;
    internal int _streamPosition;
    internal int _nameStartIndex;
    internal int _nameLength;
    internal int _valueStartIndex;
    internal int _valueLength;
    internal AttributeValueQuote _quoteType = AttributeValueQuote.DoubleQuote;

    /// <summary>
    /// Initializes a new instance of the HtmlAttribute class.
    /// </summary>
    /// <param name="name">The attribute name.</param>
    /// <param name="value">The attribute value.</param>
    public HtmlAttribute(string name, string? value = null)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _value = value;
    }

    /// <summary>
    /// Initializes a new instance of the HtmlAttribute class with an owner document.
    /// </summary>
    /// <param name="ownerDocument">The owner document.</param>
    internal HtmlAttribute(HtmlDocument ownerDocument)
    {
        _ownerDocument = ownerDocument;
        _name = string.Empty;
    }

    /// <summary>
    /// Gets the name of the attribute (lowercase).
    /// </summary>
    public string Name
    {
        get => _name.ToLowerInvariant();
        set => _name = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets the original name of the attribute (preserves casing).
    /// </summary>
    public string OriginalName => _name;

    /// <summary>
    /// Gets or sets the value of the attribute.
    /// </summary>
    public string Value
    {
        get => _value ?? string.Empty;
        set => _value = value;
    }

    /// <summary>
    /// Gets the line number where this attribute appears in the source.
    /// </summary>
    public int Line => _line;

    /// <summary>
    /// Gets the column number where this attribute appears in the source.
    /// </summary>
    public int LinePosition => _linePosition;

    /// <summary>
    /// Gets the stream position of this attribute in the source.
    /// </summary>
    public int StreamPosition => _streamPosition;

    /// <summary>
    /// Gets the start index of the attribute value.
    /// </summary>
    public int ValueStartIndex => _valueStartIndex;

    /// <summary>
    /// Gets the length of the attribute value.
    /// </summary>
    public int ValueLength => _valueLength;

    /// <summary>
    /// Gets or sets the quote type used for this attribute's value.
    /// </summary>
    public AttributeValueQuote QuoteType
    {
        get => _quoteType;
        set => _quoteType = value;
    }

    /// <summary>
    /// Gets the owner document.
    /// </summary>
    public HtmlDocument? OwnerDocument => _ownerDocument;

    /// <summary>
    /// Gets the owner node.
    /// </summary>
    public HtmlNode? OwnerNode => _ownerNode;

    /// <summary>
    /// Creates a deep copy of this attribute.
    /// </summary>
    /// <returns>A new HtmlAttribute with the same values.</returns>
    public HtmlAttribute Clone()
    {
        return new HtmlAttribute(_name, _value)
        {
            _quoteType = _quoteType,
            _line = _line,
            _linePosition = _linePosition,
            _streamPosition = _streamPosition
        };
    }

    /// <summary>
    /// Removes this attribute from its owner node.
    /// </summary>
    public void Remove()
    {
        _ownerNode?.Attributes.Remove(this);
    }

    /// <summary>
    /// Compares this attribute to another based on name.
    /// </summary>
    public int CompareTo(object? obj)
    {
        if (obj is HtmlAttribute other)
        {
            return string.Compare(Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }
        return 0;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        var quote = _quoteType switch
        {
            AttributeValueQuote.SingleQuote => "'",
            AttributeValueQuote.None => "",
            _ => "\""
        };

        if (string.IsNullOrEmpty(_value))
        {
            return _name;
        }

        return _quoteType == AttributeValueQuote.None
            ? $"{_name}={_value}"
            : $"{_name}={quote}{_value}{quote}";
    }
}
