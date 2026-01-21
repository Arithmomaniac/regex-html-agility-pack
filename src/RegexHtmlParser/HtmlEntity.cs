// Description: Regex Html Parser - A regex-powered HTML parser using .NET balancing groups.

using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace RegexHtmlParser;

/// <summary>
/// Provides methods for encoding and decoding HTML entities.
/// </summary>
public static class HtmlEntity
{
    private static readonly Dictionary<string, char> EntityToChar = new()
    {
        // Basic HTML entities
        {"amp", '&'},
        {"lt", '<'},
        {"gt", '>'},
        {"quot", '"'},
        {"apos", '\''},
        {"nbsp", '\u00A0'},
        
        // Common entities
        {"copy", '©'},
        {"reg", '®'},
        {"trade", '™'},
        {"mdash", '\u2014'},
        {"ndash", '\u2013'},
        {"lsquo", '\u2018'},
        {"rsquo", '\u2019'},
        {"ldquo", '\u201C'},
        {"rdquo", '\u201D'},
        {"bull", '•'},
        {"hellip", '…'},
        {"euro", '€'},
        {"pound", '£'},
        {"yen", '¥'},
        {"cent", '¢'},
        {"deg", '°'},
        {"plusmn", '±'},
        {"times", '×'},
        {"divide", '÷'},
        {"frac12", '½'},
        {"frac14", '¼'},
        {"frac34", '¾'},
        {"para", '¶'},
        {"sect", '§'},
        {"dagger", '†'},
        {"Dagger", '‡'},
        
        // Latin extended entities
        {"Agrave", 'À'},
        {"Aacute", 'Á'},
        {"Acirc", 'Â'},
        {"Atilde", 'Ã'},
        {"Auml", 'Ä'},
        {"Aring", 'Å'},
        {"AElig", 'Æ'},
        {"Ccedil", 'Ç'},
        {"Egrave", 'È'},
        {"Eacute", 'É'},
        {"Ecirc", 'Ê'},
        {"Euml", 'Ë'},
        {"Igrave", 'Ì'},
        {"Iacute", 'Í'},
        {"Icirc", 'Î'},
        {"Iuml", 'Ï'},
        {"Ntilde", 'Ñ'},
        {"Ograve", 'Ò'},
        {"Oacute", 'Ó'},
        {"Ocirc", 'Ô'},
        {"Otilde", 'Õ'},
        {"Ouml", 'Ö'},
        {"Oslash", 'Ø'},
        {"Ugrave", 'Ù'},
        {"Uacute", 'Ú'},
        {"Ucirc", 'Û'},
        {"Uuml", 'Ü'},
        {"Yacute", 'Ý'},
        {"szlig", 'ß'},
        {"agrave", 'à'},
        {"aacute", 'á'},
        {"acirc", 'â'},
        {"atilde", 'ã'},
        {"auml", 'ä'},
        {"aring", 'å'},
        {"aelig", 'æ'},
        {"ccedil", 'ç'},
        {"egrave", 'è'},
        {"eacute", 'é'},
        {"ecirc", 'ê'},
        {"euml", 'ë'},
        {"igrave", 'ì'},
        {"iacute", 'í'},
        {"icirc", 'î'},
        {"iuml", 'ï'},
        {"ntilde", 'ñ'},
        {"ograve", 'ò'},
        {"oacute", 'ó'},
        {"ocirc", 'ô'},
        {"otilde", 'õ'},
        {"ouml", 'ö'},
        {"oslash", 'ø'},
        {"ugrave", 'ù'},
        {"uacute", 'ú'},
        {"ucirc", 'û'},
        {"uuml", 'ü'},
        {"yacute", 'ý'},
        {"yuml", 'ÿ'},
    };

    private static readonly Dictionary<char, string> CharToEntity = new()
    {
        {'&', "amp"},
        {'<', "lt"},
        {'>', "gt"},
        {'"', "quot"},
    };

    private static readonly Regex EntityPattern = new(
        @"&(?:(?<named>[a-zA-Z]+)|#(?:(?<decimal>\d+)|x(?<hex>[0-9a-fA-F]+)));",
        RegexOptions.Compiled);

    /// <summary>
    /// Decodes HTML entities in a string.
    /// </summary>
    /// <param name="text">The text containing HTML entities.</param>
    /// <returns>The decoded text.</returns>
    public static string DeEntitize(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return EntityPattern.Replace(text, match =>
        {
            // Named entity
            if (match.Groups["named"].Success)
            {
                var name = match.Groups["named"].Value;
                if (EntityToChar.TryGetValue(name, out var c))
                    return c.ToString();
                return match.Value; // Unknown entity, keep as-is
            }

            // Decimal numeric reference
            if (match.Groups["decimal"].Success)
            {
                if (int.TryParse(match.Groups["decimal"].Value, out var code) && code <= char.MaxValue)
                    return ((char)code).ToString();
                return match.Value;
            }

            // Hexadecimal numeric reference
            if (match.Groups["hex"].Success)
            {
                if (int.TryParse(match.Groups["hex"].Value, NumberStyles.HexNumber, null, out var code) && code <= char.MaxValue)
                    return ((char)code).ToString();
                return match.Value;
            }

            return match.Value;
        });
    }

    /// <summary>
    /// Encodes special characters as HTML entities.
    /// </summary>
    /// <param name="text">The text to encode.</param>
    /// <returns>The encoded text.</returns>
    public static string Entitize(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (CharToEntity.TryGetValue(c, out var entity))
            {
                sb.Append('&').Append(entity).Append(';');
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Encodes special characters as HTML entities with optional full encoding.
    /// </summary>
    /// <param name="text">The text to encode.</param>
    /// <param name="useNames">Use named entities where possible.</param>
    /// <param name="entitizeQuotAmpAndLtGt">Entitize quote, ampersand, less-than, and greater-than.</param>
    /// <returns>The encoded text.</returns>
    public static string Entitize(string text, bool useNames, bool entitizeQuotAmpAndLtGt = true)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (entitizeQuotAmpAndLtGt && CharToEntity.TryGetValue(c, out var entity))
            {
                if (useNames)
                {
                    sb.Append('&').Append(entity).Append(';');
                }
                else
                {
                    sb.Append("&#").Append((int)c).Append(';');
                }
            }
            else if (c > 127)
            {
                sb.Append("&#").Append((int)c).Append(';');
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Gets the value of a named entity.
    /// </summary>
    /// <param name="name">The entity name (without & and ;).</param>
    /// <returns>The character value, or 0 if not found.</returns>
    public static int EntityValue(string name)
    {
        if (EntityToChar.TryGetValue(name, out var c))
            return c;
        return 0;
    }

    /// <summary>
    /// Gets the name of an entity for a character.
    /// </summary>
    /// <param name="c">The character.</param>
    /// <returns>The entity name (without & and ;), or null if not found.</returns>
    public static string? EntityName(char c)
    {
        if (CharToEntity.TryGetValue(c, out var name))
            return name;
        return null;
    }
}
