// Description: Regex Html Parser - A regex-powered HTML parser using .NET balancing groups.

namespace RegexHtmlParser;

/// <summary>
/// Represents an error that occurred during HTML parsing.
/// </summary>
public class HtmlParseError
{
    /// <summary>
    /// Initializes a new instance of the HtmlParseError class.
    /// </summary>
    public HtmlParseError(
        HtmlParseErrorCode code,
        int line,
        int linePosition,
        int streamPosition,
        string reason,
        string sourceText)
    {
        Code = code;
        Line = line;
        LinePosition = linePosition;
        StreamPosition = streamPosition;
        Reason = reason;
        SourceText = sourceText;
    }

    /// <summary>
    /// Gets the error code.
    /// </summary>
    public HtmlParseErrorCode Code { get; }

    /// <summary>
    /// Gets the line number where the error occurred.
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// Gets the column position where the error occurred.
    /// </summary>
    public int LinePosition { get; }

    /// <summary>
    /// Gets the stream position where the error occurred.
    /// </summary>
    public int StreamPosition { get; }

    /// <summary>
    /// Gets the reason for the error.
    /// </summary>
    public string Reason { get; }

    /// <summary>
    /// Gets the source text that caused the error.
    /// </summary>
    public string SourceText { get; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"Error at line {Line}, position {LinePosition}: {Reason}";
    }
}

/// <summary>
/// Specifies the error code for HTML parse errors.
/// </summary>
public enum HtmlParseErrorCode
{
    /// <summary>
    /// An opening tag was not closed.
    /// </summary>
    TagNotClosed,

    /// <summary>
    /// A closing tag was found without a matching opening tag.
    /// </summary>
    TagNotOpened,

    /// <summary>
    /// The document encoding could not be determined.
    /// </summary>
    EncodingError,

    /// <summary>
    /// An attribute has an invalid format.
    /// </summary>
    InvalidAttribute,

    /// <summary>
    /// A character reference is invalid.
    /// </summary>
    CharRefInvalid,

    /// <summary>
    /// An end tag is not required.
    /// </summary>
    EndTagNotRequired,

    /// <summary>
    /// An end tag is invalid.
    /// </summary>
    EndTagInvalid
}
