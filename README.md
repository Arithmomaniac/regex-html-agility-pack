# 🧪 Regex HTML Parser: The .NET Exception

> "You can't parse HTML with regex."  
> — [Stack Overflow, 2009](https://stackoverflow.com/a/1732454)

**Challenge accepted.**

## What Is This?

This is a **regex-powered HTML parser** that implements the HtmlAgilityPack interface using **.NET's balancing groups** feature. It demonstrates that the "impossible" is possible — with an asterisk.

## 🌟 Two Regex Parsers, Both Using Balancing Groups

This project provides **two complete HTML parser implementations**, both proving that regex CAN parse HTML in .NET:

### 1. PureRegexParser — ONE Regex To Rule Them All

The `PureRegexParser` is a **single-pass HTML parser built from ONE UNIFIED REGEX** composed via string interpolation. Everything—including attribute parsing—is embedded in one massive regex pattern:

```csharp
var doc = new HtmlDocument();
doc.LoadHtmlWithPureRegex("<div class='outer'><div>Nested!</div></div>");
```

**Key features:**
- **ZERO separate regexes** - attributes captured directly in the main pattern via `.Captures` collection
- Uses .NET balancing groups (`(?<DEPTH>)`, `(?<-DEPTH>)`, `(?(DEPTH)(?!))`) for nested tag matching
- Handles implicit closing via lookahead patterns
- Raw text elements (`<script>`, `<style>`, `<textarea>`) preserved literally

### 2. MultiPassRegexParser — Regex All The Way Down

The `MultiPassRegexParser` uses a traditional multi-pass architecture, but **every pass is powered by regex**:

```csharp
var doc = new HtmlDocument();
doc.LoadHtmlWithRegex("<div><div>Nested!</div></div>");
```

**Key features:**
- **Pass 1**: Master tokenizer regex breaks HTML into tokens
- **Pass 2**: Attribute parser regex extracts individual attributes
- **Pass 3**: Regex-based element classification (void, block, raw text)
- **Pass 4**: Regex-based implicit closing rules
- Uses `[GeneratedRegex]` source generators for compile-time pattern optimization

**Both parsers use regex throughout. Both prove the same point: .NET regex with balancing groups CAN parse HTML.**

## The Claim vs. Reality

### The Famous Argument
Regular expressions can only match **regular languages**. HTML has nested structures (like `<div><div></div></div>`), which require a **context-free grammar**. Therefore, regex cannot parse HTML. QED.

### The .NET Exception
.NET regex has **balancing groups** — a feature that gives regex a stack:

```csharp
(?<open>)     // Push to named stack
(?<-open>)    // Pop from named stack
(?(open)(?!)) // Conditional: fail if stack not empty
```

This isn't standard regex. This is regex with a **pushdown automaton**. It can match nested structures. It can count. It can balance.

## The Proof

Here's the core pattern that matches balanced tags with arbitrary nesting depth:

```regex
<(?<tagname>[a-zA-Z][a-zA-Z0-9:-]*)(?<attrs>[^>]*)>    # Opening tag
(?<content>
  (?>                                                   # Atomic group
    [^<]+                                               # Text content
    | <(?<DEPTH>)\k<tagname>\b[^>]*>                    # Same tag: PUSH
    | </\k<tagname>\s*>(?<-DEPTH>)                      # Same close: POP
    | <(?!/?\k<tagname>\b)[^>]+>                        # Other tags
  )*
)
(?(DEPTH)(?!))                                          # FAIL if unclosed
</\k<tagname>\s*>                                       # Closing tag
```

**Test it yourself:**
| Input | Result |
|-------|--------|
| `<div></div>` | ✅ Match |
| `<div><div></div></div>` | ✅ Match, captures inner |
| `<div><div><div></div></div></div>` | ✅ Match |
| `<div><span><div></div></span></div>` | ✅ Match |
| `<div><div></div>` | ❌ Fail (unclosed outer) |

## Dual Parser Architecture

Both parsers implement `IHtmlParser` and pass all 50 tests:

### Parser Comparison

| Feature | PureRegexParser | MultiPassRegexParser |
|---------|----------------|---------------------|
| **Architecture** | Single unified regex | Multi-pass with regex at each stage |
| **Balancing groups** | ✅ For nested elements | ✅ Used in HtmlPatterns factory |
| **Attribute parsing** | ✅ Embedded in main regex | ✅ Separate [GeneratedRegex] |
| **Source generators** | ❌ Uses runtime composition | ✅ Uses [GeneratedRegex] |
| **Implicit closing** | ✅ Lookahead patterns | ✅ Regex rule matching |
| **Raw text elements** | ✅ Regex capture | ✅ State tracking + regex |
| **Test coverage** | 50/50 tests passing | 50/50 tests passing |

## Usage

```csharp
using HtmlAgilityPack;

var doc = new HtmlDocument();
doc.OptionUseIdAttribute = true;  // Enable GetElementById

// Pure regex parser - ONE regex does everything
doc.LoadHtmlWithPureRegex("<p>A<p>B<p>C");  // Implicit closing works!

// Multi-pass regex parser - regex at every stage
doc.LoadHtmlWithRegex("<div><div>Nested!</div></div>");

// Custom parser injection
doc.LoadHtmlWithParser(html, new PureRegexParser());

// Same API as always
var inner = doc.DocumentNode.SelectSingleNode("//div/div");
Console.WriteLine(inner.InnerText);
```

## Compatibility

Tested against HtmlAgilityPack behavior:

```
✅ Simple element
✅ Nested elements (including same-tag nesting)
✅ Attributes (quoted, unquoted, boolean)
✅ Void elements (br, img, input, etc.)
✅ Self-closing syntax
✅ Deeply nested structures
✅ Multiple root elements
✅ Text between tags
✅ GetElementById
✅ Comments
✅ Script/style content preservation
✅ Implicit tag closing (p, li, dt, dd)
✅ Mixed case tags
✅ DOCTYPE handling
✅ Textarea raw content

Results: 50/50 tests passing (100% compatibility)
```

## Intellectual Honesty

Both parsers demonstrate that:
1. The core "impossible" operation (nested matching) works in .NET regex via balancing groups
2. A complete HTML parser can be built with regex as the primary parsing mechanism
3. The claim needs an asterisk: *"You can't parse HTML with regex — except in .NET"*

**PureRegexParser**: ONE regex pattern handles everything including attribute extraction via `.Captures` collection.

**MultiPassRegexParser**: Regex at every stage—tokenization, attributes, classification, and implicit closing rules—all powered by `.NET regex` including balancing groups in the pattern factory.

## Files

```
src/HtmlAgilityPack.Net7/RegexParser/
├── IHtmlParser.cs             # Common interface for both parsers
├── PureRegexParser.cs         # ⭐ ONE REGEX - balancing groups + embedded attributes
├── MultiPassRegexParser.cs    # Regex at every pass - also uses balancing groups
├── RegexBalancingDemo.cs      # Proof of concept: balancing groups work
├── Token.cs                   # Token types and structures
├── HtmlPatterns.cs            # [GeneratedRegex] patterns including balancing group factory
├── RegexTokenizer.cs          # HTML → tokens via regex
├── RegexTreeBuilder.cs        # Tokens → HtmlNode tree with regex rules
└── HtmlDocumentRegexExtensions.cs  # Extension methods
```

## References

- [Stack Overflow: RegEx match open tags except XHTML self-contained tags](https://stackoverflow.com/a/1732454) — The famous answer
- [.NET Balancing Groups Documentation](https://docs.microsoft.com/en-us/dotnet/standard/base-types/grouping-constructs-in-regular-expressions#balancing-group-definitions)
- [Regular Expression Improvements in .NET 7](https://devblogs.microsoft.com/dotnet/regular-expression-improvements-in-dotnet-7/) — Source generators

## License

MIT

---

*"I'm learnding!"* — Ralph Wiggum
