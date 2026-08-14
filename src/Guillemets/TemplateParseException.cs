namespace Guillemets;

/// <summary>
/// Thrown when a template violates a MUST rule of the template language
/// (see specs.md) — either while parsing (<see cref="Template.Create"/>)
/// or, for a rule that can only be checked once data is available, while
/// rendering (<see cref="Template.Render"/>).
/// </summary>
public class TemplateParseException(string message, Position position)
    : Exception($"{message} at line {position.Line}, column {position.Column}.")
{
    /// <summary>
    /// The location in the original template source where the violation
    /// occurred.
    /// </summary>
    public Position Position { get; } = position;
}