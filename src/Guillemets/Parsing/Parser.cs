using Guillemets.Ast;
using Guillemets.Tokenization;
using Guillemets.Tokens;

namespace Guillemets.Parsing;

internal class Parser(TokenCursor _tokens)
{
    readonly IParser _parser = new ParserBuilder(_tokens)
        .Register<OpenToken>(_ => new VariableParser(_tokens))
        .Register<OpenBlockToken>(np => new BlockParser(_tokens, np))
        .Register<ITextToken>(_ => new TextParser(_tokens))
        .Build();

    public List<INode> Parse()
    {
        var nodes = new List<INode>();
        while (!_tokens.AtEnd)
        {
            nodes.Add(_parser.Parse(_tokens.Current));
        }

        return nodes;
    }
}