using AwesomeAssertions;

namespace PKHeX.Facade.Tests;

public class GameTests
{
    [Fact]
    public void CreateDraft_ShouldReturnIsolatedGameWithEquivalentBytes()
    {
        var game = Game.LoadFrom(SaveFilePath.Emerald);

        var draft = game.CreateDraft();

        draft.Should().NotBeSameAs(game);
        draft.SaveFile.Should().NotBeSameAs(game.SaveFile);
        draft.ToByteArray().Should().Equal(game.ToByteArray());
    }

    [Fact]
    public void CreateDraft_ShouldCapturePendingCachedPartyChanges()
    {
        var game = Game.LoadFrom(SaveFilePath.Emerald);
        var pendingPokemon = game.Trainer.Party.Pokemons.First();
        var expectedShinyState = !pendingPokemon.IsShiny;
        pendingPokemon.SetShiny(expectedShinyState);
        game.SaveFile.PartyData.First().IsShiny.Should().NotBe(expectedShinyState);

        var draft = game.CreateDraft();

        draft.Trainer.Party.Pokemons.First().IsShiny.Should().Be(expectedShinyState);
    }

    [Fact]
    public void Game_Gen4_ShouldPreserveExactEmulatorFooterWhenExporting()
    {
        const int footerOffset = 0x080000;
        var originalFooter = File.ReadAllBytes(SaveFilePath.HgSs).AsSpan(footerOffset).ToArray();

        var game = Game.LoadFrom(SaveFilePath.HgSs);
        game.SaveAndReload(savedGame =>
        {
            var savedFooter = savedGame.ToByteArray().AsSpan(footerOffset).ToArray();
            savedFooter.Should().Equal(originalFooter);
        });
    }
}
