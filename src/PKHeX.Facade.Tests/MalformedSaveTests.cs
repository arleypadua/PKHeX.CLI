using AwesomeAssertions;
using PKHeX.Facade.Tests.Base;

namespace PKHeX.Facade.Tests;

public class MalformedSaveTests
{
    /// <summary>
    /// A real save with corrupted contents still matches on size and footer, so it is detected as a save
    /// and then decoded. Garbage in a box slot decodes to a species id that does not exist, which used to
    /// surface as a raw KeyNotFoundException out of the repository indexer and reach the user as a crash.
    /// </summary>
    private static byte[] CorruptedSave()
    {
        var bytes = File.ReadAllBytes(SaveFilePath.HgSs);
        Array.Fill(bytes, (byte)0xFF, 0x10000, 0x100);
        return bytes;
    }

    [Fact]
    public void LoadFrom_ShouldReportGameNotLoaded_WhenASaveIsDetectedButItsContentsCannotBeDecoded()
    {
        var loading = () => Game.LoadFrom(CorruptedSave(), "corrupted.dsv");

        loading.Should().Throw<GameNotLoadedException>();
    }

    [Fact]
    public void LoadFrom_ShouldKeepTheUnderlyingFailure_WhenContentsCannotBeDecoded()
    {
        // The original cause has to survive, otherwise error reporting loses the only clue about why a
        // file failed, and a genuine decoding bug becomes indistinguishable from an unsupported file.
        var thrown = Record.Exception(() => Game.LoadFrom(CorruptedSave(), "corrupted.dsv"));

        thrown.Should().BeOfType<GameNotLoadedException>()
            .Which.InnerException.Should().NotBeNull();
    }

    [Fact]
    public void LoadFrom_ShouldReportGameNotLoaded_WhenBytesMatchASaveSizeButAreNotASave()
    {
        var notASave = new byte[File.ReadAllBytes(SaveFilePath.HgSs).Length];
        Array.Fill(notASave, (byte)0xFF);

        var loading = () => Game.LoadFrom(notASave, "not-a-save.dsv");

        loading.Should().Throw<GameNotLoadedException>();
    }

    [Fact]
    public void LoadFrom_ShouldReportGameNotLoaded_WhenBytesAreNotACredibleSaveAtAll()
    {
        var loading = () => Game.LoadFrom([1, 2, 3, 4], "not-a-save.txt");

        loading.Should().Throw<GameNotLoadedException>();
    }

    [Fact]
    public void LoadFrom_ShouldStillLoadAValidSave()
    {
        var game = Game.LoadFrom(File.ReadAllBytes(SaveFilePath.HgSs), "savedata_4hgss.dsv");

        game.Trainer.Should().NotBeNull();
    }
}
