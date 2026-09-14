namespace Guillemets.Tokenization;

internal static class TokenExtensions
{
    static readonly string CLOSE_BLOCK = new(Symbols.CLOSE, 2);

    extension(Token token)
    {
        public void ValidateAsBlockOpen(TokenCursor tokens)
        {
            if (tokens.CurrentPrecededByBlankLine) { return; }

            throw new TemplateParseException("Expected a blank line right before the block's opening", token.Position);
        }

        public void ValidateAsBlockClose(TokenCursor tokens)
        {
            if (!tokens.CurrentStartsLine)
            {
                throw new TemplateParseException(
                    $"A literal may not share a line with the block's closing {CLOSE_BLOCK}",
                    token.Position
                );
            }

            if (tokens.CurrentPrecededByBlankLine) { return; }

            throw new TemplateParseException(
                $"Expected a blank line right before the block's closing {CLOSE_BLOCK}",
                token.Position
            );
        }

        public void ValidateBlankLineAfterBlockClose(TokenCursor tokens)
        {
            if (tokens.CurrentFollowedByBlankLine) { return; }

            throw new TemplateParseException(
                $"Expected a blank line right after the block's closing {CLOSE_BLOCK}",
                token.Position.NextLine()
            );
        }

        public void ValidateAsBlockElse(TokenCursor tokens)
        {
            if (!tokens.CurrentPrecededByBlankLine)
            {
                throw new TemplateParseException(
                    $"Expected a blank line right before the block's else {Symbols.TILDE}",
                    token.Position
                );
            }

            if (tokens.CurrentFollowedByBlankLine) { return; }

            throw new TemplateParseException(
                $"Expected a blank line right after the block's else {Symbols.TILDE}",
                token.Position.NextLine()
            );
        }

        public void ValidateAsBlockFooter(bool precededByBlankLine)
        {
            if (precededByBlankLine) { return; }

            throw new TemplateParseException("Expected a blank line right before the block's footer", token.Position);
        }
    }

    extension(Token close)
    {
        public void ValidateQuoteDepthMatches(int openDepth, int closeDepth)
        {
            if (closeDepth == openDepth) { return; }

            throw new TemplateParseException(
                "A block's closing must sit at the same blockquote depth as its opening",
                close.Position
            );
        }

        public void ValidateDepthMatches(Token open)
        {
            if (close.Depth == open.Depth) { return; }

            throw new TemplateParseException(
                $"Block opened with {open.Text} but closed with {close.Text}",
                close.Position
            );
        }
    }
}