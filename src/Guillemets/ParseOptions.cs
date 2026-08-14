using Guillemets.Filters;
using Microsoft.Extensions.Localization;

namespace Guillemets;

public class ParseOptions
{
    public FilterRegistry Filters { get; } = FilterRegistry.CreateDefault();
    public IStringLocalizer? Localizer { get; set; }
    public Func<string, string> PropertyNameConversion { get; set; } = TextCasing.Dehumanize;
    public Func<IEnumerable<string>, string>? GlossaryCollisionResolver { get; set; }
}