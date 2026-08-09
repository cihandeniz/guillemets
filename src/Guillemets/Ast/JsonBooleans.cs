using System.Text.Json;

namespace Guillemets.Ast;

internal static class JsonBooleans
{
    public static readonly JsonElement TRUE = JsonDocument.Parse("true").RootElement;
    public static readonly JsonElement FALSE = JsonDocument.Parse("false").RootElement;
}