using static Guillemets.Ast;
using static Guillemets.Tokens;

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
            if (tokens[i] is OpenToken) { ParseGuillemet(); }
            else if (tokens[i] is LiteralToken) { ParseLiteral(); }
            else { ParseColon(); }
        }

        return nodes;
    }

    void ParseGuillemet()
    {
        i++;
        var segments = new List<string>();

        while (tokens[i] is not CloseToken)
        {
            if (tokens[i] is LiteralToken segment)
            {
                segments.Add(segment.Text.Trim());
            }

            i++;
        }

        nodes.Add(new TokenNode(segments));
        i++;
    }

    void ParseLiteral()
    {
        nodes.Add(new LiteralNode(((LiteralToken)tokens[i]).Text));
        i++;
    }

    void ParseColon()
    {
        nodes.Add(new LiteralNode(":"));
        i++;
    }
}
