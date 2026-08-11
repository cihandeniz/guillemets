using Humanizer;
using System.Buffers;
using System.Text.Json;

namespace Guillemets.Ast;

internal class VariableStore
{
    readonly Dictionary<string, JsonElement> _values = [];

    public void Define(string name, string value) =>
        _values[name.Dehumanize()] = ToJsonElement(value);

    public bool TryResolve(string name, out JsonElement value) =>
        _values.TryGetValue(name.Dehumanize(), out value);

    static JsonElement ToJsonElement(string value)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);
        writer.WriteStringValue(value);
        writer.Flush();

        return JsonDocument.Parse(buffer.WrittenMemory).RootElement;
    }
}