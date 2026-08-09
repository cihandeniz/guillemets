using Guillemets.Ast;
using Guillemets.Tokenization;
using Guillemets.Tokens;

using static Guillemets.Position;
using static Guillemets.Tokenization.Symbols;

namespace Guillemets;

internal class Parser(TokenCursor _tokens)
{
    public List<INode> Parse() =>
        ParseNodes(insideBlock: false);

    List<INode> ParseNodes(bool insideBlock, bool stopAtElse = false)
    {
        var nodes = new List<INode>();
        while (!_tokens.AtEnd && !ReachedClose(insideBlock) && !ReachedElse(stopAtElse))
        {
            nodes.Add(ParseNext());
        }

        return nodes;
    }

    bool ReachedClose(bool insideBlock) =>
        insideBlock && _tokens.Current is CloseBlockToken;

    bool ReachedElse(bool stopAtElse) =>
        stopAtElse && _tokens.Current is ElseToken;

    INode ParseNext()
    {
        if (_tokens.Current is OpenBlockToken openBlock)
        {
            return ParseBlock(openBlock);
        }

        if (_tokens.Current is OpenToken open)
        {
            return ParseVariable(open);
        }

        if (_tokens.Current is ITextToken textToken)
        {
            _tokens.Advance();

            return new LiteralNode(textToken.Text);
        }

        throw new TemplateParseException($"Unexpected token '{_tokens.Current.GetType().Name}'", _tokens.Current.Position);
    }

    INode ParseVariable(OpenToken open)
    {
        _tokens.Advance();

        var properties = new List<string>();
        while (true)
        {
            if (_tokens.AtEnd) { throw new TemplateParseException($"Unclosed {OPEN}{CLOSE}", open.Position); }
            if (_tokens.Current is CloseToken) { break; }

            if (_tokens.Current is LiteralToken literal) { properties.Add(NormalizeWhitespace(literal.Text)); }
            _tokens.Advance();
        }

        _tokens.Advance();
        return new TokenNode(new PropertyChain(properties));
    }

    INode ParseBlock(OpenBlockToken open)
    {
        _tokens.Advance();

        var properties = ParseBlockHeader(open.Position);
        var truthy = ParseNodes(insideBlock: true, stopAtElse: true);

        List<INode>? falsy = null;
        if (!_tokens.AtEnd && _tokens.Current is ElseToken)
        {
            _tokens.Advance();
            falsy = ParseNodes(insideBlock: true);
        }

        if (_tokens.AtEnd) { throw new TemplateParseException($"Unclosed {open.Text}", open.Position); }

        _tokens.Advance();

        return new BlockNode(properties, truthy, falsy);
    }

    PropertyChain ParseBlockHeader(Position openPosition)
    {
        var properties = new List<string>();
        while (true)
        {
            if (_tokens.AtEnd) { throw new TemplateParseException("Unclosed block header", openPosition); }

            if (_tokens.Current is not LiteralToken literal)
            {
                _tokens.Advance();

                continue;
            }

            var newlineIndex = literal.Text.IndexOf(NEWLINE);
            if (newlineIndex < 0)
            {
                properties.Add(NormalizeWhitespace(literal.Text));
                _tokens.Advance();

                continue;
            }

            var header = literal.Text[..newlineIndex];
            if (header.Length > 0) { properties.Add(NormalizeWhitespace(header)); }

            _tokens.TrimCurrentLiteral(newlineIndex + 1);

            return new(properties);
        }
    }

    string NormalizeWhitespace(string text) =>
        string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}