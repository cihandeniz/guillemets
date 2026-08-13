using Guillemets.Filters;
using Microsoft.Extensions.Localization;

namespace Guillemets;

public class ParseOptions
{
    public FilterRegistry Filters { get; } = FilterRegistry.CreateDefault();
    public IStringLocalizer? Glossary { get; set; }
}