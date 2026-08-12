using Guillemets.Ast;
using Guillemets.Tokenization;
using Guillemets.Tokens;

using static Guillemets.Position;

namespace Guillemets.Parsing;

internal class BodyParser(TokenCursor _tokens, ParserRegistry _registry)
{
    internal static bool AtLineStart(IReadOnlyList<IRenderable> nodes) =>
        nodes.Count == 0
        || nodes[^1] is BlockNode
        || (nodes[^1] is LiteralNode { Text: var text } && text.EndsWith(NEWLINE));

    readonly Lazy<VariableParser> _lazyVariableParser = _registry.GetLazy<VariableParser>();
    readonly Lazy<BlockParser> _lazyBlockParser = _registry.GetLazy<BlockParser>();
    readonly Lazy<TextParser> _lazyTextParser = _registry.GetLazy<TextParser>();
    readonly Lazy<FilterParser> _lazyFilterParser = _registry.GetLazy<FilterParser>();

    VariableParser VariableParser => _lazyVariableParser.Value;
    BlockParser BlockParser => _lazyBlockParser.Value;
    TextParser TextParser => _lazyTextParser.Value;
    FilterParser FilterParser => _lazyFilterParser.Value;

    public List<IRenderable> ParseNodes(bool insideBlock, bool stopAtElse) =>
        ParseNodes(insideBlock, stopAtElse, out _);

    public List<IRenderable> ParseNodes(bool insideBlock, bool stopAtElse, out IReadOnlyList<FilterNode> footer)
    {
        footer = [];
        var nodes = new List<IRenderable>();
        while (!_tokens.AtEnd && !ReachedClose(insideBlock) && !ReachedElse(stopAtElse))
        {
            if (insideBlock && AtLineStart(nodes) && TryParseFooter(out footer)) { break; }

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

    bool TryParseFooter(out IReadOnlyList<FilterNode> footer)
    {
        footer = [];
        var checkpoint = _tokens.Position;
        try
        {
            // TODO filter parser should allow .TryParse, and here it will be
            // used, so cursor will be rewinded in else not catch
            //
            // a filter either ends with CloseBlockToken or CloseToken. so it can
            // try without throwing
            var pipeline = FilterParser.ParseFooterPipeline();
            if (_tokens.AtEnd || _tokens.Current is not CloseBlockToken)
            {
                _tokens.Rewind(checkpoint);

                return false;
            }

            footer = pipeline;

            return true;
        }
        // TODO no control flow via exception
        catch (TemplateParseException)
        {
            _tokens.Rewind(checkpoint);

            return false;
        }
    }

    bool ReachedClose(bool insideBlock) =>
        insideBlock && _tokens.Current is CloseBlockToken;

    bool ReachedElse(bool stopAtElse) =>
        stopAtElse && _tokens.Current is ElseToken;
}