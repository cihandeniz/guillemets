namespace Guillemets.Tests;

static class SpecsRoot
{
    public static readonly string PATH = Find();

    static string Find()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(System.IO.Path.Combine(dir.FullName, "Guillemets.slnx")))
        {
            dir = dir.Parent;
        }

        if (dir is null)
        {
            throw new InvalidOperationException(
                "Could not locate repo root (Guillemets.slnx) from test assembly location.");
        }

        return System.IO.Path.Combine(dir.FullName, "specs");
    }
}
