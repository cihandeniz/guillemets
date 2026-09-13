namespace Guillemets.Tokenization;

internal static class TokenExtensions
{
    static readonly string CLOSE_BLOCK = new(Symbols.CLOSE, 2);

    extension(Token token)
    {
        public void ValidateAsBlockOpen()
        {
            if (token.PrecededByBlankLine) { return; }

            throw new TemplateParseException("Expected a blank line right before the block's opening", token.Position);
        }

        public void ValidateAsBlockClose()
        {
            if (!token.Position.AtLineStart)
            {
                throw new TemplateParseException(
                    $"A literal may not share a line with the block's closing {CLOSE_BLOCK}",
                    token.Position
                );
            }

            if (token.PrecededByBlankLine) { return; }

            throw new TemplateParseException(
                $"Expected a blank line right before the block's closing {CLOSE_BLOCK}",
                token.Position
            );
        }

        public void ValidateBlankLineAfterBlockClose()
        {
            if (token.FollowedByBlankLine) { return; }

            throw new TemplateParseException(
                $"Expected a blank line right after the block's closing {CLOSE_BLOCK}",
                token.Position.NextLine()
            );
        }

        public void ValidateAsBlockElse()
        {
            if (!token.PrecededByBlankLine)
            {
                throw new TemplateParseException(
                    $"Expected a blank line right before the block's else {Symbols.TILDE}",
                    token.Position
                );
            }

            if (token.FollowedByBlankLine) { return; }

            throw new TemplateParseException(
                $"Expected a blank line right after the block's else {Symbols.TILDE}",
                token.Position.NextLine()
            );
        }

        public void ValidateAsBlockFooter()
        {
            if (token.PrecededByBlankLine) { return; }

            throw new TemplateParseException("Expected a blank line right before the block's footer", token.Position);
        }
    }

    extension(Token close)
    {
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