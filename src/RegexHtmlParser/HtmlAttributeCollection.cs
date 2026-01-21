// Description: Regex Html Parser - A regex-powered HTML parser using .NET balancing groups.

using System.Collections;

namespace RegexHtmlParser;

/// <summary>
/// Represents a collection of HTML attributes.
/// </summary>
public class HtmlAttributeCollection : IList<HtmlAttribute>
{
    private readonly List<HtmlAttribute> _items = new();
    private readonly HtmlNode? _ownerNode;

    /// <summary>
    /// Initializes a new instance of the HtmlAttributeCollection class.
    /// </summary>
    /// <param name="ownerNode">The owner node.</param>
    internal HtmlAttributeCollection(HtmlNode? ownerNode)
    {
        _ownerNode = ownerNode;
    }

    /// <summary>
    /// Gets or sets the attribute at the specified index.
    /// </summary>
    public HtmlAttribute this[int index]
    {
        get => _items[index];
        set
        {
            // Remove old attribute name from dictionary
            var oldAttr = _items[index];
            _items[index] = value;
            value._ownerNode = _ownerNode;
        }
    }

    /// <summary>
    /// Gets the attribute with the specified name (case-insensitive).
    /// </summary>
    public HtmlAttribute? this[string name]
    {
        get
        {
            if (string.IsNullOrEmpty(name))
                return null;

            return _items.FirstOrDefault(a => 
                string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase));
        }
        set
        {
            if (string.IsNullOrEmpty(name) || value == null)
                return;

            var existing = this[name];
            if (existing != null)
            {
                var index = _items.IndexOf(existing);
                _items[index] = value;
                value._ownerNode = _ownerNode;
            }
            else
            {
                Add(value);
            }
        }
    }

    /// <summary>
    /// Gets the number of attributes in the collection.
    /// </summary>
    public int Count => _items.Count;

    /// <summary>
    /// Gets a value indicating whether the collection is read-only.
    /// </summary>
    public bool IsReadOnly => false;

    /// <summary>
    /// Adds an attribute to the collection.
    /// </summary>
    public void Add(HtmlAttribute item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        item._ownerNode = _ownerNode;
        _items.Add(item);
    }

    /// <summary>
    /// Adds an attribute with the specified name and value.
    /// </summary>
    public HtmlAttribute Add(string name, string? value = null)
    {
        var attr = new HtmlAttribute(name, value)
        {
            _ownerNode = _ownerNode,
            _ownerDocument = _ownerNode?.OwnerDocument
        };
        _items.Add(attr);
        return attr;
    }

    /// <summary>
    /// Appends an attribute to the collection.
    /// </summary>
    public HtmlAttribute Append(HtmlAttribute attribute)
    {
        Add(attribute);
        return attribute;
    }

    /// <summary>
    /// Appends an attribute with the given name and value.
    /// </summary>
    public HtmlAttribute Append(string name, string? value = null)
    {
        return Add(name, value);
    }

    /// <summary>
    /// Prepends an attribute to the collection.
    /// </summary>
    public HtmlAttribute Prepend(HtmlAttribute attribute)
    {
        Insert(0, attribute);
        return attribute;
    }

    /// <summary>
    /// Removes all attributes from the collection.
    /// </summary>
    public void Clear()
    {
        foreach (var attr in _items)
        {
            attr._ownerNode = null;
        }
        _items.Clear();
    }

    /// <summary>
    /// Determines whether the collection contains the specified attribute.
    /// </summary>
    public bool Contains(HtmlAttribute item) => _items.Contains(item);

    /// <summary>
    /// Determines whether the collection contains an attribute with the specified name.
    /// </summary>
    public bool Contains(string name)
    {
        return _items.Any(a => string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Copies the attributes to an array.
    /// </summary>
    public void CopyTo(HtmlAttribute[] array, int arrayIndex)
    {
        _items.CopyTo(array, arrayIndex);
    }

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    public IEnumerator<HtmlAttribute> GetEnumerator() => _items.GetEnumerator();

    /// <summary>
    /// Returns the index of the specified attribute.
    /// </summary>
    public int IndexOf(HtmlAttribute item) => _items.IndexOf(item);

    /// <summary>
    /// Inserts an attribute at the specified index.
    /// </summary>
    public void Insert(int index, HtmlAttribute item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        item._ownerNode = _ownerNode;
        _items.Insert(index, item);
    }

    /// <summary>
    /// Removes the specified attribute from the collection.
    /// </summary>
    public bool Remove(HtmlAttribute item)
    {
        if (item != null)
        {
            item._ownerNode = null;
        }
        return _items.Remove(item!);
    }

    /// <summary>
    /// Removes the attribute with the specified name.
    /// </summary>
    public void Remove(string name)
    {
        var attr = this[name];
        if (attr != null)
        {
            Remove(attr);
        }
    }

    /// <summary>
    /// Removes the attribute at the specified index.
    /// </summary>
    public void RemoveAt(int index)
    {
        var attr = _items[index];
        attr._ownerNode = null;
        _items.RemoveAt(index);
    }

    /// <summary>
    /// Removes all attributes from the collection.
    /// </summary>
    public void RemoveAll()
    {
        Clear();
    }

    /// <summary>
    /// Returns a non-generic enumerator.
    /// </summary>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Gets all attribute names.
    /// </summary>
    public IEnumerable<string> AttributeNames => _items.Select(a => a.Name);

    /// <summary>
    /// Gets all attribute values.
    /// </summary>
    public IEnumerable<string> AttributeValues => _items.Select(a => a.Value);
}
