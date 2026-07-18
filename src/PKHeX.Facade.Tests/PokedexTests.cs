using AwesomeAssertions;
using PKHeX.Core;
using PKHeX.Facade.Repositories;

namespace PKHeX.Facade.Tests;

public class PokedexTests
{
    private static readonly SpeciesDefinition Bulbasaur = new(Species.Bulbasaur, "Bulbasaur");

    [Theory]
    [InlineData(SaveFilePath.Yellow)]
    [InlineData(SaveFilePath.Crystal)]
    [InlineData(SaveFilePath.Emerald)]
    [InlineData(SaveFilePath.HgSs)]
    [InlineData(SaveFilePath.LetsGoPikachu)]
    public void SetState_ShouldPersistSeenAndCaughtForNativeDexFormats(string saveFilePath)
    {
        var game = Game.LoadFrom(saveFilePath);
        game.Pokedex.Capabilities.Should().Be(PokedexCapabilities.SpeciesSeenCaught);
        game.Pokedex.SupportsSpeciesSeenCaught(Bulbasaur).Should().BeTrue();
        game.Pokedex.SetState(Bulbasaur, PokedexState.NotSeen);
        game.Pokedex.SetState(Bulbasaur, PokedexState.Caught);

        game.SaveAndReload(reloadedGame =>
            reloadedGame.Pokedex.GetState(Bulbasaur).Should().Be(PokedexState.Caught));
    }

    [Fact]
    public void SetState_WhenCaughtWithoutSeen_ShouldRejectInvalidState()
    {
        var game = Game.LoadFrom(SaveFilePath.Emerald);
        game.Pokedex.Invoking(pokedex => pokedex.SetState(Bulbasaur, new PokedexState(false, true)))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FormSpecificState_ShouldBeExplicitlyUnsupported()
    {
        var game = Game.LoadFrom(SaveFilePath.Emerald);
        game.Pokedex.Capabilities.Should().Be(PokedexCapabilities.SpeciesSeenCaught);
        game.Pokedex.SupportsFormSeenCaught.Should().BeFalse();
        game.Pokedex.Invoking(pokedex => pokedex.GetState(Bulbasaur, FormDefinition.Default with { Id = 1 }))
            .Should().Throw<PokedexOperationNotSupportedException>();
    }

    [Fact]
    public void CreateDraft_ShouldPreserveActiveSaveBytesAndCopyPendingPartyChanges()
    {
        var game = Game.LoadFrom(SaveFilePath.LetsGoPikachu);
        var pendingPokemon = game.Trainer.Party.Pokemons.First();
        var expectedShinyState = !pendingPokemon.IsShiny;
        pendingPokemon.SetShiny(expectedShinyState);
        var activeSaveBytes = game.SaveFile.Data.ToArray();

        var draft = game.CreateDraft();

        game.SaveFile.Data.ToArray().Should().Equal(activeSaveBytes);
        draft.Trainer.Party.Pokemons.First().IsShiny.Should().Be(expectedShinyState);
    }

    [Fact]
    public void CreateDraft_ShouldCopyPendingBoxChangesWithoutMutatingTheActiveSave()
    {
        var game = Game.LoadFrom(SaveFilePath.HgSs);
        var pendingPokemonIndex = game.Trainer.PokemonBox.All
            .Select((pokemon, index) => (pokemon, index))
            .First(entry => entry.pokemon.Species != Species.None).index;
        var pendingPokemon = game.Trainer.PokemonBox.All[pendingPokemonIndex];
        var expectedShinyState = !pendingPokemon.IsShiny;
        pendingPokemon.SetShiny(expectedShinyState);
        var activeSaveBytes = game.SaveFile.Data.ToArray();

        var draft = game.CreateDraft();

        game.SaveFile.Data.ToArray().Should().Equal(activeSaveBytes);
        draft.Trainer.PokemonBox.All[pendingPokemonIndex].IsShiny.Should().Be(expectedShinyState);
    }

    [Fact]
    public void SupportsSpeciesSeenCaught_ShouldRejectSwordShieldSpeciesWithoutNativeDexEntry()
    {
        var game = new Game(new SAV8SWSH());
        var swordShield = (SAV8SWSH)game.SaveFile;
        var speciesWithDexEntry = game.SpeciesRepository.AllGameSpecies
            .First(species => swordShield.Zukan.GetEntry((ushort)species.Species, out _));
        var speciesWithoutDexEntry = game.SpeciesRepository.AllGameSpecies
            .First(species => !swordShield.Zukan.GetEntry((ushort)species.Species, out _));

        game.Pokedex.Capabilities.Should().Be(PokedexCapabilities.SpeciesSeenCaught);
        game.Pokedex.SupportsSpeciesSeenCaught(speciesWithDexEntry).Should().BeTrue();
        game.Pokedex.SupportsSpeciesSeenCaught(speciesWithoutDexEntry).Should().BeFalse();
    }


}
