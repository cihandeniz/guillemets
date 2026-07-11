using Guillemets.Ast;
using Guillemets.Tokens;

using static Guillemets.Tokenizer;

namespace Guillemets;

internal sealed class Parser
{
    readonly List<INode> nodes = [];
    readonly List<IToken> tokens;
    int i = 0;

    public static List<INode> Parse(List<IToken> input) =>
        new Parser(input).Parse();

    Parser(List<IToken> input)
    {
        tokens = input;
    }

    List<INode> Parse()
    {
        while (i < tokens.Count)
        {
            if (tokens[i] is OpenToken open) { ParseGuillemet(open); }
            else if (tokens[i] is LiteralToken literal) { ParseLiteral(literal); }
            else { ParseColon(); }
        }

        return nodes;
    }

    void ParseGuillemet(OpenToken open)
    {
        i++;

        var segments = new List<string>();
        while (true)
        {
            if (i >= tokens.Count) { throw new TemplateParseException($"Unclosed {OPEN}{CLOSE}", open.Position); }
            if (tokens[i] is CloseToken) { break; }

            if (tokens[i] is LiteralToken segment)
            {
                segments.Add(NormalizeWhitespace(segment.Text));
            }

            i++;
        }

        nodes.Add(new TokenNode(segments));
        i++;
    }

    void ParseLiteral(LiteralToken literal)
    {
        nodes.Add(new LiteralNode(literal.Text));
        i++;
    }

    void ParseColon()
    {
        nodes.Add(new LiteralNode($"{COLON}"));
        i++;
    }

    static string NormalizeWhitespace(string text) =>
        string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
