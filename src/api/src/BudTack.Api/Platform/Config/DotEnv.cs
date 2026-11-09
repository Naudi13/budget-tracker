namespace BudTack.Api.Platform.Config;

/// <summary>
/// Loads <c>KEY=value</c> lines from a local <c>.env</c> file into the process
/// environment. A variable already set in the environment wins, so CI and the
/// deployment platform's secret store always override the file.
/// </summary>
internal static class DotEnv
{
    public const string FileName = ".env";

    /// <summary>
    /// The nearest <c>.env</c> at or above <paramref name="startDirectory"/>, or
    /// null. <c>dotnet run --project</c> and the IDE start the host in
    /// <c>src/BudTack.Api</c>, while the file lives at the repository root.
    /// </summary>
    public static string? Find(string startDirectory)
    {
        for (var directory = new DirectoryInfo(startDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, FileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    public static void LoadIfPresent(string? path)
    {
        if (path is null || !File.Exists(path))
        {
            return;
        }

        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var separator = line.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim().Trim('"');
            if (Environment.GetEnvironmentVariable(key) is null)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}
