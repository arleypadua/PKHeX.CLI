using AwesomeAssertions;
using PKHeX.Facade.Tests.Base;

namespace PKHeX.Facade.Tests;

public class PokemonPartyTests
{
    [Theory]
    [SupportedSaveFiles]
    public void PartyShouldContainPokemon(string saveFile)
    {
        var game = Game.LoadFrom(saveFile);
        game.Trainer.Party.Pokemons.Should().HaveCountGreaterThan(0);
        game.Trainer.Party.Pokemons.Should().AllSatisfy(p =>
        {
            p.BaseStats.Attack.Should().BeGreaterThan(0);
            p.BaseStats.Defense.Should().BeGreaterThan(0);
            p.BaseStats.Health.Should().BeGreaterThan(0);
            p.BaseStats.Speed.Should().BeGreaterThan(0);
            p.BaseStats.Total.Should().BeGreaterThan(0);
            p.BaseStats.SpecialAttack.Should().BeGreaterThan(0);
            p.BaseStats.SpecialDefense.Should().BeGreaterThan(0);
        });
    }

    [Theory]
    [SupportedSaveFiles]
    public void PartyShouldBePersistedAcrossSaves(string saveFile)
    {
        var game = Game.LoadFrom(saveFile);
        var firstPokemon = game.Trainer.Party.Pokemons.First();

        firstPokemon.IsShiny.Should().BeFalse();

        firstPokemon.SetShiny(true);

        firstPokemon.IsShiny.Should().BeTrue();

        game.SaveAndReload(savedGame =>
        {
            var savedPokemon = savedGame.Trainer.Party.Pokemons.First();
            savedPokemon.PID.Should().Be(firstPokemon.PID);
            savedPokemon.IsShiny.Should().Be(firstPokemon.IsShiny);
        });
    }

    [Theory]
    [SupportedSaveFiles]
    [InlineData(SaveFilePath.Yellow)]
    public void Commit_ShouldLeaveSaveFilePartyExactlyMatchingCachedParty(string saveFile)
    {
        var game = Game.LoadFrom(saveFile);
        var expectedParty = game.Trainer.Party.Pokemons.Select(pokemon => pokemon.Pkm).ToList();

        game.Trainer.Party.Commit();

        var savedParty = game.SaveFile.PartyData;
        savedParty.Should().HaveSameCount(expectedParty);
        for (var partyIndex = 0; partyIndex < savedParty.Count; partyIndex++)
            savedParty[partyIndex].Data.SequenceEqual(expectedParty[partyIndex].Data).Should().BeTrue();
    }
}
