using Guillemets.Ast;
using Guillemets.Filters;
using Guillemets.Tokenization;
using System.Text;

using static Guillemets.Position;
using static Guillemets.Tokenization.Symbols;
using static Guillemets.Tokenization.TokenKind;

namespace Guillemets.Parsing;

internal class FilterParser(TokenCursor _tokens, FilterRegistry _filters)
{
    internal record StageResult(FilterNode? Stage, string Name, Position Position);
    internal record PipelineResult(IReadOnlyList<FilterNode> Pipeline, string Name, Position Position);

    static readonly string ESCAPED_NEWLINE = $"{BACKSLASH}n";
    static readonly string ESCAPED_TAB = $"{BACKSLASH}t";
    static readonly string ESCAPED_SLASH = $"{BACKSLASH}{SLASH}";
    static readonly Position NO_ERROR_POSITION = new(0, 0);

    static string SegmentText(Token token, bool unescape) => token.Kind switch
    {
        Newline => " ",
        Escaped or Quote or Pipe => token.Text,
        Literal => unescape ? Unescape(token.Text) : token.Text,
        Colon or BareColon or LocalScope or ParentScope or Else or Negation or Assign =>
            token.Text,
        Open or OpenBlock or Close or CloseBlock or FilterDelimiter =>
            throw new InvalidOperationException($"Unexpected token '{token.Kind}' in filter text."),
        _ => throw new ArgumentOutOfRangeException(nameof(token), token.Kind, "Unrecognized token kind."),
    };

    static string Unescape(string text) =>
        text.Replace(ESCAPED_NEWLINE, NEWLINE.ToString())
            .Replace(ESCAPED_TAB, TAB.ToString())
            .Replace(ESCAPED_SLASH, SLASH.ToString());

    public IReadOnlyList<FilterNode> Parse(bool expectLeadingDelimiter)
    {
        if (!TryParsePipeline(expectLeadingDelimiter, stopAtNewline: false, out var result))
        {
            throw new TemplateParseException($"Unknown filter '{result.Name}'", result.Position);
        }

        return result.Pipeline;
    }

    public bool TryParse(bool expectLeadingDelimiter, out IReadOnlyList<FilterNode> pipeline)
    {
        var success = TryParsePipeline(expectLeadingDelimiter, stopAtNewline: true, out var result);
        pipeline = result.Pipeline;

        return success;
    }

    bool TryParsePipeline(bool expectLeadingDelimiter, bool stopAtNewline, out PipelineResult result)
    {
        var stages = new List<FilterNode>();
        if (!expectLeadingDelimiter && !TryParseAndAddStage(stopAtNewline, stages, out result)) { return false; }

        while (!_tokens.AtEnd && _tokens.Current.Kind is FilterDelimiter)
        {
            _tokens.Advance();
            if (_tokens.AtEnd) { break; }
            if (!TryParseAndAddStage(stopAtNewline, stages, out result)) { return false; }
        }

        result = new(stages, string.Empty, NO_ERROR_POSITION);

        return true;
    }

    bool TryParseAndAddStage(bool stopAtNewline, List<FilterNode> stages, out PipelineResult result)
    {
        if (!TryParseStage(stopAtNewline, out var stage))
        {
            result = new(stages, stage.Name, stage.Position);

            return false;
        }

        stages.Add(stage.Stage ?? throw new InvalidOperationException("A successful stage has a filter node."));
        result = new(stages, string.Empty, NO_ERROR_POSITION);

        return true;
    }

    bool TryParseStage(bool stopAtNewline, out StageResult result)
    {
        var position = _tokens.Current.Position;
        var name = ReadName(stopAtNewline).Trim();
        if (!_filters.TryGet(name, out var filter))
        {
            result = new(null, name, position);

            return false;
        }

        if (!_tokens.AtEnd && _tokens.Current.Kind is BareColon)
        {
            throw new TemplateParseException("Expected a space after ':'", _tokens.Current.Position.NextColumn());
        }

        if (_tokens.AtEnd || _tokens.Current.Kind is not Colon)
        {
            result = new(new(filter, null, position), name, position);

            return true;
        }

        _tokens.Advance();
        result = new(new(filter, ReadValue(stopAtNewline), position), name, position);

        return true;
    }

    string ReadName(bool stopAtNewline) =>
        ReadText(unescape: false, stopAtNewline, inValue: false);

    string ReadValue(bool stopAtNewline) =>
        ReadText(unescape: true, stopAtNewline, inValue: true);

    string ReadText(bool unescape, bool stopAtNewline, bool inValue)
    {
        var builder = new StringBuilder();
        while (!_tokens.AtEnd && ContinuesText(stopAtNewline, inValue))
        {
            if (_tokens.TrySkipQuoteMarker()) { continue; }

            builder.Append(SegmentText(_tokens.Current, unescape));
            _tokens.Advance();
        }

        return builder.ToString();
    }

    bool ContinuesText(bool stopAtNewline, bool inValue) => _tokens.Current.Kind switch
    {
        Newline => !stopAtNewline,
        Literal or Escaped or Quote or Pipe => true,
        Open or OpenBlock or Close or CloseBlock or FilterDelimiter => false,
        Colon or BareColon or LocalScope or ParentScope or Else or Negation or Assign => inValue,
        _ => throw new ArgumentOutOfRangeException(nameof(inValue), _tokens.Current.Kind, "Unrecognized token kind."),
    };
}