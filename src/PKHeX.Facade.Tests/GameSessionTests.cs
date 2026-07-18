using System.Security.Cryptography;
using AwesomeAssertions;

namespace PKHeX.Facade.Tests;

public class GameSessionTests
{
    [Fact]
    public void OperationsWithoutLoadedGame_ShouldUseStableError()
    {
        var session = new GameSession();

        session.Invoking(currentSession => currentSession.CreateDraft())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("No game is loaded.");
        session.Invoking(currentSession => currentSession.Export())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("No game is loaded.");
        session.Invoking(currentSession => currentSession.RestoreOriginal())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("No game is loaded.");
    }

    [Fact]
    public void Load_ShouldDefensivelyCopyCallerBytes()
    {
        var callerBytes = File.ReadAllBytes(SaveFilePath.Emerald);
        var expectedBytes = callerBytes.ToArray();
        var session = new GameSession();

        session.Load(callerBytes, "emerald.sav");
        callerBytes.AsSpan().Clear();
        session.RestoreOriginal();

        session.Export().Should().Equal(expectedBytes);
        session.OriginalSha256.Should().Be(Convert.ToHexString(SHA256.HashData(expectedBytes)));
    }

    [Fact]
    public void Load_WhenBytesAreInvalid_ShouldKeepExistingSession()
    {
        var session = LoadSession(SaveFilePath.Emerald);
        var activeGame = session.ActiveGame;
        var activeBytes = session.Export();
        var originalSha256 = session.OriginalSha256;

        session.Invoking(currentSession => currentSession.Load([1, 2, 3], "invalid.sav"))
            .Should().Throw<GameNotLoadedException>();

        session.ActiveGame.Should().BeSameAs(activeGame);
        session.Export().Should().Equal(activeBytes);
        session.OriginalSha256.Should().Be(originalSha256);
        session.FileName.Should().Be(Path.GetFileName(SaveFilePath.Emerald));
    }

    [Fact]
    public void Export_ShouldReturnBytesIsolatedFromActiveGame()
    {
        var session = LoadSession(SaveFilePath.Emerald);
        var expectedBytes = session.Export();
        var exportedBytes = session.Export();

        exportedBytes.AsSpan().Clear();

        session.Export().Should().Equal(expectedBytes);
    }

    [Fact]
    public void CreateDraft_ShouldIsolateReferencesAndActiveBytes()
    {
        var session = LoadSession(SaveFilePath.Emerald);
        var activeGame = session.ActiveGame;
        var activeBytes = session.Export();

        var draft = session.CreateDraft();
        draft.Trainer.Money.Set(1);

        draft.Should().NotBeSameAs(activeGame);
        draft.SaveFile.Should().NotBeSameAs(activeGame!.SaveFile);
        session.ActiveGame.Should().BeSameAs(activeGame);
        session.Export().Should().Equal(activeBytes);
        session.HasDraft.Should().BeTrue();
        session.DraftGame.Should().BeSameAs(draft);
    }

    [Fact]
    public void CreateDraft_WhenDraftExists_ShouldUseStableErrorAndKeepExistingDraft()
    {
        var session = LoadSession(SaveFilePath.Emerald);
        var draft = session.CreateDraft();

        session.Invoking(currentSession => currentSession.CreateDraft())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("A game draft is already active.");
        session.DraftGame.Should().BeSameAs(draft);
    }

    [Fact]
    public void DiscardDraft_ShouldLeaveActiveGameUnchanged()
    {
        var session = LoadSession(SaveFilePath.Emerald);
        var activeGame = session.ActiveGame;
        var activeBytes = session.Export();
        session.CreateDraft().Trainer.Money.Set(1);

        session.DiscardDraft();

        session.HasDraft.Should().BeFalse();
        session.DraftGame.Should().BeNull();
        session.ActiveGame.Should().BeSameAs(activeGame);
        session.Export().Should().Equal(activeBytes);
    }

    [Fact]
    public void DiscardDraft_WhenNoDraftExists_ShouldUseStableError()
    {
        var session = LoadSession(SaveFilePath.Emerald);

        session.Invoking(currentSession => currentSession.DiscardDraft())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("No game draft is active.");
    }

    [Fact]
    public void ApplyDraft_ShouldReloadFreshGameAndPreserveDraftChanges()
    {
        var session = LoadSession(SaveFilePath.Emerald);
        var originalActiveGame = session.ActiveGame;
        var draft = session.CreateDraft();
        draft.Trainer.Money.Set(1);

        session.ApplyDraft();

        session.ActiveGame.Should().NotBeSameAs(originalActiveGame);
        session.ActiveGame.Should().NotBeSameAs(draft);
        session.ActiveGame!.SaveFile.Should().NotBeSameAs(draft.SaveFile);
        session.ActiveGame.Trainer.Money.Amount.Should().Be(1);
        session.HasDraft.Should().BeFalse();
    }

    [Fact]
    public void ApplyDraft_WhenNoDraftExists_ShouldUseStableError()
    {
        var session = LoadSession(SaveFilePath.Emerald);

        session.Invoking(currentSession => currentSession.ApplyDraft())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("No game draft is active.");
    }

    [Fact]
    public void RestoreOriginal_ShouldReproduceFixtureBytesAndDigest()
    {
        var originalBytes = File.ReadAllBytes(SaveFilePath.Emerald);
        var session = new GameSession();
        session.Load(originalBytes, "emerald.sav");
        session.CreateDraft().Trainer.Money.Set(1);
        session.ApplyDraft();

        session.RestoreOriginal();

        session.Export().Should().Equal(originalBytes);
        session.OriginalSha256.Should().Be(Convert.ToHexString(SHA256.HashData(originalBytes)));
    }

    [Fact]
    public void RestoreOriginal_WhenCoreNormalizesSave_ShouldReturnExactUploadBytes()
    {
        var originalBytes = File.ReadAllBytes(SaveFilePath.Crystal);
        var session = LoadSession(SaveFilePath.Crystal);
        session.CreateDraft().Trainer.Money.Set(1);
        session.ApplyDraft();

        session.RestoreOriginal();

        session.Export().Should().Equal(originalBytes);
        session.OriginalSha256.Should().Be(Convert.ToHexString(SHA256.HashData(originalBytes)));
    }

    [Fact]
    public void RestoreOriginal_WhenDraftExists_ShouldRejectWithoutChangingSession()
    {
        var session = LoadSession(SaveFilePath.Emerald);
        var activeGame = session.ActiveGame;
        var activeBytes = session.Export();
        var draft = session.CreateDraft();
        draft.Trainer.Money.Set(1);

        session.Invoking(currentSession => currentSession.RestoreOriginal())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("Apply or discard the active game draft before restoring the original save.");
        session.ActiveGame.Should().BeSameAs(activeGame);
        session.Export().Should().Equal(activeBytes);
        session.DraftGame.Should().BeSameAs(draft);
    }

    [Theory]
    [InlineData(SaveFilePath.Yellow)]
    [InlineData(SaveFilePath.Emerald)]
    [InlineData(SaveFilePath.HgSs)]
    [InlineData(SaveFilePath.LetsGoEevee)]
    public void DraftApplyAndRestore_ShouldCoverRepresentativeFormats(string saveFilePath)
    {
        var originalBytes = File.ReadAllBytes(saveFilePath);
        var session = LoadSession(saveFilePath);
        var draft = session.CreateDraft();
        var expectedMoney = draft.Trainer.Money.Amount == 1 ? 2u : 1u;
        draft.Trainer.Money.Set(expectedMoney);

        session.ApplyDraft();

        session.ActiveGame!.Trainer.Money.Amount.Should().Be(expectedMoney);

        session.RestoreOriginal();

        session.Export().Should().Equal(originalBytes);
    }

    [Fact]
    public void Load_ShouldReplaceOriginalBaselineAndDiscardExistingDraft()
    {
        var replacementBytes = File.ReadAllBytes(SaveFilePath.Emerald);
        var session = LoadSession(SaveFilePath.Yellow);
        session.CreateDraft();

        session.Load(replacementBytes, "emerald.sav");
        session.CreateDraft().Trainer.Money.Set(1);
        session.ApplyDraft();
        session.RestoreOriginal();

        session.Export().Should().Equal(replacementBytes);
        session.FileName.Should().Be("emerald.sav");
        session.OriginalSha256.Should().Be(Convert.ToHexString(SHA256.HashData(replacementBytes)));
        session.HasDraft.Should().BeFalse();
    }

    [Fact]
    public void Gen4Session_ShouldPreserveExactEmulatorFooterThroughLifecycle()
    {
        const int footerOffset = 0x080000;
        var originalBytes = File.ReadAllBytes(SaveFilePath.HgSs);
        var originalFooter = originalBytes.AsSpan(footerOffset).ToArray();
        var session = LoadSession(SaveFilePath.HgSs);

        session.Export().AsSpan(footerOffset).ToArray().Should().Equal(originalFooter);

        session.CreateDraft().Trainer.Money.Set(1);
        session.ApplyDraft();

        session.Export().AsSpan(footerOffset).ToArray().Should().Equal(originalFooter);

        session.RestoreOriginal();

        session.Export().Should().Equal(originalBytes);
        session.Export().AsSpan(footerOffset).ToArray().Should().Equal(originalFooter);
    }

    private static GameSession LoadSession(string path)
    {
        var session = new GameSession();
        session.Load(File.ReadAllBytes(path), Path.GetFileName(path));
        return session;
    }
}
