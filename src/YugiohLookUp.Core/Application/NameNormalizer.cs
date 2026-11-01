using System.Text;

namespace YugiohLookUp.Core.Application;

public static class NameNormalizer
{
    public static string Normalize(string name)
    {
        var result = new StringBuilder();
        // WebAssembly supports canonical normalization, but not FormKC.
        foreach (var original in name.Normalize(NormalizationForm.FormC))
        {
            var character = original is >= '\uff01' and <= '\uff5e' ? (char)(original - 0xfee0) : original;
            if (char.IsWhiteSpace(character) || character is '-' or '\u2010' or '\u2011' or '\u2013' or '\u2014')
            {
                if (result.Length > 0 && result[^1] != ' ')
                    result.Append(' ');
            }
            else if (character is not ('\'' or '\u2019' or '"' or '.' or ',' or ':' or ';' or '!' or '?'))
            {
                result.Append(char.ToUpperInvariant(character));
            }
        }
        return result.ToString().Trim();
    }
}
