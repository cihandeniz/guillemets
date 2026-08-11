using Guillemets.Ast;
using Guillemets.Tokenization;
using Guillemets.Tokens;

namespace Guillemets.Parsing;

internal class Parser(TokenCursor _tokens)
{
    readonly ParserRegistry _registry = new ParserRegistry()
        .Register<NodesParser>(pr => new NodesParser(_tokens, pr))
        .Register<FilterParser>(_ => new FilterParser(_tokens))
        .Register<OpenToken>(_ => new VariableParser(_tokens))
        .Register<OpenBlockToken>(pr => new BlockParser(_tokens, pr))
        .Register<ITextToken>(_ => new TextParser(_tokens))
        .Build();

    public List<INode> Parse()
    {
        var nodes = new List<INode>();
        while (!_tokens.AtEnd)
        {
            nodes.Add(_registry.Resolve(_tokens.Current).Parse(_tokens.Current));
        }

        return nodes;
    }
}