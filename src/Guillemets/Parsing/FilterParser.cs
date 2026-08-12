using Guillemets.Ast;
using Guillemets.Filters;
using Guillemets.Tokenization;
using Guillemets.Tokens;
using System.Text;

using static Guillemets.Position;
using static Guillemets.Tokens.EscapedToken;
using static Guillemets.Tokens.PipeToken;

namespace Guillemets.Parsing;

internal class FilterParser(TokenCursor _tokens, FilterRegistry _filters)
{
    static readonly string ESCAPED_NEWLINE = $"{BACKSLASH}n";
    static readonly string ESCAPED_TAB = $"{BACKSLASH}t";
    static readonly string ESCAPED_PIPE = $"{BACKSLASH}{DELIMITER}";

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
        ReadText(unescape: false).Trim();

    string ReadValue() =>
        ReadText(unescape: true);

    string ReadText(bool unescape)
    {
        var builder = new StringBuilder();
        while (_tokens.Current is LiteralToken or NewlineToken)
        {
            builder.Append(SegmentText(_tokens.Current, unescape));
            _tokens.Advance();
        }

        return builder.ToString();
    }

    static string SegmentText(IToken token, bool unescape) => token switch
    {
        NewlineToken => " ",
        EscapedToken escaped => escaped.Text,
        LiteralToken literal => unescape ? Unescape(literal.Text) : literal.Text,
        _ => throw new InvalidOperationException($"Unexpected token '{token.GetType().Name}' in filter text."),
    };

    static string Unescape(string text) =>
        text.Replace(ESCAPED_NEWLINE, NEWLINE.ToString())
            .Replace(ESCAPED_TAB, TAB.ToString())
            .Replace(ESCAPED_PIPE, DELIMITER.ToString());
}