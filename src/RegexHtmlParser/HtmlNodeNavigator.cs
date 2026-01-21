// Description: Regex Html Parser - A regex-powered HTML parser using .NET balancing groups.

using System.Xml;
using System.Xml.XPath;

namespace RegexHtmlParser;

/// <summary>
/// Provides XPath navigation capabilities over HtmlNode objects.
/// </summary>
public class HtmlNodeNavigator : XPathNavigator
{
    private readonly HtmlDocument _document;
    private HtmlNode _currentNode;
    private int _attributeIndex = -1;

    /// <summary>
    /// Initializes a new instance of the HtmlNodeNavigator class.
    /// </summary>
    public HtmlNodeNavigator(HtmlDocument document, HtmlNode currentNode)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _currentNode = currentNode ?? throw new ArgumentNullException(nameof(currentNode));
    }

    /// <summary>
    /// Gets the current node.
    /// </summary>
    public HtmlNode CurrentNode => _currentNode;

    /// <summary>
    /// Gets the base URI of the current node.
    /// </summary>
    public override string BaseURI => string.Empty;

    /// <summary>
    /// Gets a value indicating whether the current node is an empty element.
    /// </summary>
    public override bool IsEmptyElement => !_currentNode.HasChildNodes;

    /// <summary>
    /// Gets the local name of the current node.
    /// </summary>
    public override string LocalName
    {
        get
        {
            if (_attributeIndex >= 0)
            {
                return _currentNode.Attributes[_attributeIndex].Name;
            }
            return _currentNode.Name;
        }
    }

    /// <summary>
    /// Gets the qualified name of the current node.
    /// </summary>
    public override string Name => LocalName;

    /// <summary>
    /// Gets the namespace URI of the current node.
    /// </summary>
    public override string NamespaceURI => string.Empty;

    /// <summary>
    /// Gets the XmlNameTable associated with this navigator.
    /// </summary>
    public override XmlNameTable NameTable => _document.NameTable;

    /// <summary>
    /// Gets the type of the current node.
    /// </summary>
    public override XPathNodeType NodeType
    {
        get
        {
            if (_attributeIndex >= 0)
                return XPathNodeType.Attribute;

            return _currentNode.NodeType switch
            {
                HtmlNodeType.Document => XPathNodeType.Root,
                HtmlNodeType.Element => XPathNodeType.Element,
                HtmlNodeType.Text => XPathNodeType.Text,
                HtmlNodeType.Comment => XPathNodeType.Comment,
                _ => XPathNodeType.Element
            };
        }
    }

    /// <summary>
    /// Gets the namespace prefix of the current node.
    /// </summary>
    public override string Prefix => string.Empty;

    /// <summary>
    /// Gets the string value of the current node.
    /// </summary>
    public override string Value
    {
        get
        {
            if (_attributeIndex >= 0)
            {
                return _currentNode.Attributes[_attributeIndex].Value;
            }
            return _currentNode.InnerText;
        }
    }

    /// <summary>
    /// Creates a copy of this navigator.
    /// </summary>
    public override XPathNavigator Clone()
    {
        var clone = new HtmlNodeNavigator(_document, _currentNode)
        {
            _attributeIndex = _attributeIndex
        };
        return clone;
    }

    /// <summary>
    /// Determines whether two navigators are positioned at the same node.
    /// </summary>
    public override bool IsSamePosition(XPathNavigator other)
    {
        if (other is HtmlNodeNavigator nav)
        {
            return ReferenceEquals(_currentNode, nav._currentNode) && 
                   _attributeIndex == nav._attributeIndex;
        }
        return false;
    }

    /// <summary>
    /// Moves to the first attribute of the current element.
    /// </summary>
    public override bool MoveToFirstAttribute()
    {
        if (_currentNode.NodeType != HtmlNodeType.Element || !_currentNode.HasAttributes)
            return false;

        _attributeIndex = 0;
        return true;
    }

    /// <summary>
    /// Moves to the first child of the current node.
    /// </summary>
    public override bool MoveToFirstChild()
    {
        if (_attributeIndex >= 0)
            return false;

        if (!_currentNode.HasChildNodes)
            return false;

        _currentNode = _currentNode.FirstChild!;
        return true;
    }

    /// <summary>
    /// Moves to the first namespace node of the current element.
    /// </summary>
    public override bool MoveToFirstNamespace(XPathNamespaceScope namespaceScope)
    {
        return false; // HTML doesn't have namespaces in the XML sense
    }

    /// <summary>
    /// Moves to the node that has the specified Id attribute.
    /// </summary>
    public override bool MoveToId(string id)
    {
        var node = _document.GetElementById(id);
        if (node == null)
            return false;

        _currentNode = node;
        _attributeIndex = -1;
        return true;
    }

    /// <summary>
    /// Moves to the next attribute.
    /// </summary>
    public override bool MoveToNextAttribute()
    {
        if (_attributeIndex < 0)
            return false;

        if (_attributeIndex >= _currentNode.Attributes.Count - 1)
            return false;

        _attributeIndex++;
        return true;
    }

    /// <summary>
    /// Moves to the next namespace node.
    /// </summary>
    public override bool MoveToNextNamespace(XPathNamespaceScope namespaceScope)
    {
        return false; // HTML doesn't have namespaces in the XML sense
    }

    /// <summary>
    /// Moves to the next sibling of the current node.
    /// </summary>
    public override bool MoveToNext()
    {
        if (_attributeIndex >= 0)
            return false;

        if (_currentNode.NextSibling == null)
            return false;

        _currentNode = _currentNode.NextSibling;
        return true;
    }

    /// <summary>
    /// Moves to the parent of the current node.
    /// </summary>
    public override bool MoveToParent()
    {
        if (_attributeIndex >= 0)
        {
            _attributeIndex = -1;
            return true;
        }

        if (_currentNode.ParentNode == null)
            return false;

        _currentNode = _currentNode.ParentNode;
        return true;
    }

    /// <summary>
    /// Moves to the previous sibling of the current node.
    /// </summary>
    public override bool MoveToPrevious()
    {
        if (_attributeIndex >= 0)
            return false;

        if (_currentNode.PreviousSibling == null)
            return false;

        _currentNode = _currentNode.PreviousSibling;
        return true;
    }

    /// <summary>
    /// Moves to the same position as the specified navigator.
    /// </summary>
    public override bool MoveTo(XPathNavigator other)
    {
        if (other is HtmlNodeNavigator nav)
        {
            if (!ReferenceEquals(_document, nav._document))
                return false;

            _currentNode = nav._currentNode;
            _attributeIndex = nav._attributeIndex;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Moves to the root of the document.
    /// </summary>
    public override void MoveToRoot()
    {
        _currentNode = _document.DocumentNode;
        _attributeIndex = -1;
    }

    /// <summary>
    /// Gets the value of the specified attribute.
    /// </summary>
    public override string GetAttribute(string localName, string namespaceURI)
    {
        return _currentNode.GetAttributeValue(localName, string.Empty);
    }

    /// <summary>
    /// Moves to the attribute with the specified name.
    /// </summary>
    public override bool MoveToAttribute(string localName, string namespaceURI)
    {
        if (_currentNode.NodeType != HtmlNodeType.Element)
            return false;

        for (int i = 0; i < _currentNode.Attributes.Count; i++)
        {
            if (string.Equals(_currentNode.Attributes[i].Name, localName, StringComparison.OrdinalIgnoreCase))
            {
                _attributeIndex = i;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Moves to the namespace node with the specified name.
    /// </summary>
    public override bool MoveToNamespace(string name)
    {
        return false; // HTML doesn't have namespaces in the XML sense
    }
}
