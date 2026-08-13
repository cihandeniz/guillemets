using Guillemets.Ast;
using Guillemets.Filters;
using Guillemets.Tokenization;
using Guillemets.Tokens;
using System.Diagnostics.CodeAnalysis;
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
    static readonly Position NO_ERROR_POSITION = new(0, 0);

    public IReadOnlyList<FilterNode> Parse(bool expectLeadingPipe)
    {
        if (!TryParse(expectLeadingPipe, out var pipeline, out var name, out var position))
        {
            throw new TemplateParseException($"Unknown filter '{name}'", position);
        }

        return pipeline;
    }

    public bool TryParse(bool expectLeadingPipe, out IReadOnlyList<FilterNode> pipeline) =>
        TryParse(expectLeadingPipe, out pipeline, out _, out _);

    bool TryParse(bool expectLeadingPipe, out IReadOnlyList<FilterNode> pipeline, out string name, out Position position)
    {
        var stages = new List<FilterNode>();
        pipeline = stages;
        name = string.Empty;
        position = NO_ERROR_POSITION;

        if (!expectLeadingPipe)
        {
            if (!TryParseStage(out var first, out name, out position)) { return false; }

            stages.Add(first);
        }

        while (!_tokens.AtEnd && _tokens.Current is PipeToken)
        {
            _tokens.Advance();
            if (_tokens.AtEnd) { break; }
            if (!TryParseStage(out var stage, out name, out position)) { return false; }

            stages.Add(stage);
        }

        return true;
    }

    bool TryParseStage([NotNullWhen(true)] out FilterNode? stage, out string name, out Position position)
    {
        position = _tokens.Current.Position;
        name = ReadName();
        if (!_filters.TryGet(name, out var filter))
        {
            stage = null;

            return false;
        }

        if (_tokens.AtEnd || _tokens.Current is not ColonToken)
        {
            stage = new FilterNode(filter, null);

            return true;
        }
        _tokens.Advance();

        stage = new FilterNode(filter, ReadValue());

        return true;
    }

    string ReadName() =>
        ReadText(unescape: false).Trim();

    string ReadValue() =>
        ReadText(unescape: true);

    string ReadText(bool unescape)
    {
        var builder = new StringBuilder();
        while (!_tokens.AtEnd && _tokens.Current is LiteralToken or NewlineToken)
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