using HtmlAgilityPack;
using HtmlAgilityPack.RegexParser;

Console.WriteLine("=== Phase 0: Balancing Groups Demo ===\n");
RegexBalancingDemo.RunAllTests();

Console.WriteLine("\n\n=== Phase 1: Tokenizer Demo ===\n");
TestTokenizer();

Console.WriteLine("\n\n=== Phase 2: Tree Builder Demo ===\n");
TestTreeBuilder();

Console.WriteLine("\n\n=== Phase 3: HAP Compatibility Tests ===\n");
TestHapCompatibility();

static void TestTokenizer()
{
    var tokenizer = new RegexTokenizer();
    
    // Test 1: Simple HTML
    Console.WriteLine("Test 1: Simple HTML");
    var html1 = "<html><body><div>Hello World</div></body></html>";
    var tokens1 = tokenizer.Tokenize(html1);
    Console.WriteLine($"  Input: {html1}");
    Console.WriteLine($"  Tokens ({tokens1.Count}):");
    foreach (var t in tokens1)
        Console.WriteLine($"    [{t.Line}:{t.LinePosition}] {t.Type}: {t}");
    Console.WriteLine();

    // Test 2: Attributes
    Console.WriteLine("Test 2: Attributes");
    var html2 = "<div class=\"container\" id='main' disabled data-value=123>Content</div>";
    var tokens2 = tokenizer.TokenizeWithAttributes(html2);
    Console.WriteLine($"  Input: {html2}");
    foreach (var t in tokens2)
    {
        Console.WriteLine($"    {t.Type}: {t}");
        if (t.Attributes != null)
        {
            foreach (var attr in t.Attributes)
                Console.WriteLine($"      - {attr}");
        }
    }
    Console.WriteLine();

    // Test 3: Comments and special content
    Console.WriteLine("Test 3: Comments, DOCTYPE, void elements");
    var html3 = "<!DOCTYPE html><!-- comment --><br><img src=\"x.png\"><hr/>";
    var tokens3 = tokenizer.Tokenize(html3);
    Console.WriteLine($"  Input: {html3}");
    foreach (var t in tokens3)
        Console.WriteLine($"    {t.Type}: {t}");
    Console.WriteLine();

    // Test 4: Multi-line with position tracking
    Console.WriteLine("Test 4: Multi-line position tracking");
    var html4 = @"<html>
<head>
  <title>Test</title>
</head>
<body>
  <div>Content</div>
</body>
</html>";
    var tokens4 = tokenizer.Tokenize(html4);
    Console.WriteLine("  Tokens with line:col positions:");
    foreach (var t in tokens4.Where(t => t.Type != TokenType.Text || !string.IsNullOrWhiteSpace(t.Content)))
        Console.WriteLine($"    [{t.Line}:{t.LinePosition}] {t.Type}: {t}");
    Console.WriteLine();

    // Test 5: Script tag (raw content)
    Console.WriteLine("Test 5: Script and style tags");
    var html5 = "<script>var x = '<div>not a tag</div>';</script><style>.foo { color: red; }</style>";
    var tokens5 = tokenizer.Tokenize(html5);
    Console.WriteLine($"  Input: {html5}");
    foreach (var t in tokens5)
        Console.WriteLine($"    {t.Type}: {t}");

    Console.WriteLine("\n✅ Tokenizer tests complete!");
}

static void TestTreeBuilder()
{
    // Test 1: Simple HTML with tree building
    Console.WriteLine("Test 1: Simple nested HTML");
    var html1 = "<html><head><title>Test</title></head><body><div>Hello</div></body></html>";
    var doc1 = ParseWithRegex(html1);
    Console.WriteLine($"  Input: {html1}");
    PrintTree(doc1.DocumentNode, "  ");
    Console.WriteLine();

    // Test 2: Nested divs
    Console.WriteLine("Test 2: Nested divs");
    var html2 = "<div id='outer'><div id='inner'>Content</div></div>";
    var doc2 = ParseWithRegex(html2);
    Console.WriteLine($"  Input: {html2}");
    PrintTree(doc2.DocumentNode, "  ");
    Console.WriteLine();

    // Test 3: Self-closing tags
    Console.WriteLine("Test 3: Self-closing/void elements");
    var html3 = "<p>Line 1<br>Line 2</p><img src='x.png'>";
    var doc3 = ParseWithRegex(html3);
    Console.WriteLine($"  Input: {html3}");
    PrintTree(doc3.DocumentNode, "  ");
    Console.WriteLine();

    // Test 4: Compare with standard HAP
    Console.WriteLine("Test 4: Compare with standard HAP");
    var html4 = "<div class='test'><span>Hello</span> World</div>";
    var regexDoc = ParseWithRegex(html4);
    var hapDoc = new HtmlDocument();
    hapDoc.LoadHtml(html4);
    
    Console.WriteLine($"  Input: {html4}");
    Console.WriteLine($"  Regex parser - InnerText: '{regexDoc.DocumentNode.InnerText}'");
    Console.WriteLine($"  Standard HAP - InnerText: '{hapDoc.DocumentNode.InnerText}'");
    
    var regexDiv = regexDoc.DocumentNode.SelectSingleNode("//div");
    var hapDiv = hapDoc.DocumentNode.SelectSingleNode("//div");
    Console.WriteLine($"  Regex div class: '{regexDiv?.GetAttributeValue("class", "")}'");
    Console.WriteLine($"  HAP div class: '{hapDiv?.GetAttributeValue("class", "")}'");
    Console.WriteLine();

    // Test 5: Implicit tag closing
    Console.WriteLine("Test 5: Implicit tag closing (<p><p>)");
    var html5 = "<p>First<p>Second<p>Third";
    var doc5 = ParseWithRegex(html5);
    Console.WriteLine($"  Input: {html5}");
    PrintTree(doc5.DocumentNode, "  ");
    var pCount = doc5.DocumentNode.SelectNodes("//p")?.Count ?? 0;
    Console.WriteLine($"  Found {pCount} <p> elements");

    Console.WriteLine("\n✅ Tree builder tests complete!");
}

static HtmlDocument ParseWithRegex(string html)
{
    var doc = new HtmlDocument();
    doc.LoadHtmlWithRegex(html);  // Use our new extension method
    return doc;
}

static void PrintTree(HtmlNode node, string indent)
{
    foreach (var child in node.ChildNodes)
    {
        var desc = child.NodeType switch
        {
            HtmlNodeType.Element => $"<{child.Name}>" + (child.Attributes.Count > 0 ? $" [{child.Attributes.Count} attrs]" : ""),
            HtmlNodeType.Text => $"TEXT: \"{Truncate(child.InnerText, 20)}\"",
            HtmlNodeType.Comment => $"COMMENT: \"{Truncate(child.InnerText, 20)}\"",
            _ => child.NodeType.ToString()
        };
        Console.WriteLine($"{indent}{desc}");
        
        if (child.HasChildNodes)
        {
            PrintTree(child, indent + "  ");
        }
    }
}

static string Truncate(string s, int max) => s.Length <= max ? s : s.Substring(0, max) + "...";

static void TestHapCompatibility()
{
    int passed = 0;
    int failed = 0;

    // Test cases adapted from HtmlAgilityPack tests
    var testCases = new (string Name, string Html, Func<HtmlDocument, HtmlDocument, bool> Compare)[]
    {
        ("Simple element", "<div>Hello</div>", (r, h) => 
            r.DocumentNode.InnerText == h.DocumentNode.InnerText),
        
        ("Nested elements", "<div><span>Inner</span></div>", (r, h) =>
            r.DocumentNode.SelectSingleNode("//span")?.InnerText == 
            h.DocumentNode.SelectSingleNode("//span")?.InnerText),
        
        ("Attributes", "<div class='test' id=\"foo\">Content</div>", (r, h) =>
            r.DocumentNode.SelectSingleNode("//div")?.GetAttributeValue("class", "") ==
            h.DocumentNode.SelectSingleNode("//div")?.GetAttributeValue("class", "")),
        
        ("Void elements (br)", "<p>Line1<br>Line2</p>", (r, h) =>
            r.DocumentNode.SelectNodes("//br")?.Count == 
            h.DocumentNode.SelectNodes("//br")?.Count),
        
        ("Void elements (img)", "<img src='x.png'><p>After</p>", (r, h) =>
            r.DocumentNode.SelectSingleNode("//p")?.InnerText ==
            h.DocumentNode.SelectSingleNode("//p")?.InnerText),

        ("Self-closing syntax", "<br/><hr/><input/>", (r, h) =>
            r.DocumentNode.ChildNodes.Count == h.DocumentNode.ChildNodes.Count),
        
        ("Deeply nested", "<div><div><div><div>Deep</div></div></div></div>", (r, h) =>
            r.DocumentNode.SelectSingleNode("//div/div/div/div")?.InnerText ==
            h.DocumentNode.SelectSingleNode("//div/div/div/div")?.InnerText),
        
        ("Multiple roots", "<div>A</div><div>B</div><div>C</div>", (r, h) =>
            r.DocumentNode.SelectNodes("//div")?.Count ==
            h.DocumentNode.SelectNodes("//div")?.Count),
        
        ("Text between tags", "<p>Hello <b>World</b>!</p>", (r, h) =>
            r.DocumentNode.InnerText.Trim() == h.DocumentNode.InnerText.Trim()),
        
        ("GetElementById", "<div id='target'>Found</div>", (r, h) =>
            r.GetElementbyId("target")?.InnerText ==
            h.GetElementbyId("target")?.InnerText),
        
        ("Comments", "<!-- comment --><div>Content</div>", (r, h) =>
            r.DocumentNode.SelectSingleNode("//div")?.InnerText ==
            h.DocumentNode.SelectSingleNode("//div")?.InnerText),
        
        ("Script content", "<script>var x = '<div>fake</div>';</script><div>Real</div>", (r, h) =>
            r.DocumentNode.SelectSingleNode("//div")?.InnerText ==
            h.DocumentNode.SelectSingleNode("//div")?.InnerText),
        
        ("Implicit p closing", "<p>A<p>B<p>C", (r, h) =>
            r.DocumentNode.SelectNodes("//p")?.Count ==
            h.DocumentNode.SelectNodes("//p")?.Count),
        
        ("Implicit li closing", "<ul><li>A<li>B<li>C</ul>", (r, h) =>
            r.DocumentNode.SelectNodes("//li")?.Count ==
            h.DocumentNode.SelectNodes("//li")?.Count),
        
        ("Mixed case tags", "<DIV><Span>Text</SPAN></div>", (r, h) =>
            r.DocumentNode.SelectSingleNode("//div")?.InnerText ==
            h.DocumentNode.SelectSingleNode("//div")?.InnerText),
        
        ("Boolean attributes", "<input disabled readonly>", (r, h) =>
            r.DocumentNode.SelectSingleNode("//input")?.GetAttributeValue("disabled", null) != null ==
            (h.DocumentNode.SelectSingleNode("//input")?.GetAttributeValue("disabled", null) != null)),
        
        ("Unquoted attributes", "<div class=test>Content</div>", (r, h) =>
            r.DocumentNode.SelectSingleNode("//div")?.GetAttributeValue("class", "") ==
            h.DocumentNode.SelectSingleNode("//div")?.GetAttributeValue("class", "")),
        
        ("DOCTYPE", "<!DOCTYPE html><html><body>Test</body></html>", (r, h) =>
            r.DocumentNode.SelectSingleNode("//body")?.InnerText ==
            h.DocumentNode.SelectSingleNode("//body")?.InnerText),
        
        ("Textarea content", "<textarea><div>Not a tag</div></textarea>", (r, h) =>
            r.DocumentNode.SelectSingleNode("//textarea")?.InnerHtml?.Contains("div") == true),
    };

    foreach (var (name, html, compare) in testCases)
    {
        try
        {
            var regexDoc = new HtmlDocument();
            regexDoc.OptionUseIdAttribute = true;  // Enable ID tracking
            regexDoc.LoadHtmlWithRegex(html);
            
            var hapDoc = new HtmlDocument();
            hapDoc.OptionUseIdAttribute = true;
            hapDoc.LoadHtml(html);

            if (compare(regexDoc, hapDoc))
            {
                Console.WriteLine($"  ✅ {name}");
                passed++;
            }
            else
            {
                Console.WriteLine($"  ❌ {name}");
                Console.WriteLine($"     Input: {html}");
                failed++;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  💥 {name}: {ex.Message}");
            failed++;
        }
    }

    Console.WriteLine();
    Console.WriteLine($"Results: {passed} passed, {failed} failed ({100.0 * passed / (passed + failed):F1}% compatibility)");
}
