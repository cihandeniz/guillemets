namespace Guillemets;

public sealed class TemplateParseException(string message, Position position)
    : Exception($"{message} at line {position.Line}, column {position.Column}.")
{
    public Position Position { get; } = position;
}
