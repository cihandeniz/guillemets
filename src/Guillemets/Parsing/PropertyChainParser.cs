using Guillemets.Ast;
using Guillemets.Rendering;
using Guillemets.Tokenization;
using Guillemets.Tokens;

using static Guillemets.Position;

namespace Guillemets.Parsing;

internal class PropertyChainParser(TokenCursor _tokens)
    : IParser
{
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
        while (true)
        {
            if (_tokens.AtEnd) { break; }

            if (_tokens.Current is NegationToken)
            {
                chain.Negate();
                _tokens.Advance();

                continue;
            }

            if (_tokens.Current is CloseToken) { break; }

            if (allowVariableDefinition && _tokens.Current is EqualsToken)
            {
                variableName = chain.PopVariableName();
                _tokens.Advance();

                continue;
            }

            if (_tokens.Current is not LiteralToken literal)
            {
                _tokens.Advance();

                continue;
            }

            var newlineIndex = stopAtNewline ? literal.Text.IndexOf(NEWLINE) : -1;
            if (newlineIndex < 0)
            {
                chain.Add(literal.Text);
                _tokens.Advance();

                continue;
            }

            chain.Add(literal.Text[..newlineIndex]);
            _tokens.ReplaceCurrent(literal with { Text = literal.Text[(newlineIndex + 1)..] });

            break;
        }

        if (_tokens.AtEnd || (!stopAtNewline && _tokens.Current is not CloseToken))
        {
            throw new TemplateParseException(stopAtNewline ? "Unclosed block header" : "Unclosed variable", openPosition);
        }

        if (!stopAtNewline) { _tokens.Advance(); }

        return chain.Build();
    }
}