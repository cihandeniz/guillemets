namespace Guillemets.Rendering;

/// <summary>
/// Thrown when a glossary is ambiguous — two localization entries
/// translate to the same term — and
/// <see cref="ParseOptions.GlossaryCollisionResolver"/> isn't set to
/// resolve it.
/// </summary>
public class GlossaryException(string message)
    : Exception(message);