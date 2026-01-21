// Description: Regex Html Parser - A regex-powered HTML parser using .NET balancing groups.

namespace RegexHtmlParser;

/// <summary>
/// Represents the type of an HTML node.
/// </summary>
public enum HtmlNodeType
{
    /// <summary>
    /// The root of a document.
    /// </summary>
    Document,

    /// <summary>
    /// An HTML element.
    /// </summary>
    Element,

    /// <summary>
    /// An HTML comment.
    /// </summary>
    Comment,

    /// <summary>
    /// A text node is always the child of an element or a document node.
    /// </summary>
    Text
}
