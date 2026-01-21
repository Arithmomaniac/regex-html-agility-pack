// Description: Tests for the balancing group regex patterns used in RegexHtmlParser.

using Xunit;
using RegexHtmlParser.Patterns;

namespace RegexHtmlParser.Tests;

/// <summary>
/// Tests specifically for the regex patterns with .NET balancing groups.
/// </summary>
public class BalancingGroupTests
{
    [Fact]
    public void TokenizerPattern_MatchesOpeningTag()
    {
        var html = "<div>";
        var matches = TokenizerPatterns.TokenizerPattern.Matches(html);
        
        Assert.Single(matches);
        Assert.True(matches[0].Groups["opening"].Success);
    }

    [Fact]
    public void TokenizerPattern_MatchesClosingTag()
    {
        var html = "</div>";
        var matches = TokenizerPatterns.TokenizerPattern.Matches(html);
        
        Assert.Single(matches);
        Assert.True(matches[0].Groups["closing"].Success);
    }

    [Fact]
    public void TokenizerPattern_MatchesSelfClosingTag()
    {
        var html = "<br/>";
        var matches = TokenizerPatterns.TokenizerPattern.Matches(html);
        
        Assert.Single(matches);
        Assert.True(matches[0].Groups["selfClosing"].Success);
    }

    [Fact]
    public void TokenizerPattern_MatchesComment()
    {
        var html = "<!-- comment -->";
        var matches = TokenizerPatterns.TokenizerPattern.Matches(html);
        
        Assert.Single(matches);
        Assert.True(matches[0].Groups["comment"].Success);
    }

    [Fact]
    public void TokenizerPattern_MatchesDoctype()
    {
        var html = "<!DOCTYPE html>";
        var matches = TokenizerPatterns.TokenizerPattern.Matches(html);
        
        Assert.Single(matches);
        Assert.True(matches[0].Groups["doctype"].Success);
    }

    [Fact]
    public void TokenizerPattern_MatchesCData()
    {
        var html = "<![CDATA[content]]>";
        var matches = TokenizerPatterns.TokenizerPattern.Matches(html);
        
        Assert.Single(matches);
        Assert.True(matches[0].Groups["cdata"].Success);
    }

    [Fact]
    public void TokenizerPattern_MatchesText()
    {
        var html = "plain text";
        var matches = TokenizerPatterns.TokenizerPattern.Matches(html);
        
        Assert.Single(matches);
        Assert.True(matches[0].Groups["text"].Success);
    }

    [Fact]
    public void TokenizerPattern_MatchesComplexHtml()
    {
        var html = "<!DOCTYPE html><html><head></head><body>text</body></html>";
        var matches = TokenizerPatterns.TokenizerPattern.Matches(html);
        
        // Should match: DOCTYPE, <html>, <head>, </head>, <body>, text, </body>, </html>
        Assert.Equal(8, matches.Count);
    }

    [Fact]
    public void AttributePattern_MatchesDoubleQuotedAttribute()
    {
        var attrs = "id=\"myId\"";
        var matches = TokenizerPatterns.AttributePattern.Matches(attrs);
        
        Assert.Single(matches);
        Assert.Equal("id", matches[0].Groups["name"].Value);
        Assert.Equal("myId", matches[0].Groups["dqValue"].Value);
    }

    [Fact]
    public void AttributePattern_MatchesSingleQuotedAttribute()
    {
        var attrs = "id='myId'";
        var matches = TokenizerPatterns.AttributePattern.Matches(attrs);
        
        Assert.Single(matches);
        Assert.Equal("id", matches[0].Groups["name"].Value);
        Assert.Equal("myId", matches[0].Groups["sqValue"].Value);
    }

    [Fact]
    public void AttributePattern_MatchesUnquotedAttribute()
    {
        var attrs = "id=myId";
        var matches = TokenizerPatterns.AttributePattern.Matches(attrs);
        
        Assert.Single(matches);
        Assert.Equal("id", matches[0].Groups["name"].Value);
        Assert.Equal("myId", matches[0].Groups["uqValue"].Value);
    }

    [Fact]
    public void AttributePattern_MatchesBooleanAttribute()
    {
        var attrs = "disabled";
        var matches = TokenizerPatterns.AttributePattern.Matches(attrs);
        
        Assert.Single(matches);
        Assert.Equal("disabled", matches[0].Groups["name"].Value);
    }

    [Fact]
    public void AttributePattern_MatchesMultipleAttributes()
    {
        var attrs = "id=\"myId\" class=\"myClass\" disabled";
        var matches = TokenizerPatterns.AttributePattern.Matches(attrs);
        
        Assert.Equal(3, matches.Count);
    }

    [Fact]
    public void CommentPattern_MatchesSimpleComment()
    {
        var html = "<!-- This is a comment -->";
        var match = TokenizerPatterns.CommentPattern.Match(html);
        
        Assert.True(match.Success);
        Assert.Equal(" This is a comment ", match.Groups["content"].Value);
    }

    [Fact]
    public void CommentPattern_MatchesMultilineComment()
    {
        var html = "<!--\nLine 1\nLine 2\n-->";
        var match = TokenizerPatterns.CommentPattern.Match(html);
        
        Assert.True(match.Success);
        Assert.Contains("Line 1", match.Groups["content"].Value);
        Assert.Contains("Line 2", match.Groups["content"].Value);
    }

    [Fact]
    public void BalancedTagPattern_MatchesSimpleNesting()
    {
        var html = "<div><div>inner</div></div>";
        var pattern = TokenizerPatterns.CreateBalancedTagPattern("div");
        var match = pattern.Match(html);
        
        Assert.True(match.Success);
        Assert.Contains("<div>inner</div>", match.Groups["content"].Value);
    }

    [Fact]
    public void BalancedTagPattern_MatchesDeepNesting()
    {
        var html = "<div><div><div>deep</div></div></div>";
        var pattern = TokenizerPatterns.CreateBalancedTagPattern("div");
        var match = pattern.Match(html);
        
        Assert.True(match.Success);
    }

    [Fact]
    public void BalancedTagPattern_MatchesMixedContent()
    {
        var html = "<div>text<div>inner</div>more text</div>";
        var pattern = TokenizerPatterns.CreateBalancedTagPattern("div");
        var match = pattern.Match(html);
        
        Assert.True(match.Success);
        var content = match.Groups["content"].Value;
        Assert.Contains("text", content);
        Assert.Contains("<div>inner</div>", content);
        Assert.Contains("more text", content);
    }

    [Fact]
    public void VoidElements_ContainsAllVoidElements()
    {
        var voidElements = new[] { "br", "hr", "img", "input", "meta", "link", "area", "base", "col", "embed", "param", "source", "track", "wbr" };
        
        foreach (var element in voidElements)
        {
            Assert.True(TokenizerPatterns.VoidElements.Contains(element), $"{element} should be in VoidElements");
        }
    }

    [Fact]
    public void RawTextElements_ContainsScriptAndStyle()
    {
        Assert.True(TokenizerPatterns.RawTextElements.Contains("script"));
        Assert.True(TokenizerPatterns.RawTextElements.Contains("style"));
        Assert.True(TokenizerPatterns.RawTextElements.Contains("textarea"));
    }

    [Fact]
    public void CreateRawTextContentPattern_MatchesScriptContent()
    {
        var html = "if (x < 5) { alert('hello'); }</script>";
        var pattern = TokenizerPatterns.CreateRawTextContentPattern("script");
        var match = pattern.Match(html);
        
        Assert.True(match.Success);
        Assert.Equal("if (x < 5) { alert('hello'); }", match.Groups["content"].Value);
    }

    [Fact]
    public void CreateRawTextContentPattern_MatchesStyleContent()
    {
        var html = ".class { color: red; }</style>";
        var pattern = TokenizerPatterns.CreateRawTextContentPattern("style");
        var match = pattern.Match(html);
        
        Assert.True(match.Success);
        Assert.Equal(".class { color: red; }", match.Groups["content"].Value);
    }

    [Fact]
    public void OpeningTagPattern_MatchesTagWithAttributes()
    {
        var html = "<div id=\"test\" class=\"myClass\">";
        var match = TokenizerPatterns.OpeningTagPattern.Match(html);
        
        Assert.True(match.Success);
        Assert.Equal("div", match.Groups["tagName"].Value);
        Assert.Contains("id=\"test\"", match.Groups["attrs"].Value);
    }

    [Fact]
    public void ClosingTagPattern_MatchesClosingTag()
    {
        var html = "</div>";
        var match = TokenizerPatterns.ClosingTagPattern.Match(html);
        
        Assert.True(match.Success);
        Assert.Equal("div", match.Groups["tagName"].Value);
    }

    [Fact]
    public void ClosingTagPattern_MatchesClosingTagWithWhitespace()
    {
        var html = "</div  >";
        var match = TokenizerPatterns.ClosingTagPattern.Match(html);
        
        Assert.True(match.Success);
        Assert.Equal("div", match.Groups["tagName"].Value);
    }

    [Fact]
    public void SelfClosingTagPattern_MatchesSelfClosingTag()
    {
        var html = "<br />";
        var match = TokenizerPatterns.SelfClosingTagPattern.Match(html);
        
        Assert.True(match.Success);
        Assert.Equal("br", match.Groups["tagName"].Value);
    }

    [Fact]
    public void SelfClosingTagPattern_MatchesSelfClosingTagWithAttributes()
    {
        var html = "<img src=\"test.jpg\" alt=\"test\" />";
        var match = TokenizerPatterns.SelfClosingTagPattern.Match(html);
        
        Assert.True(match.Success);
        Assert.Equal("img", match.Groups["tagName"].Value);
        Assert.Contains("src=\"test.jpg\"", match.Groups["attrs"].Value);
    }

    [Theory]
    [InlineData("<div>")]
    [InlineData("<DIV>")]
    [InlineData("<Div>")]
    public void TokenizerPattern_IsCaseInsensitive(string html)
    {
        var matches = TokenizerPatterns.TokenizerPattern.Matches(html);
        
        Assert.Single(matches);
        Assert.True(matches[0].Groups["opening"].Success);
    }

    [Theory]
    [InlineData("<div id=\"a\">", "a")]
    [InlineData("<div id='b'>", "b")]
    [InlineData("<div id=c>", "c")]
    public void AttributePattern_MatchesDifferentQuoteStyles(string html, string expectedId)
    {
        var tagMatch = TokenizerPatterns.OpeningTagPattern.Match(html);
        Assert.True(tagMatch.Success);
        
        var attrs = tagMatch.Groups["attrs"].Value;
        var attrMatches = TokenizerPatterns.AttributePattern.Matches(attrs);
        
        Assert.Single(attrMatches);
        
        var value = attrMatches[0].Groups["dqValue"].Success ? attrMatches[0].Groups["dqValue"].Value :
                   attrMatches[0].Groups["sqValue"].Success ? attrMatches[0].Groups["sqValue"].Value :
                   attrMatches[0].Groups["uqValue"].Value;
        
        Assert.Equal(expectedId, value);
    }

    [Fact]
    public void TokenizerPattern_HandlesMultilineHtml()
    {
        var html = @"<div>
    <span>text</span>
</div>";
        var matches = TokenizerPatterns.TokenizerPattern.Matches(html);
        
        // Should match: <div>, newline+spaces, <span>, text, </span>, newline, </div>
        Assert.True(matches.Count >= 5);
    }
}
