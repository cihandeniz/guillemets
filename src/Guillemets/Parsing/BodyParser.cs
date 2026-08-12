using Guillemets.Ast;
using Guillemets.Tokenization;
using Guillemets.Tokens;

namespace Guillemets.Parsing;

internal class BodyParser(TokenCursor _tokens, ParserRegistry _registry)
{
    readonly Lazy<VariableParser> _lazyVariableParser = _registry.GetLazy<VariableParser>();
    readonly Lazy<BlockParser> _lazyBlockParser = _registry.GetLazy<BlockParser>();
    readonly Lazy<TextParser> _lazyTextParser = _registry.GetLazy<TextParser>();

    VariableParser VariableParser => _lazyVariableParser.Value;
    BlockParser BlockParser => _lazyBlockParser.Value;
    TextParser TextParser => _lazyTextParser.Value;

    public List<IRenderable> ParseNodes(bool insideBlock, bool stopAtElse)
    {
        var nodes = new List<IRenderable>();
        while (!_tokens.AtEnd && !ReachedClose(insideBlock) && !ReachedElse(stopAtElse))
        {
            var node = _tokens.Current switch
            {
                OpenToken => VariableParser.Parse(_tokens.Current),
                OpenBlockToken => BlockParser.Parse(_tokens.Current),
                ITextToken => TextParser.Parse(_tokens.Current),
                _ => throw new TemplateParseException($"Unexpected token '{_tokens.Current.GetType().Name}'", _tokens.Current.Position),
            };
            nodes.Add(node);
        }

        return nodes;
    }

    bool ReachedClose(bool insideBlock) =>
        insideBlock && _tokens.Current is CloseBlockToken;

    bool ReachedElse(bool stopAtElse) =>
        stopAtElse && _tokens.Current is ElseToken;
}