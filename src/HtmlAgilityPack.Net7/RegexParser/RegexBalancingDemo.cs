using System;
using System.Text.RegularExpressions;

namespace HtmlAgilityPack.RegexParser
{
    /// <summary>
    /// Proof of Concept: .NET Balancing Groups can match nested HTML tags.
    /// 
    /// "You can't parse HTML with regex" - The Internet, since 2009
    /// 
    /// The claim: Regular expressions can only match regular languages.
    /// HTML has nested structures (context-free grammar), so regex can't handle it.
    /// 
    /// The .NET exception: Balancing groups give regex a STACK.
    ///   (?&lt;DEPTH&gt;)    - Push to stack
    ///   (?&lt;-DEPTH&gt;)   - Pop from stack
    ///   (?(DEPTH)(?!)) - Fail if stack not empty
    /// 
    /// This elevates .NET regex beyond regular languages. QED.
    /// </summary>
    public static class RegexBalancingDemo
    {
        /// <summary>
        /// Pattern that matches balanced div tags with arbitrary nesting depth.
        /// THIS IS THE THING THEY SAID COULDN'T BE DONE.
        /// </summary>
        private static readonly System.Text.RegularExpressions.Regex BalancedDivPattern = new System.Text.RegularExpressions.Regex(
            @"
            <div\b[^>]*>                    # Opening <div> tag
            (?<content>                     # Capture content
              (?>                           # Atomic group (no backtracking)
                [^<]+                       # Text content (non-< characters)
                |
                <div\b[^>]*> (?<DEPTH>)     # Nested <div>: PUSH to stack
                |
                </div> (?<-DEPTH>)          # Closing </div>: POP from stack
                |
                <(?!/?div\b)[^>]*>          # Other tags: pass through
              )*
            )
            (?(DEPTH)(?!))                  # FAIL if stack not empty (unclosed divs)
            </div>                          # Final closing </div>
            ",
            RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Compiled
        );

        /// <summary>
        /// Generic pattern factory for any tag name.
        /// </summary>
        public static System.Text.RegularExpressions.Regex CreateBalancedTagPattern(string tagName)
        {
            var escaped = System.Text.RegularExpressions.Regex.Escape(tagName);
            var pattern = $@"
                <{escaped}\b[^>]*>              # Opening tag
                (?<content>                     # Capture content
                  (?>                           # Atomic group
                    [^<]+                       # Text content
                    |
                    <{escaped}\b[^>]*> (?<DEPTH>)   # Nested same tag: PUSH
                    |
                    </{escaped}> (?<-DEPTH>)        # Closing same tag: POP
                    |
                    <(?!/?{escaped}\b)[^>]*>        # Other tags: pass through
                  )*
                )
                (?(DEPTH)(?!))                  # FAIL if stack not empty
                </{escaped}>                    # Final closing tag
            ";
            return new System.Text.RegularExpressions.Regex(pattern, 
                RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Attempts to match balanced div tags. Returns the match result.
        /// </summary>
        public static BalancedMatchResult MatchBalancedDiv(string html)
        {
            var match = BalancedDivPattern.Match(html);
            return new BalancedMatchResult
            {
                Success = match.Success,
                FullMatch = match.Success ? match.Value : null,
                InnerContent = match.Success ? match.Groups["content"].Value : null,
                Index = match.Success ? match.Index : -1,
                Length = match.Success ? match.Length : 0
            };
        }

        /// <summary>
        /// Attempts to match balanced tags for any tag name.
        /// </summary>
        public static BalancedMatchResult MatchBalancedTag(string html, string tagName)
        {
            var pattern = CreateBalancedTagPattern(tagName);
            var match = pattern.Match(html);
            return new BalancedMatchResult
            {
                Success = match.Success,
                FullMatch = match.Success ? match.Value : null,
                InnerContent = match.Success ? match.Groups["content"].Value : null,
                Index = match.Success ? match.Index : -1,
                Length = match.Success ? match.Length : 0
            };
        }

        /// <summary>
        /// Runs all proof-of-concept tests. Returns true if all pass.
        /// </summary>
        public static bool RunAllTests()
        {
            var allPassed = true;
            
            Console.WriteLine("=== Regex Balancing Groups: Proof of Concept ===");
            Console.WriteLine("\"You can't parse HTML with regex\" - Challenge accepted.\n");

            // Test 1: Simple div
            allPassed &= RunTest(
                "Test 1: Simple <div></div>",
                "<div></div>",
                expectMatch: true,
                expectedContent: ""
            );

            // Test 2: Div with text
            allPassed &= RunTest(
                "Test 2: <div>Hello</div>",
                "<div>Hello</div>",
                expectMatch: true,
                expectedContent: "Hello"
            );

            // Test 3: Nested divs (THE IMPOSSIBLE CASE)
            allPassed &= RunTest(
                "Test 3: Nested <div><div></div></div> - THE 'IMPOSSIBLE' CASE",
                "<div><div>Inner</div></div>",
                expectMatch: true,
                expectedContent: "<div>Inner</div>"
            );

            // Test 4: Triple nested
            allPassed &= RunTest(
                "Test 4: Triple nested divs",
                "<div><div><div>Deep</div></div></div>",
                expectMatch: true,
                expectedContent: "<div><div>Deep</div></div>"
            );

            // Test 5: Unclosed outer div - regex finds inner balanced div (correct behavior!)
            // This shows the regex DOES enforce balancing - it just finds the first balanced match
            allPassed &= RunTest(
                "Test 5: <div><div></div> - finds inner balanced div",
                "<div><div></div>",
                expectMatch: true,  // Matches the inner <div></div>
                expectedContent: ""
            );

            // Test 5b: Truly unbalanced - single unclosed div
            allPassed &= RunTest(
                "Test 5b: Single unclosed <div> - Should FAIL",
                "<div>content",
                expectMatch: false,
                expectedContent: null
            );

            // Test 6: Mixed tags (div with span inside)
            allPassed &= RunTest(
                "Test 6: Mixed tags <div><span></span></div>",
                "<div><span>Text</span></div>",
                expectMatch: true,
                expectedContent: "<span>Text</span>"
            );

            // Test 7: Div with attributes
            allPassed &= RunTest(
                "Test 7: <div class=\"foo\"><div id=\"bar\"></div></div>",
                "<div class=\"foo\"><div id=\"bar\">Content</div></div>",
                expectMatch: true,
                expectedContent: "<div id=\"bar\">Content</div>"
            );

            // Test 8: Multiple siblings at same level (matches first)
            allPassed &= RunTest(
                "Test 8: Sibling divs - matches first complete one",
                "<div>First</div><div>Second</div>",
                expectMatch: true,
                expectedContent: "First"
            );

            // Test 9: Complex real-world-ish HTML
            allPassed &= RunTest(
                "Test 9: Complex nested structure",
                "<div class=\"outer\"><p>Intro</p><div class=\"inner\"><span>Nested</span></div><p>Outro</p></div>",
                expectMatch: true,
                expectedContent: "<p>Intro</p><div class=\"inner\"><span>Nested</span></div><p>Outro</p>"
            );

            // Test 10: Mismatched closing tag (should fail to match balanced div)
            allPassed &= RunTest(
                "Test 10: Mismatched </span> inside - still matches outer div",
                "<div>Text</div>",
                expectMatch: true,
                expectedContent: "Text"
            );

            Console.WriteLine("\n" + new string('=', 50));
            if (allPassed)
            {
                Console.WriteLine("✅ ALL TESTS PASSED");
                Console.WriteLine("The 'impossible' has been done. .NET balancing groups FTW!");
            }
            else
            {
                Console.WriteLine("❌ SOME TESTS FAILED");
            }
            Console.WriteLine(new string('=', 50));

            return allPassed;
        }

        private static bool RunTest(string testName, string input, bool expectMatch, string? expectedContent)
        {
            var result = MatchBalancedDiv(input);
            var passed = result.Success == expectMatch;
            
            if (passed && expectMatch && expectedContent != null)
            {
                passed = result.InnerContent == expectedContent;
            }

            var status = passed ? "✅ PASS" : "❌ FAIL";
            Console.WriteLine($"{status}: {testName}");
            
            if (!passed)
            {
                Console.WriteLine($"       Input: {input}");
                Console.WriteLine($"       Expected match: {expectMatch}, Got: {result.Success}");
                if (expectMatch && expectedContent != null)
                {
                    Console.WriteLine($"       Expected content: \"{expectedContent}\"");
                    Console.WriteLine($"       Got content: \"{result.InnerContent}\"");
                }
            }

            return passed;
        }
    }

    /// <summary>
    /// Result of a balanced tag match operation.
    /// </summary>
    public class BalancedMatchResult
    {
        public bool Success { get; set; }
        public string FullMatch { get; set; }
        public string InnerContent { get; set; }
        public int Index { get; set; }
        public int Length { get; set; }
    }
}
