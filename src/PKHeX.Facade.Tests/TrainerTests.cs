using AwesomeAssertions;
using PKHeX.Facade.Tests.Base;

namespace PKHeX.Facade.Tests;

public class TrainerTests
{
    [Theory]
    [SupportedSaveFiles]
    public void TrainerData_ShouldBeParsed(string saveFile)
    {
        var game = Game.LoadFrom(saveFile);
        game.Trainer.Gender.Should().Be(Gender.Male);
        game.Trainer.Name.Should().NotBeNull();
        game.Trainer.Money.Amount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Editor_ShouldPersistProfileMoneyAndPlayTimeThroughDraftApply()
    {
        var session = new GameSession();
        session.Load(File.ReadAllBytes(SaveFilePath.Emerald), "emerald.sav");
        var activeGame = session.ActiveGame!;
        var draft = session.CreateDraft();

        draft.Trainer.Editor.SetName("Ravi");
        draft.Trainer.Editor.SetGender(Gender.Female);
        draft.Trainer.Editor.SetMoney(123_456);
        draft.Trainer.Editor.SetPlayTime(new TrainerPlayTime(hours: 12, minutes: 34, seconds: 56));

        activeGame.Trainer.Name.Should().NotBe("Ravi");
        activeGame.Trainer.Gender.Should().NotBe(Gender.Female);
        activeGame.Trainer.Money.Amount.Should().NotBe(123_456);

        session.ApplyDraft();

        session.ActiveGame!.Trainer.Name.Should().Be("Ravi");
        session.ActiveGame.Trainer.Gender.Should().Be(Gender.Female);
        session.ActiveGame.Trainer.Money.Amount.Should().Be(123_456);
        session.ActiveGame.Trainer.PlayTime.Should().Be(new TrainerPlayTime(hours: 12, minutes: 34, seconds: 56));
    }

    [Fact]
    public void Editor_ShouldRejectUnavailableGenderCapability()
    {
        var game = Game.LoadFrom(SaveFilePath.Yellow);

        game.Trainer.Editor.Capabilities.CanEditGender.Should().BeFalse();
        game.Invoking(currentGame => currentGame.Trainer.Editor.SetGender(Gender.Female))
            .Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Trainer_ShouldNotExposeGenderAsAnUncheckedMutationPath()
    {
        typeof(Trainer).GetProperty(nameof(Trainer.Gender))!.SetMethod.Should().BeNull();
    }

    [Fact]
    public void Editor_ShouldRejectInvalidGeneration1MaximumPlayTime()
    {
        var game = Game.LoadFrom(SaveFilePath.Yellow);
        var originalPlayTime = game.Trainer.PlayTime;
        var originalBytes = game.ToByteArray();

        game.Invoking(currentGame => currentGame.Trainer.Editor.SetPlayTime(
                new TrainerPlayTime(hours: byte.MaxValue, minutes: 1, seconds: 1)))
            .Should().Throw<ArgumentOutOfRangeException>();
        game.Trainer.PlayTime.Should().Be(originalPlayTime);
        game.ToByteArray().Should().Equal(originalBytes);
    }

    [Fact]
    public void Editor_ShouldClearGeneration1MaximumPlayTimeMarkerWhenLowered()
    {
        var game = Game.LoadFrom(SaveFilePath.Yellow);

        game.Trainer.Editor.SetPlayTime(new TrainerPlayTime(hours: byte.MaxValue, minutes: 0, seconds: 0));
        game.Trainer.Editor.SetPlayTime(new TrainerPlayTime(hours: 1, minutes: 2, seconds: 3));

        game.SaveAndReload(reloadedGame =>
        {
            reloadedGame.Trainer.PlayTime.Should().Be(new TrainerPlayTime(hours: 1, minutes: 2, seconds: 3));
        });
    }

    [Fact]
    public void Editor_ShouldRejectUnrepresentableNamesWithoutChangingTheDraft()
    {
        var game = Game.LoadFrom(SaveFilePath.Emerald);
        var originalName = game.Trainer.Name;
        var originalBytes = game.ToByteArray();
        var unrepresentableName = new string('A', game.SaveFile.MaxStringLengthTrainer + 1);

        game.Invoking(currentGame => currentGame.Trainer.Editor.SetName(unrepresentableName))
            .Should().Throw<ArgumentException>();
        game.Trainer.Name.Should().Be(originalName);
        game.ToByteArray().Should().Equal(originalBytes);
    }
}
