// Description: Regex Html Parser - A regex-powered HTML parser using .NET balancing groups.

using System.Collections;

namespace RegexHtmlParser;

/// <summary>
/// Represents a collection of HTML nodes.
/// </summary>
public class HtmlNodeCollection : IList<HtmlNode>
{
    private readonly List<HtmlNode> _items = new();
    private readonly HtmlNode? _parentNode;

    /// <summary>
    /// Initializes a new instance of the HtmlNodeCollection class.
    /// </summary>
    /// <param name="parentNode">The parent node of this collection.</param>
    public HtmlNodeCollection(HtmlNode? parentNode)
    {
        _parentNode = parentNode;
    }

    /// <summary>
    /// Gets the parent node of this collection.
    /// </summary>
    internal HtmlNode? ParentNode => _parentNode;

    /// <summary>
    /// Gets the index of the specified node.
    /// </summary>
    public int this[HtmlNode node]
    {
        get
        {
            int index = GetNodeIndex(node);
            if (index == -1)
                throw new ArgumentOutOfRangeException(nameof(node), 
                    $"Node \"{node.CloneNode(false).OuterHtml}\" was not found in the collection");
            return index;
        }
    }

    /// <summary>
    /// Gets the first node with the specified name.
    /// </summary>
    public HtmlNode? this[string nodeName]
    {
        get
        {
            return _items.FirstOrDefault(n => 
                string.Equals(n.Name, nodeName, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// Gets or sets the node at the specified index.
    /// </summary>
    public HtmlNode this[int index]
    {
        get => _items[index];
        set
        {
            var oldNode = _items[index];
            oldNode._parentNode = null;
            
            _items[index] = value;
            value._parentNode = _parentNode;
            
            // Update sibling references
            if (index > 0)
            {
                _items[index - 1]._nextNode = value;
                value._prevNode = _items[index - 1];
            }
            if (index < _items.Count - 1)
            {
                _items[index + 1]._prevNode = value;
                value._nextNode = _items[index + 1];
            }
        }
    }

    /// <summary>
    /// Gets the number of nodes in the collection.
    /// </summary>
    public int Count => _items.Count;

    /// <summary>
    /// Gets a value indicating whether the collection is read-only.
    /// </summary>
    public bool IsReadOnly => false;

    /// <summary>
    /// Adds a node to the collection.
    /// </summary>
    public void Add(HtmlNode item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        // Only set parent if this collection has a parent node.
        // This preserves original parent references when adding to result collections.
        if (_parentNode != null)
        {
            item._parentNode = _parentNode;
        }

        // Update sibling references only if this collection has a parent node
        // (i.e., it's a real child node collection, not a results collection)
        if (_parentNode != null && _items.Count > 0)
        {
            var lastItem = _items[^1];
            lastItem._nextNode = item;
            item._prevNode = lastItem;
        }
        
        if (_parentNode != null)
        {
            item._nextNode = null;
        }
        _items.Add(item);
    }

    /// <summary>
    /// Appends a node to the collection.
    /// </summary>
    public void Append(HtmlNode node)
    {
        Add(node);
    }

    /// <summary>
    /// Prepends a node to the collection.
    /// </summary>
    public void Prepend(HtmlNode node)
    {
        Insert(0, node);
    }

    /// <summary>
    /// Removes all nodes from the collection.
    /// </summary>
    public void Clear()
    {
        foreach (var node in _items)
        {
            node._parentNode = null;
            node._nextNode = null;
            node._prevNode = null;
        }
        _items.Clear();
    }

    /// <summary>
    /// Determines whether the collection contains the specified node.
    /// </summary>
    public bool Contains(HtmlNode item) => _items.Contains(item);

    /// <summary>
    /// Copies the nodes to an array.
    /// </summary>
    public void CopyTo(HtmlNode[] array, int arrayIndex)
    {
        _items.CopyTo(array, arrayIndex);
    }

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    public IEnumerator<HtmlNode> GetEnumerator() => _items.GetEnumerator();

    /// <summary>
    /// Returns the index of the specified node.
    /// </summary>
    public int IndexOf(HtmlNode item) => _items.IndexOf(item);

    /// <summary>
    /// Gets the index of the specified node in the collection.
    /// </summary>
    public int GetNodeIndex(HtmlNode node)
    {
        for (int i = 0; i < _items.Count; i++)
        {
            if (ReferenceEquals(_items[i], node))
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Inserts a node at the specified index.
    /// </summary>
    public void Insert(int index, HtmlNode item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        item._parentNode = _parentNode;
        
        // Update sibling references
        if (index > 0 && index <= _items.Count)
        {
            item._prevNode = _items[index - 1];
            _items[index - 1]._nextNode = item;
        }
        if (index < _items.Count)
        {
            item._nextNode = _items[index];
            _items[index]._prevNode = item;
        }
        
        _items.Insert(index, item);
    }

    /// <summary>
    /// Inserts a node after the specified reference node.
    /// </summary>
    public bool InsertAfter(HtmlNode newNode, HtmlNode refNode)
    {
        var index = IndexOf(refNode);
        if (index == -1)
            return false;
        Insert(index + 1, newNode);
        return true;
    }

    /// <summary>
    /// Inserts a node before the specified reference node.
    /// </summary>
    public bool InsertBefore(HtmlNode newNode, HtmlNode refNode)
    {
        var index = IndexOf(refNode);
        if (index == -1)
            return false;
        Insert(index, newNode);
        return true;
    }

    /// <summary>
    /// Removes the specified node from the collection.
    /// </summary>
    public bool Remove(HtmlNode item)
    {
        var index = IndexOf(item);
        if (index == -1)
            return false;

        RemoveAt(index);
        return true;
    }

    /// <summary>
    /// Removes the node at the specified index.
    /// </summary>
    public void RemoveAt(int index)
    {
        var node = _items[index];
        
        // Update sibling references
        if (node._prevNode != null)
            node._prevNode._nextNode = node._nextNode;
        if (node._nextNode != null)
            node._nextNode._prevNode = node._prevNode;
        
        node._parentNode = null;
        node._nextNode = null;
        node._prevNode = null;
        
        _items.RemoveAt(index);
    }

    /// <summary>
    /// Replaces the specified old node with a new node.
    /// </summary>
    public bool Replace(HtmlNode oldNode, HtmlNode newNode)
    {
        var index = IndexOf(oldNode);
        if (index == -1)
            return false;

        this[index] = newNode;
        return true;
    }

    /// <summary>
    /// Returns a non-generic enumerator.
    /// </summary>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Finds all descendant nodes matching the specified name.
    /// </summary>
    public IEnumerable<HtmlNode> Descendants(string? name = null)
    {
        foreach (var node in _items)
        {
            foreach (var desc in node.Descendants(name))
            {
                yield return desc;
            }
        }
    }

    /// <summary>
    /// Gets all elements (non-text, non-comment nodes).
    /// </summary>
    public IEnumerable<HtmlNode> Elements()
    {
        return _items.Where(n => n.NodeType == HtmlNodeType.Element);
    }

    /// <summary>
    /// Gets all elements with the specified name.
    /// </summary>
    public IEnumerable<HtmlNode> Elements(string name)
    {
        return _items.Where(n => 
            n.NodeType == HtmlNodeType.Element && 
            string.Equals(n.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets the first node in the collection.
    /// </summary>
    public HtmlNode? First => _items.Count > 0 ? _items[0] : null;

    /// <summary>
    /// Gets the last node in the collection.
    /// </summary>
    public HtmlNode? Last => _items.Count > 0 ? _items[^1] : null;

    /// <summary>
    /// Converts the collection to an array.
    /// </summary>
    public HtmlNode[] ToArray() => _items.ToArray();

    /// <summary>
    /// Finds all nodes matching the given predicate.
    /// </summary>
    public IEnumerable<HtmlNode> FindAll(Func<HtmlNode, bool> predicate)
    {
        return _items.Where(predicate);
    }

    /// <summary>
    /// Finds the first node matching the given predicate.
    /// </summary>
    public HtmlNode? FindFirst(Func<HtmlNode, bool> predicate)
    {
        return _items.FirstOrDefault(predicate);
    }
}
