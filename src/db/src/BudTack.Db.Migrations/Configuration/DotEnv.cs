namespace BudTack.Db.Migrations.Configuration;

/// <summary>
/// Loads <c>KEY=value</c> lines from a local <c>.env</c> file into the process
/// environment. A variable already set in the environment wins, so CI and the
/// deployment platform's secret store always override the file.
/// </summary>
public static class DotEnv
{
    public static void LoadIfPresent(string path)
    {
        if (!File.Exists(path))
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
