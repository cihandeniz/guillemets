using Guillemets.Ast;
using Guillemets.Rendering;
using Guillemets.Tokenization;
using Guillemets.Tokens;
using System.Text;

namespace Guillemets.Parsing;

internal class PropertyChainParser(TokenCursor _tokens)
    : IParser
{
    static void Flush(StringBuilder buffer, PropertyChainBuilder chain)
    {
        if (buffer.Length == 0) { return; }

        chain.Add(buffer.ToString());
        buffer.Clear();
    }

    INode IParser.Parse(IToken token) =>
        throw new InvalidOperationException($"{nameof(PropertyChainParser)} does not parse tokens directly.");

    public PropertyChain Parse(Position openPosition, bool stopAtNewline) =>
        Parse(openPosition, stopAtNewline, allowVariableDefinition: false, out _);

    public PropertyChain Parse(Position openPosition, bool stopAtNewline, out string? variableName) =>
        Parse(openPosition, stopAtNewline, allowVariableDefinition: true, out variableName);

    PropertyChain Parse(Position openPosition, bool stopAtNewline, bool allowVariableDefinition, out string? variableName)
    {
        variableName = null;
        var chain = new PropertyChainBuilder();
        var buffer = new StringBuilder();
        while (true)
        {
            if (_tokens.AtEnd) { Flush(buffer, chain); break; }

            if (_tokens.Current is NegationToken)
            {
                chain.Negate();
                _tokens.Advance();

                continue;
            }

            if (_tokens.Current is CloseToken) { Flush(buffer, chain); break; }

            if (stopAtNewline && _tokens.Current is NewlineToken)
            {
                Flush(buffer, chain);
                _tokens.Advance();

                break;
            }

            if (allowVariableDefinition && _tokens.Current is EqualsToken)
            {
                Flush(buffer, chain);
                variableName = chain.PopVariableName();
                _tokens.Advance();

                continue;
            }

            if (_tokens.Current is LiteralToken literal)
            {
                buffer.Append(literal.Text);
                _tokens.Advance();

                continue;
            }

            if (_tokens.Current is NewlineToken)
            {
                buffer.Append(' ');
                _tokens.Advance();

                continue;
            }

            Flush(buffer, chain);
            _tokens.Advance();
        }

        if (_tokens.AtEnd || (!stopAtNewline && _tokens.Current is not CloseToken))
        {
            throw new TemplateParseException(stopAtNewline ? "Unclosed block header" : "Unclosed variable", openPosition);
        }

        if (!stopAtNewline) { _tokens.Advance(); }

        return chain.Build();
    }
}