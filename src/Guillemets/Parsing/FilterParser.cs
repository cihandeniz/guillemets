using Guillemets.Ast;
using Guillemets.Filters;
using Guillemets.Tokenization;
using Guillemets.Tokens;
using System.Text;

namespace Guillemets.Parsing;

internal class FilterParser(TokenCursor _tokens, FilterRegistry _filters)
{
    public IReadOnlyList<FilterNode> ParsePipeline()
    {
        var stages = new List<FilterNode>();
        while (_tokens.Current is PipeToken)
        {
            _tokens.Advance();
            stages.Add(ParseStage());
        }

        return stages;
    }

    FilterNode ParseStage()
    {
        var position = _tokens.Current.Position;
        var name = ReadName();
        if (!_filters.TryGet(name, out var filter))
        {
            throw new TemplateParseException($"Unknown filter '{name}'", position);
        }

        if (_tokens.Current is not ColonToken) { return new FilterNode(filter, null); }
        _tokens.Advance();

        return new FilterNode(filter, ReadValue());
    }

    string ReadName() =>
        ReadText().Trim();

    string ReadValue() =>
        ReadText();

    string ReadText()
    {
        var builder = new StringBuilder();
        while (_tokens.Current is LiteralToken or NewlineToken)
        {
            builder.Append(_tokens.Current is NewlineToken ? " " : ((LiteralToken)_tokens.Current).Text);
            _tokens.Advance();
        }

        return builder.ToString();
    }
}