// Description: Regex Html Parser - A regex-powered HTML parser using .NET balancing groups.

using System.Diagnostics;
using System.Text;
using System.Xml.XPath;
using RegexHtmlParser.Patterns;

namespace RegexHtmlParser;

/// <summary>
/// Represents an HTML node in the document tree.
/// </summary>
[DebuggerDisplay("Name: {OriginalName}")]
public class HtmlNode
{
    #region Static Fields

    /// <summary>
    /// Gets the name of a comment node. It is actually defined as '#comment'.
    /// </summary>
    public static readonly string HtmlNodeTypeNameComment = "#comment";

    /// <summary>
    /// Gets the name of the document node. It is actually defined as '#document'.
    /// </summary>
    public static readonly string HtmlNodeTypeNameDocument = "#document";

    /// <summary>
    /// Gets the name of a text node. It is actually defined as '#text'.
    /// </summary>
    public static readonly string HtmlNodeTypeNameText = "#text";

    /// <summary>
    /// Gets a collection of flags that define specific behaviors for specific element nodes.
    /// </summary>
    public static Dictionary<string, HtmlElementFlag> ElementsFlags { get; } = new(StringComparer.OrdinalIgnoreCase)
    {
        // Tags whose content may be anything
        ["script"] = HtmlElementFlag.CData,
        ["style"] = HtmlElementFlag.CData,
        ["noxhtml"] = HtmlElementFlag.CData,
        ["textarea"] = HtmlElementFlag.CData,
        ["title"] = HtmlElementFlag.CData,
        
        // Tags that can not contain other tags (void elements)
        ["base"] = HtmlElementFlag.Empty,
        ["link"] = HtmlElementFlag.Empty,
        ["meta"] = HtmlElementFlag.Empty,
        ["isindex"] = HtmlElementFlag.Empty,
        ["hr"] = HtmlElementFlag.Empty,
        ["col"] = HtmlElementFlag.Empty,
        ["img"] = HtmlElementFlag.Empty,
        ["param"] = HtmlElementFlag.Empty,
        ["embed"] = HtmlElementFlag.Empty,
        ["frame"] = HtmlElementFlag.Empty,
        ["wbr"] = HtmlElementFlag.Empty,
        ["bgsound"] = HtmlElementFlag.Empty,
        ["spacer"] = HtmlElementFlag.Empty,
        ["keygen"] = HtmlElementFlag.Empty,
        ["area"] = HtmlElementFlag.Empty,
        ["input"] = HtmlElementFlag.Empty,
        ["basefont"] = HtmlElementFlag.Empty,
        ["source"] = HtmlElementFlag.Empty,
        ["form"] = HtmlElementFlag.CanOverlap,
        ["br"] = HtmlElementFlag.Empty | HtmlElementFlag.Closed,
    };

    #endregion

    #region Fields

    internal HtmlAttributeCollection? _attributes;
    internal HtmlNodeCollection? _childNodes;
    internal HtmlNode? _endNode;
    internal string? _innerHtml;
    internal int _innerLength;
    internal int _innerStartIndex;
    internal int _line;
    internal int _linePosition;
    internal string _name;
    internal int _nameLength;
    internal int _nameStartIndex;
    internal HtmlNode? _nextNode;
    internal HtmlNodeType _nodeType;
    internal string? _outerHtml;
    internal int _outerLength;
    internal int _outerStartIndex;
    internal HtmlDocument? _ownerDocument;
    internal HtmlNode? _parentNode;
    internal HtmlNode? _prevNode;
    internal int _streamPosition;
    internal bool _isImplicitEnd;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the HtmlNode class with the specified type, owner document and index.
    /// </summary>
    public HtmlNode(HtmlNodeType type, HtmlDocument ownerDocument, int index)
    {
        _nodeType = type;
        _ownerDocument = ownerDocument;
        _outerStartIndex = index;
        
        _name = type switch
        {
            HtmlNodeType.Document => HtmlNodeTypeNameDocument,
            HtmlNodeType.Comment => HtmlNodeTypeNameComment,
            HtmlNodeType.Text => HtmlNodeTypeNameText,
            _ => string.Empty
        };
    }

    /// <summary>
    /// Internal constructor for creating nodes without an owner document.
    /// </summary>
    internal HtmlNode(HtmlNodeType type)
    {
        _nodeType = type;
        _name = type switch
        {
            HtmlNodeType.Document => HtmlNodeTypeNameDocument,
            HtmlNodeType.Comment => HtmlNodeTypeNameComment,
            HtmlNodeType.Text => HtmlNodeTypeNameText,
            _ => string.Empty
        };
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the name of the node (lowercase).
    /// </summary>
    public string Name
    {
        get => _name.ToLowerInvariant();
        set => _name = value ?? string.Empty;
    }

    /// <summary>
    /// Gets the original name of the node (preserves casing).
    /// </summary>
    public string OriginalName => _name;

    /// <summary>
    /// Gets the type of this node.
    /// </summary>
    public HtmlNodeType NodeType => _nodeType;

    /// <summary>
    /// Gets the HTML between the start and end tags of this node.
    /// </summary>
    public virtual string InnerHtml
    {
        get
        {
            if (_innerHtml != null)
                return _innerHtml;

            if (_childNodes == null || _childNodes.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            foreach (var child in _childNodes)
            {
                sb.Append(child.OuterHtml);
            }
            return sb.ToString();
        }
        set
        {
            _innerHtml = value;
            if (_ownerDocument != null && !string.IsNullOrEmpty(value))
            {
                // Re-parse the inner HTML
                var tempDoc = new HtmlDocument();
                tempDoc.LoadHtml(value);
                
                _childNodes?.Clear();
                EnsureChildNodes();
                
                foreach (var child in tempDoc.DocumentNode.ChildNodes)
                {
                    var clonedChild = child.CloneNode(true);
                    clonedChild._ownerDocument = _ownerDocument;
                    clonedChild._parentNode = this;
                    _childNodes!.Add(clonedChild);
                }
            }
        }
    }

    /// <summary>
    /// Gets the complete HTML markup of this node including its start tag, content, and end tag.
    /// </summary>
    public virtual string OuterHtml
    {
        get
        {
            if (_outerHtml != null)
                return _outerHtml;

            if (_nodeType == HtmlNodeType.Text)
                return _innerHtml ?? string.Empty;

            if (_nodeType == HtmlNodeType.Comment)
                return $"<!--{_innerHtml}-->";

            if (_nodeType == HtmlNodeType.Document)
                return InnerHtml;

            var sb = new StringBuilder();
            
            // Opening tag
            sb.Append('<').Append(OriginalName);
            
            if (_attributes != null && _attributes.Count > 0)
            {
                foreach (var attr in _attributes)
                {
                    sb.Append(' ').Append(attr.ToString());
                }
            }

            // Check if this is a void element
            if (IsVoidElement)
            {
                sb.Append(" />");
                return sb.ToString();
            }

            sb.Append('>');
            
            // Inner content
            sb.Append(InnerHtml);
            
            // Closing tag
            sb.Append("</").Append(OriginalName).Append('>');
            
            return sb.ToString();
        }
    }

    /// <summary>
    /// Gets the text content of this node and all descendants.
    /// </summary>
    public virtual string InnerText
    {
        get
        {
            if (_nodeType == HtmlNodeType.Text)
                return HtmlEntity.DeEntitize(_innerHtml ?? string.Empty);

            if (_nodeType == HtmlNodeType.Comment)
                return string.Empty;

            if (_childNodes == null || _childNodes.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            foreach (var child in _childNodes)
            {
                sb.Append(child.InnerText);
            }
            return sb.ToString();
        }
        set
        {
            // Remove all children and add a text node with the value
            _childNodes?.Clear();
            EnsureChildNodes();
            
            if (!string.IsNullOrEmpty(value))
            {
                var textNode = new HtmlNode(HtmlNodeType.Text)
                {
                    _innerHtml = HtmlEntity.Entitize(value),
                    _ownerDocument = _ownerDocument,
                    _parentNode = this
                };
                _childNodes!.Add(textNode);
            }
        }
    }

    /// <summary>
    /// Gets the direct text content (without child elements' text).
    /// </summary>
    public string DirectInnerText
    {
        get
        {
            if (_nodeType == HtmlNodeType.Text)
                return HtmlEntity.DeEntitize(_innerHtml ?? string.Empty);

            if (_childNodes == null || _childNodes.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            foreach (var child in _childNodes)
            {
                if (child.NodeType == HtmlNodeType.Text)
                {
                    sb.Append(child.InnerText);
                }
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// Gets the collection of child nodes.
    /// </summary>
    public HtmlNodeCollection ChildNodes
    {
        get
        {
            EnsureChildNodes();
            return _childNodes!;
        }
    }

    /// <summary>
    /// Gets the collection of attributes.
    /// </summary>
    public HtmlAttributeCollection Attributes
    {
        get
        {
            _attributes ??= new HtmlAttributeCollection(this);
            return _attributes;
        }
    }

    /// <summary>
    /// Gets the parent node.
    /// </summary>
    public HtmlNode? ParentNode => _parentNode;

    /// <summary>
    /// Gets the next sibling node.
    /// </summary>
    public HtmlNode? NextSibling => _nextNode;

    /// <summary>
    /// Gets the previous sibling node.
    /// </summary>
    public HtmlNode? PreviousSibling => _prevNode;

    /// <summary>
    /// Gets the first child node.
    /// </summary>
    public HtmlNode? FirstChild => _childNodes?.First;

    /// <summary>
    /// Gets the last child node.
    /// </summary>
    public HtmlNode? LastChild => _childNodes?.Last;

    /// <summary>
    /// Gets the owner document.
    /// </summary>
    public HtmlDocument? OwnerDocument => _ownerDocument;

    /// <summary>
    /// Gets whether this node has child nodes.
    /// </summary>
    public bool HasChildNodes => _childNodes != null && _childNodes.Count > 0;

    /// <summary>
    /// Gets whether this node has attributes.
    /// </summary>
    public bool HasAttributes => _attributes != null && _attributes.Count > 0;

    /// <summary>
    /// Gets the line number where this node appears in the source.
    /// </summary>
    public int Line => _line;

    /// <summary>
    /// Gets the column number where this node appears in the source.
    /// </summary>
    public int LinePosition => _linePosition;

    /// <summary>
    /// Gets the stream position of this node in the source.
    /// </summary>
    public int StreamPosition => _streamPosition;

    /// <summary>
    /// Gets whether this element is a void element (self-closing by definition).
    /// </summary>
    public bool IsVoidElement
    {
        get
        {
            if (_nodeType != HtmlNodeType.Element)
                return false;
            return TokenizerPatterns.VoidElements.Contains(_name);
        }
    }

    /// <summary>
    /// Gets a value indicating whether the node is closed.
    /// </summary>
    public bool Closed => _endNode != null || IsVoidElement;

    /// <summary>
    /// Gets or sets the id attribute value.
    /// </summary>
    public string Id
    {
        get => GetAttributeValue("id", string.Empty);
        set => SetAttributeValue("id", value);
    }

    #endregion

    #region Methods

    /// <summary>
    /// Gets the value of an attribute.
    /// </summary>
    public string GetAttributeValue(string name, string defaultValue)
    {
        var attr = Attributes[name];
        return attr?.Value ?? defaultValue;
    }

    /// <summary>
    /// Gets the value of an attribute as an integer.
    /// </summary>
    public int GetAttributeValue(string name, int defaultValue)
    {
        var attr = Attributes[name];
        if (attr == null)
            return defaultValue;
        return int.TryParse(attr.Value, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// Gets the value of an attribute as a boolean.
    /// </summary>
    public bool GetAttributeValue(string name, bool defaultValue)
    {
        var attr = Attributes[name];
        if (attr == null)
            return defaultValue;
        return bool.TryParse(attr.Value, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// Sets the value of an attribute.
    /// </summary>
    public HtmlAttribute SetAttributeValue(string name, string value)
    {
        var attr = Attributes[name];
        if (attr == null)
        {
            return Attributes.Add(name, value);
        }
        attr.Value = value;
        return attr;
    }

    /// <summary>
    /// Creates a deep clone of this node.
    /// </summary>
    public HtmlNode Clone()
    {
        return CloneNode(true);
    }

    /// <summary>
    /// Creates a clone of this node.
    /// </summary>
    /// <param name="deep">If true, clones all descendant nodes as well.</param>
    public HtmlNode CloneNode(bool deep)
    {
        var clone = new HtmlNode(_nodeType)
        {
            _name = _name,
            _innerHtml = _innerHtml,
            _outerHtml = _outerHtml,
            _line = _line,
            _linePosition = _linePosition,
            _streamPosition = _streamPosition,
            _outerStartIndex = _outerStartIndex,
            _innerStartIndex = _innerStartIndex,
            _outerLength = _outerLength,
            _innerLength = _innerLength
        };

        // Clone attributes
        if (_attributes != null && _attributes.Count > 0)
        {
            foreach (var attr in _attributes)
            {
                clone.Attributes.Add(attr.Clone());
            }
        }

        // Clone children if deep
        if (deep && _childNodes != null && _childNodes.Count > 0)
        {
            foreach (var child in _childNodes)
            {
                var clonedChild = child.CloneNode(true);
                clonedChild._parentNode = clone;
                clone.ChildNodes.Add(clonedChild);
            }
        }

        return clone;
    }

    /// <summary>
    /// Gets all descendant nodes.
    /// </summary>
    public IEnumerable<HtmlNode> Descendants(string? name = null)
    {
        if (_childNodes == null)
            yield break;

        foreach (var child in _childNodes)
        {
            if (name == null || string.Equals(child.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                yield return child;
            }

            foreach (var desc in child.Descendants(name))
            {
                yield return desc;
            }
        }
    }

    /// <summary>
    /// Gets all descendant nodes and self.
    /// </summary>
    public IEnumerable<HtmlNode> DescendantsAndSelf(string? name = null)
    {
        if (name == null || string.Equals(Name, name, StringComparison.OrdinalIgnoreCase))
        {
            yield return this;
        }

        foreach (var desc in Descendants(name))
        {
            yield return desc;
        }
    }

    /// <summary>
    /// Gets all ancestor nodes.
    /// </summary>
    public IEnumerable<HtmlNode> Ancestors(string? name = null)
    {
        var node = _parentNode;
        while (node != null)
        {
            if (name == null || string.Equals(node.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                yield return node;
            }
            node = node._parentNode;
        }
    }

    /// <summary>
    /// Gets all ancestor nodes and self.
    /// </summary>
    public IEnumerable<HtmlNode> AncestorsAndSelf(string? name = null)
    {
        if (name == null || string.Equals(Name, name, StringComparison.OrdinalIgnoreCase))
        {
            yield return this;
        }

        foreach (var ancestor in Ancestors(name))
        {
            yield return ancestor;
        }
    }

    /// <summary>
    /// Gets all element child nodes (excluding text and comments).
    /// </summary>
    public IEnumerable<HtmlNode> Elements(string? name = null)
    {
        if (_childNodes == null)
            yield break;

        foreach (var child in _childNodes)
        {
            if (child.NodeType != HtmlNodeType.Element)
                continue;

            if (name == null || string.Equals(child.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                yield return child;
            }
        }
    }

    /// <summary>
    /// Gets the first element with the specified name.
    /// </summary>
    public HtmlNode? Element(string name)
    {
        return Elements(name).FirstOrDefault();
    }

    /// <summary>
    /// Appends a child node to this node.
    /// </summary>
    public HtmlNode AppendChild(HtmlNode newChild)
    {
        newChild._ownerDocument = _ownerDocument;
        ChildNodes.Add(newChild);
        return newChild;
    }

    /// <summary>
    /// Prepends a child node to this node.
    /// </summary>
    public HtmlNode PrependChild(HtmlNode newChild)
    {
        newChild._ownerDocument = _ownerDocument;
        ChildNodes.Prepend(newChild);
        return newChild;
    }

    /// <summary>
    /// Inserts a child node after the specified reference node.
    /// </summary>
    public HtmlNode InsertAfter(HtmlNode newChild, HtmlNode refChild)
    {
        newChild._ownerDocument = _ownerDocument;
        ChildNodes.InsertAfter(newChild, refChild);
        return newChild;
    }

    /// <summary>
    /// Inserts a child node before the specified reference node.
    /// </summary>
    public HtmlNode InsertBefore(HtmlNode newChild, HtmlNode refChild)
    {
        newChild._ownerDocument = _ownerDocument;
        ChildNodes.InsertBefore(newChild, refChild);
        return newChild;
    }

    /// <summary>
    /// Removes a child node.
    /// </summary>
    public HtmlNode RemoveChild(HtmlNode oldChild)
    {
        ChildNodes.Remove(oldChild);
        return oldChild;
    }

    /// <summary>
    /// Removes a child node and optionally keeps its grandchildren.
    /// </summary>
    public HtmlNode RemoveChild(HtmlNode oldChild, bool keepGrandChildren)
    {
        if (keepGrandChildren && oldChild.HasChildNodes)
        {
            var index = ChildNodes.IndexOf(oldChild);
            var grandChildren = oldChild.ChildNodes.ToArray();
            for (int i = grandChildren.Length - 1; i >= 0; i--)
            {
                var grandChild = grandChildren[i];
                grandChild._parentNode = this;
                ChildNodes.Insert(index, grandChild);
            }
        }
        return RemoveChild(oldChild);
    }

    /// <summary>
    /// Replaces a child node with a new node.
    /// </summary>
    public HtmlNode ReplaceChild(HtmlNode newChild, HtmlNode oldChild)
    {
        newChild._ownerDocument = _ownerDocument;
        ChildNodes.Replace(oldChild, newChild);
        return oldChild;
    }

    /// <summary>
    /// Removes all children from this node.
    /// </summary>
    public void RemoveAllChildren()
    {
        _childNodes?.Clear();
    }

    /// <summary>
    /// Removes this node from its parent.
    /// </summary>
    public void Remove()
    {
        _parentNode?.RemoveChild(this);
    }

    /// <summary>
    /// Selects a list of nodes matching the XPath expression.
    /// </summary>
    public HtmlNodeCollection? SelectNodes(string xpath)
    {
        var navigator = new HtmlNodeNavigator(_ownerDocument!, this);
        var iterator = navigator.Select(xpath);
        
        if (iterator == null || iterator.Count == 0)
        {
            return _ownerDocument?.OptionEmptyCollection == true 
                ? new HtmlNodeCollection(null) 
                : null;
        }

        var nodes = new HtmlNodeCollection(null);
        while (iterator.MoveNext())
        {
            if (iterator.Current is HtmlNodeNavigator nav)
            {
                nodes.Add(nav.CurrentNode);
            }
        }

        if (nodes.Count == 0)
        {
            return _ownerDocument?.OptionEmptyCollection == true 
                ? new HtmlNodeCollection(null) 
                : null;
        }

        return nodes;
    }

    /// <summary>
    /// Selects the first node matching the XPath expression.
    /// </summary>
    public HtmlNode? SelectSingleNode(string xpath)
    {
        var nodes = SelectNodes(xpath);
        return nodes?.FirstOrDefault();
    }

    /// <summary>
    /// Creates a new element node.
    /// </summary>
    public static HtmlNode CreateNode(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        return doc.DocumentNode.FirstChild ?? throw new InvalidOperationException("Could not parse HTML");
    }

    /// <summary>
    /// Writes the content of the node to a TextWriter.
    /// </summary>
    public void WriteTo(TextWriter writer)
    {
        writer.Write(OuterHtml);
    }

    /// <summary>
    /// Writes the content of the node to an XmlWriter.
    /// </summary>
    public void WriteTo(System.Xml.XmlWriter writer)
    {
        switch (_nodeType)
        {
            case HtmlNodeType.Document:
                writer.WriteStartDocument();
                foreach (var child in ChildNodes)
                {
                    child.WriteTo(writer);
                }
                writer.WriteEndDocument();
                break;

            case HtmlNodeType.Text:
                writer.WriteString(InnerText);
                break;

            case HtmlNodeType.Comment:
                writer.WriteComment(_innerHtml ?? string.Empty);
                break;

            case HtmlNodeType.Element:
                writer.WriteStartElement(Name);
                foreach (var attr in Attributes)
                {
                    writer.WriteAttributeString(attr.Name, attr.Value);
                }
                foreach (var child in ChildNodes)
                {
                    child.WriteTo(writer);
                }
                writer.WriteEndElement();
                break;
        }
    }

    /// <summary>
    /// Gets the XPath to this node.
    /// </summary>
    public string XPath
    {
        get
        {
            var parts = new List<string>();
            var node = this;
            
            while (node != null && node.NodeType != HtmlNodeType.Document)
            {
                var index = 1;
                if (node._parentNode != null)
                {
                    foreach (var sibling in node._parentNode.ChildNodes)
                    {
                        if (ReferenceEquals(sibling, node))
                            break;
                        if (sibling.NodeType == HtmlNodeType.Element && 
                            string.Equals(sibling.Name, node.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            index++;
                        }
                    }
                }

                var part = node.NodeType switch
                {
                    HtmlNodeType.Text => "text()",
                    HtmlNodeType.Comment => "comment()",
                    _ => $"{node.Name}[{index}]"
                };
                
                parts.Add(part);
                node = node._parentNode;
            }

            parts.Reverse();
            return "/" + string.Join("/", parts);
        }
    }

    private void EnsureChildNodes()
    {
        _childNodes ??= new HtmlNodeCollection(this);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"HtmlNode: {Name}";
    }

    #endregion
}

/// <summary>
/// Flags that define specific behaviors for HTML elements.
/// </summary>
[Flags]
public enum HtmlElementFlag
{
    /// <summary>
    /// No special flags.
    /// </summary>
    None = 0,
    
    /// <summary>
    /// The element is a CData element (like script, style).
    /// </summary>
    CData = 1,
    
    /// <summary>
    /// The element is empty/void (like br, img, hr).
    /// </summary>
    Empty = 2,
    
    /// <summary>
    /// The element's closing tag is optional.
    /// </summary>
    Closed = 4,
    
    /// <summary>
    /// The element can overlap other elements.
    /// </summary>
    CanOverlap = 8
}
