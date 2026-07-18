using AwesomeAssertions;
using PKHeX.Core;
using PKHeX.Facade.Extensions;
using PKHeX.Facade.Pokemons;
using PKHeX.Facade.Repositories;

namespace PKHeX.Facade.Tests;

public class PokemonTests
{
    [Fact]
    public void ShouldLoadPokemonFromFile()
    {
        var pokemon = PokemonFile.LoadFor(GameVersion.SL);
        pokemon.Species.Species.Should().Be(Species.Golduck);
    }

    [Fact]
    public void ShouldAllowToInheritOwnerFromSave()
    {
        var game = AGame(GameVersion.SL, "someone");
        var pokemon = PokemonFile.LoadFor(GameVersion.SL, game);

        pokemon.Owner.Name.Should().NotBe(game.Trainer.Name);
        pokemon.Owner.Name = game.Trainer.Name;
        pokemon.Owner.Name.Should().Be(game.Trainer.Name);
    }

    [Theory]
    [Games(GameVersion.HG)]
    public void ShouldNormalizeSpeciesChangeAndPreservePokemonHistoryAcrossSaveReload(Game game)
    {
        var pokemon = game.Trainer.Party.Pokemons.First();
        var pid = pokemon.PID;
        var ownerId = pokemon.Id;
        var ownerName = pokemon.Owner.Name;
        var metLocation = pokemon.MetConditions.Location.Id;

        pokemon.Pkm.Form = 1;
        pokemon.Species = game.SpeciesRepository.Get(Species.Haunter);
        pokemon.Species = game.SpeciesRepository.Get(Species.Gengar);

        pokemon.Species.Species.Should().Be(Species.Gengar);
        pokemon.Pkm.Form.Should().Be(0);
        pokemon.PID.Should().Be(pid);
        pokemon.Id.Should().Be(ownerId);
        pokemon.Owner.Name.Should().Be(ownerName);
        pokemon.MetConditions.Location.Id.Should().Be(metLocation);

        game.SaveAndReload(reloadedGame =>
        {
            var reloadedPokemon = reloadedGame.Trainer.Party.Pokemons.Single(p => p.PID == pid);
            reloadedPokemon.Species.Species.Should().Be(Species.Gengar);
            reloadedPokemon.Pkm.Form.Should().Be(0);
            reloadedPokemon.Id.Should().Be(ownerId);
            reloadedPokemon.Owner.Name.Should().Be(ownerName);
            reloadedPokemon.MetConditions.Location.Id.Should().Be(metLocation);
        });
    }

    [Theory]
    [Games(GameVersion.GP)]
    public void ShouldRecalculateCombatPowerWhenChangingSpecies(Game game)
    {
        var pokemon = game.Trainer.Party.Pokemons.First();
        var combatPower = (ICombatPower)pokemon.Pkm;
        combatPower.Stat_CP = 0;

        pokemon.Species = game.SpeciesRepository.Get(Species.Gengar);

        combatPower.Stat_CP.Should().BeGreaterThan(0);
    }

    [Theory]
    [Games(GameVersion.HG)]
    public void ShouldRejectUnavailableSpeciesWithoutMutatingPokemon(Game game)
    {
        var pokemon = game.Trainer.Party.Pokemons.First();
        var unavailableSpecies = SpeciesRepository.All.Values
            .First(species => species.Species != Species.None && !game.IsAwareOf(species.Species));
        var originalData = pokemon.Pkm.Data.ToArray();

        pokemon.Invoking(p => p.Species = unavailableSpecies)
            .Should().Throw<ArgumentOutOfRangeException>();

        pokemon.Pkm.Data.ToArray().Should().Equal(originalData);
    }

    [Theory]
    [SupportedSaveFiles]
    public void ShouldSetFormatAwareMaximumEvsAndPersistThemAcrossSaveReload(string saveFile)
    {
        var game = Game.LoadFrom(saveFile);
        var pokemon = game.Trainer.Party.Pokemons.First();

        pokemon.EVs.SetToMaximum();

        pokemon.EVs.MaximumValue.Should().Be(game.SaveFile.MaxEV);
        pokemon.EVs.Attack.Should().BeLessThanOrEqualTo(pokemon.EVs.MaximumValue);
        pokemon.EVs.Defense.Should().BeLessThanOrEqualTo(pokemon.EVs.MaximumValue);
        pokemon.EVs.Health.Should().BeLessThanOrEqualTo(pokemon.EVs.MaximumValue);
        pokemon.EVs.SpecialAttack.Should().BeLessThanOrEqualTo(pokemon.EVs.MaximumValue);
        pokemon.EVs.SpecialDefense.Should().BeLessThanOrEqualTo(pokemon.EVs.MaximumValue);
        pokemon.EVs.Speed.Should().BeLessThanOrEqualTo(pokemon.EVs.MaximumValue);

        game.SaveAndReload(reloadedGame =>
        {
            var reloadedEvs = reloadedGame.Trainer.Party.Pokemons.First().EVs;
            reloadedEvs.Total.Should().Be(pokemon.EVs.Total);
            reloadedEvs.MaximumValue.Should().Be(game.SaveFile.MaxEV);
        });
    }

    [Theory]
    [SupportedSaveFiles(Except = [GameVersion.C])] // cloning in crystal is not working
    public async Task ShouldClonePokemonAndKeepEverythingTheSame(string saveFile)
    {
        var game = Game.LoadFrom(saveFile);

        var pokemon = game.Trainer.Party.Pokemons.First();
        var clone = pokemon.Clone();
        var cloneFromBinary = Pokemon.LoadFrom(clone.ToFile().Bytes);
        var cloneWithLegality = Pokemon.LoadFrom(clone.ToFile().Bytes);
        await cloneWithLegality.ToLegalAsync();

        pokemon.Id.Should().Be(clone.Id);
        pokemon.Id.Should().Be(cloneFromBinary.Id);
        pokemon.Id.Should().Be(cloneWithLegality.Id);

        pokemon.UniqueId.Should().Be(clone.UniqueId);
        pokemon.UniqueId.Should().Be(cloneFromBinary.UniqueId);
        pokemon.UniqueId.Should().Be(cloneWithLegality.UniqueId);

        pokemon.Pkm.EncryptionConstant.Should().Be(clone.Pkm.EncryptionConstant);
        pokemon.Pkm.EncryptionConstant.Should().Be(cloneFromBinary.Pkm.EncryptionConstant);
        pokemon.Pkm.EncryptionConstant.Should().Be(cloneWithLegality.Pkm.EncryptionConstant);
    }

    private Game AGame(GameVersion version, string trainerName) =>
        Game.EmptyOf(GameVersionRepository.Instance.Get(version), trainerName);
}
