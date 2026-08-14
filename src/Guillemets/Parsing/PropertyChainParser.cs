using Guillemets.Ast;
using Guillemets.Tokenization;
using System.Text;

using static Guillemets.Tokenization.TokenKind;

namespace Guillemets.Parsing;

internal class PropertyChainParser(TokenCursor _tokens)
{
    static void Flush(StringBuilder buffer, PropertyChainNode.Builder chain)
    {
        if (buffer.Length == 0) { return; }

        chain.Add(buffer.ToString());
        buffer.Clear();
    }

    void ParseLeadingNavigator(PropertyChainNode.Builder chain)
    {
        while (!_tokens.AtEnd && _tokens.Current.Kind is ParentScope)
        {
            chain.Climb();
            _tokens.Advance();
        }

        if (_tokens.AtEnd || _tokens.Current.Kind is not LocalScope) { return; }

        chain.PinToCurrentScope();
        _tokens.Advance();

        if (!_tokens.AtEnd && _tokens.Current.Kind is ParentScope or LocalScope)
        {
            throw new TemplateParseException(
                "A this-scope-only navigator must be the last one before the property chain",
                _tokens.Current.Position
            );
        }
    }

    public PropertyChainNode Parse(Position openPosition, bool stopAtNewline, bool stopAtPipe = false) =>
        Parse(openPosition, stopAtNewline, stopAtPipe, allowVariableDefinition: false, out _);

    public PropertyChainNode Parse(Position openPosition, bool stopAtNewline, out string? variableName) =>
        Parse(openPosition, stopAtNewline, stopAtPipe: false, allowVariableDefinition: true, out variableName);

    PropertyChainNode Parse(Position openPosition, bool stopAtNewline, bool stopAtPipe, bool allowVariableDefinition, out string? variableName)
    {
        variableName = null;
        var chain = new PropertyChainNode.Builder();
        var buffer = new StringBuilder();
        ParseLeadingNavigator(chain);
        while (true)
        {
            if (_tokens.AtEnd) { Flush(buffer, chain); break; }

            if (_tokens.Current.Kind is Negation)
            {
                chain.Negate(_tokens.Current.Position);
                _tokens.Advance();

                continue;
            }

            if (_tokens.Current.Kind is Close)
            {
                Flush(buffer, chain);

                break;
            }

            if (stopAtPipe && _tokens.Current.Kind is Pipe)
            {
                Flush(buffer, chain); break;
            }

            if (stopAtNewline && _tokens.Current.Kind is Newline)
            {
                Flush(buffer, chain);
                _tokens.Advance();

                break;
            }

            if (allowVariableDefinition && _tokens.Current.Kind is Assign)
            {
                Flush(buffer, chain);
                variableName = chain.PopVariableName();
                _tokens.Advance();

                continue;
            }

            if (_tokens.Current.Kind is Literal or Escaped)
            {
                buffer.Append(_tokens.Current.Text);
                _tokens.Advance();

                continue;
            }

            if (_tokens.Current.Kind is Newline)
            {
                buffer.Append(' ');
                _tokens.Advance();

                continue;
            }

            Flush(buffer, chain);
            _tokens.Advance();
        }

        if (_tokens.AtEnd || (stopAtPipe && _tokens.Current.Kind is not (Close or Pipe)))
        {
            throw new TemplateParseException("Unclosed variable", openPosition);
        }

        if (stopAtPipe)
        {
            return chain.Build(openPosition);
        }

        if (!stopAtNewline && _tokens.Current.Kind is not Close)
        {
            throw new TemplateParseException(stopAtNewline ? "Unclosed block header" : "Unclosed variable", openPosition);
        }

        if (!stopAtNewline)
        {
            _tokens.Advance();
        }

        return chain.Build(openPosition);
    }
}