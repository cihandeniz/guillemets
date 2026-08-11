using Guillemets.Data.Poco;

namespace Guillemets;

public static class PocoTemplateExtensions
{
    public static string RenderObject(this Template template, object data) =>
        template.Render(new PocoDataSource(data));
}
