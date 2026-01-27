# 🧪 Regex HTML Parser: The .NET Exception

> "You can't parse HTML with regex."  
> — [Stack Overflow, 2009](https://stackoverflow.com/a/1732454)

**Challenge accepted.**

## What Is This?

This is a **regex-powered HTML parser** that implements the HtmlAgilityPack interface using **.NET's balancing groups** feature. It demonstrates that the "impossible" is possible — with an asterisk.

## 🌟 The Pure Regex Parser

The crown jewel of this project is `PureRegexParser` — a **single-pass HTML parser built from ONE UNIFIED REGEX** composed via string interpolation. This proves that .NET regex can handle the full complexity of HTML parsing:

```csharp
// The "impossible" parser - ONE regex handles everything
var doc = new HtmlDocument();
doc.LoadHtmlWithPureRegex("<div><div>Nested!</div></div>");
```

### What the Pure Parser Handles (via regex alone!)

| Feature | Implementation |
|---------|---------------|
| **Nested same-tags** | `(?<DEPTH>)` push, `(?<-DEPTH>)` pop, `(?(DEPTH)(?!))` balance check |
| **Implicit tag closing** | `<p>A<p>B<p>C` → 3 separate `<p>` elements via lookahead patterns |
| **Raw text elements** | `<script>`, `<style>`, `<textarea>` content preserved literally |
| **Void elements** | `<br>`, `<img>`, `<input>` etc. treated as self-closing |
| **Attributes** | Quoted, unquoted, and boolean attributes |

All in **ONE regex pattern** built via string composition at static initialization time.

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

This project provides **two parser implementations** via the `IHtmlParser` interface:

### 1. PureRegexParser (Single-Pass) — ⭐ The Cool One
- **ONE unified regex** handles all HTML constructs
- Uses .NET balancing groups for nested tag matching
- Built via string composition (no [GeneratedRegex] needed for the main pattern)
- Handles implicit closing via lookahead patterns
- ~500 lines including the massive regex pattern

```csharp
doc.LoadHtmlWithPureRegex(html);  // Use the pure regex parser
```

### 2. MultiPassRegexParser (Tokenize → Build)
- **Pass 1**: Tokenize HTML using regex
- **Pass 2**: Parse attributes using regex  
- **Pass 3**: Build tree with regex-assisted rules
- More traditional architecture, battle-tested

```csharp
doc.LoadHtmlWithRegex(html);  // Use the multi-pass parser (default)
```

### Parser Comparison

| Feature | PureRegexParser | MultiPassRegexParser |
|---------|----------------|---------------------|
| Architecture | Single unified regex | Multi-pass tokenization |
| Nested elements | ✅ Balancing groups | ✅ Stack-based |
| Implicit closing | ✅ Lookahead patterns | ✅ Rule-based |
| Raw text elements | ✅ Regex capture | ✅ State tracking |
| Test coverage | 50/50 tests passing | 50/50 tests passing |

**Both parsers pass all 50 xUnit tests!**

## Usage

```csharp
using HtmlAgilityPack;

var doc = new HtmlDocument();
doc.OptionUseIdAttribute = true;  // Enable GetElementById

// Pure regex parser - the "impossible" single-pass approach
doc.LoadHtmlWithPureRegex("<p>A<p>B<p>C");  // Implicit closing works!

// Multi-pass parser - traditional tokenize → build approach
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

This **is** a demonstration that:
1. The core "impossible" operation (nested matching) works in .NET regex
2. A single unified regex can handle HTML parsing via string composition
3. The claim needs an asterisk: *"You can't parse HTML with regex — except in .NET"*

The PureRegexParser uses regex patterns built via string composition:
- **Main unified pattern**: DOCTYPE, comments, self-closing, void elements, raw text, implicit closing, balanced elements
- **Attribute pattern**: Individual attribute parsing (name, quoted values, unquoted values, boolean attrs)

Both patterns are defined as `const string` components and assembled at static initialization time.

## Files

```
src/HtmlAgilityPack.Net7/RegexParser/
├── IHtmlParser.cs             # Common interface for both parsers
├── PureRegexParser.cs         # ⭐ THE IMPOSSIBLE PARSER - single unified regex
├── MultiPassRegexParser.cs    # Traditional multi-pass approach
├── RegexBalancingDemo.cs      # Proof of concept: balancing groups work
├── Token.cs                   # Token types and structures
├── HtmlPatterns.cs            # [GeneratedRegex] patterns for multi-pass
├── RegexTokenizer.cs          # HTML → tokens (multi-pass)
├── RegexTreeBuilder.cs        # Tokens → HtmlNode tree (multi-pass)
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
