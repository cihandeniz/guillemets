using static Guillemets.Ast;

namespace Guillemets;

internal static class Tokenizer
{
    const char OPEN = '«';
    const char CLOSE = '»';

    public static List<INode> Tokenize(string template)
    {
        var nodes = new List<INode>();
        var literalStart = 0;
        var i = 0;

        while (i < template.Length)
        {
            if (template[i] == OPEN)
            {
                if (i > literalStart)
                {
                    nodes.Add(new LiteralNode(template[literalStart..i]));
                }

                var close = template.IndexOf(CLOSE, i);
                var path = template[(i + 1)..close];
                nodes.Add(new TokenNode(path));

                i = close + 1;
                literalStart = i;
            }
            else
            {
                i++;
            }
        }

        if (literalStart < template.Length)
        {
            nodes.Add(new LiteralNode(template[literalStart..]));
        }

        return nodes;
    }
}
