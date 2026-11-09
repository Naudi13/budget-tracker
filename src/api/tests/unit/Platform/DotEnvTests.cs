using BudTack.Api.Platform.Config;
using Xunit;

namespace BudTack.Unit.Tests.Platform;

public sealed class DotEnvTests : IDisposable
{
    private readonly DirectoryInfo _root =
        Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "bud-tack-dotenv-" + Guid.NewGuid().ToString("N")));

    public void Dispose() => _root.Delete(recursive: true);

    [Fact]
    public void Find_fileInAParentDirectory_returnsIt()
    {
        var expected = Path.Combine(_root.FullName, DotEnv.FileName);
        File.WriteAllText(expected, "KEY=value");
        var start = _root.CreateSubdirectory(Path.Combine("src", "BudTack.Api")).FullName;

        Assert.Equal(expected, DotEnv.Find(start));
    }

    [Fact]
    public void Find_fileInTheStartDirectory_winsOverAParentOne()
    {
        File.WriteAllText(Path.Combine(_root.FullName, DotEnv.FileName), "KEY=parent");
        var start = _root.CreateSubdirectory("api");
        var expected = Path.Combine(start.FullName, DotEnv.FileName);
        File.WriteAllText(expected, "KEY=own");

        Assert.Equal(expected, DotEnv.Find(start.FullName));
    }

    [Fact]
    public void LoadIfPresent_noFile_changesNothing()
    {
        var key = "BUD_TACK_TEST_" + Guid.NewGuid().ToString("N");

        DotEnv.LoadIfPresent(null);

        Assert.Null(Environment.GetEnvironmentVariable(key));
    }

    [Fact]
    public void LoadIfPresent_variableAlreadySet_keepsTheEnvironmentValue()
    {
        var key = "BUD_TACK_TEST_" + Guid.NewGuid().ToString("N");
        var path = Path.Combine(_root.FullName, DotEnv.FileName);
        File.WriteAllText(path, $"# comment\n{key}=from-file\n");
        Environment.SetEnvironmentVariable(key, "from-environment");
        try
        {
            DotEnv.LoadIfPresent(path);

            Assert.Equal("from-environment", Environment.GetEnvironmentVariable(key));
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, null);
        }
    }

    [Fact]
    public void LoadIfPresent_variableNotSet_takesTheFileValue()
    {
        var key = "BUD_TACK_TEST_" + Guid.NewGuid().ToString("N");
        var path = Path.Combine(_root.FullName, DotEnv.FileName);
        File.WriteAllText(path, $"{key}=\"from-file\"\n");
        try
        {
            DotEnv.LoadIfPresent(path);

            Assert.Equal("from-file", Environment.GetEnvironmentVariable(key));
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, null);
        }
    }
}
