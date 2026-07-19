using Guillemets.Ast;
using Guillemets.Tokens;

using static Guillemets.Tokenizer;

namespace Guillemets;

internal class Parser(TokenCursor _tokens)
{
    public List<INode> Parse() =>
        ParseNodes(closeDepth: null);

    List<INode> ParseNodes(int? closeDepth)
    {
        var nodes = new List<INode>();
        while (!_tokens.AtEnd && !ReachedClose(closeDepth))
        {
            nodes.Add(ParseNext());
        }

        return nodes;
    }

    bool ReachedClose(int? closeDepth) =>
        closeDepth is not null &&
        _tokens.Current is CloseToken &&
        _tokens.CountConsecutive<CloseToken>() == closeDepth;

    INode ParseNext()
    {
        if (_tokens.Current is OpenToken open)
        {
            var depth = _tokens.CountConsecutive<OpenToken>();

            return depth >= 2 ? ParseBlock(open, depth) : ParseVariable(open);
        }

        if (_tokens.Current is LiteralToken literal)
        {
            _tokens.Advance();

            return new LiteralNode(literal.Text);
        }

        _tokens.Advance();

        return new LiteralNode($"{COLON}");
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

    INode ParseBlock(OpenToken open, int depth)
    {
        _tokens.Skip(depth);

        var properties = ParseBlockHeader(open.Position);
        var body = ParseNodes(closeDepth: depth);

        if (_tokens.AtEnd) { throw new TemplateParseException($"Unclosed {new string(OPEN, depth)}", open.Position); }

        _tokens.Skip(depth);
        _tokens.TrimLeadingNewlineIfPresent();

        return new BlockNode(properties, body);
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