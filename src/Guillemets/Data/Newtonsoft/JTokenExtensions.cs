using Guillemets.Data.Newtonsoft;
using Newtonsoft.Json.Linq;

namespace Guillemets;

public static class JTokenExtensions
{
    public static string Render(this Template template, JToken data) =>
        template.Render(new JTokenDataSource(data));
}