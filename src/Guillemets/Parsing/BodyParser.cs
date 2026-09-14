using Guillemets.Ast;
using Guillemets.Tokenization;

using static Guillemets.Tokenization.TokenKind;

namespace Guillemets.Parsing;

internal class BodyParser(TokenCursor _tokens, ParserRegistry _registry)
{
    readonly Lazy<VariableParser> _lazyVariableParser = _registry.GetLazy<VariableParser>();
    readonly Lazy<BlockParser> _lazyBlockParser = _registry.GetLazy<BlockParser>();
    readonly Lazy<TextParser> _lazyTextParser = _registry.GetLazy<TextParser>();
    readonly Lazy<FilterParser> _lazyFilterParser = _registry.GetLazy<FilterParser>();

    VariableParser VariableParser => _lazyVariableParser.Value;
    BlockParser BlockParser => _lazyBlockParser.Value;
    TextParser TextParser => _lazyTextParser.Value;
    FilterParser FilterParser => _lazyFilterParser.Value;

    public List<IRenderable> Parse(bool insideBlock, bool stopAtElse) =>
        Parse(insideBlock, stopAtElse, out _);

    public List<IRenderable> Parse(bool insideBlock, bool stopAtElse, out IReadOnlyList<FilterNode> footer)
    {
        footer = [];
        var nodes = new List<IRenderable>();
        while (!_tokens.AtEnd)
        {
            if (_tokens.AtQuotedBlockLine) { _tokens.SkipQuotes(); }
            if (ReachedClose(insideBlock) || ReachedElse(stopAtElse)) { break; }
            if (insideBlock && TryParseFooterLine(out footer)) { break; }

            nodes.Add(ParseNode());
        }

        return nodes;
    }

    IRenderable ParseNode()
    {
        if (_tokens.Current.Kind is Open) { return VariableParser.Parse(_tokens.Current); }
        if (_tokens.Current.Kind is OpenBlock) { return BlockParser.Parse(_tokens.Current); }
        if (_tokens.Current.IsText) { return TextParser.Parse(_tokens.Current); }

        throw new TemplateParseException($"Unexpected token '{_tokens.Current.Kind}'", _tokens.Current.Position);
    }

    bool TryParseFooterLine(out IReadOnlyList<FilterNode> footer)
    {
        footer = [];
        if (!_tokens.CurrentAtLineStart) { return false; }

        var checkpoint = _tokens.Position;
        _tokens.SkipQuotes();
        if (!_tokens.AtEnd && TryParseFooter(_tokens.Current, out footer)) { return true; }

        _tokens.Rewind(checkpoint);

        return false;
    }

    bool TryParseFooter(Token start, out IReadOnlyList<FilterNode> footer)
    {
        footer = [];
        if (!_tokens.LineReaches(CloseBlock)) { return false; }

        var precededByBlankLine = _tokens.CurrentPrecededByBlankLine;
        var checkpoint = _tokens.Position;
        if (!FilterParser.TryParse(expectLeadingDelimiter: false, out var pipeline) ||
            _tokens.AtEnd ||
            _tokens.Current.Kind is not CloseBlock
        )
        {
            _tokens.Rewind(checkpoint);

            return false;
        }

        start.ValidateAsBlockFooter(precededByBlankLine);
        footer = pipeline;

        return true;
    }

    bool ReachedClose(bool insideBlock) =>
        insideBlock && _tokens.Current.Kind is CloseBlock && _tokens.CurrentEndsLine;

    bool ReachedElse(bool stopAtElse)
    {
        if (!stopAtElse || _tokens.Current.Kind is not Else) { return false; }
        if (!_tokens.CurrentStartsLine || !_tokens.CurrentEndsLine) { return false; }

        _tokens.Current.ValidateAsBlockElse(_tokens);

        return true;
    }
}