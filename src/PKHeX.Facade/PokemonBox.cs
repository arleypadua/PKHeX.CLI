using System.Collections.Immutable;
using PKHeX.Core;
using PKHeX.Facade.Abstractions;
using PKHeX.Facade.Pokemons;

namespace PKHeX.Facade;

public class PokemonBox : IMutablePokemonCollection
{
    public PokemonBox(Game game)
    {
        _game = game;

        PopulateFromSave();
    }

    private readonly Game _game;
    private IList<Pokemon> _pokemonList = default!;

    public IDictionary<Species, List<Pokemon>> BySpecies { get; private set; } = default!;
    public IList<Pokemon> All => _pokemonList;
    public int Capacity => _game.SaveFile.BoxCount * _game.SaveFile.BoxSlotCount;
    public int OccupiedSlotCount => _pokemonList.Count(pokemon => pokemon.Species != Species.None);
    public int AvailableSlotCount => Capacity - OccupiedSlotCount;

    public void Commit()
    {
        _game.SaveFile.BoxData = _pokemonList
            .Select(p => p.Pkm)
            .ToList();
    }

    public bool AddOnEmptySlot(Pokemon pokemon)
    {
        return AddToEmptySlots([pokemon]).AddedCount == 1;
    }

    public PokemonBoxAdditionSummary AddToEmptySlots(IEnumerable<Pokemon> pokemonToAdd)
    {
        var requestedPokemon = pokemonToAdd.ToList();
        var addedCount = 0;
        var incompatibleCount = 0;
        var capacitySkippedCount = 0;

        for (var pokemonIndex = 0; pokemonIndex < requestedPokemon.Count; pokemonIndex++)
        {
            var openSlot = _game.SaveFile.NextOpenBoxSlot();
            if (openSlot == -1)
            {
                capacitySkippedCount = requestedPokemon.Count - pokemonIndex;
                break;
            }

            var pokemon = requestedPokemon[pokemonIndex];
            var compatiblePokemon = EntityConverter.ConvertToType(pokemon.Pkm, _game.SaveFile.PKMType, out _);
            if (compatiblePokemon?.GetType() != _game.SaveFile.PKMType ||
                !_game.SaveFile.IsCompatiblePKM(compatiblePokemon) ||
                _game.SaveFile.EvaluateCompatibility(compatiblePokemon).Count > 0)
            {
                incompatibleCount++;
                continue;
            }

            _game.SaveFile.SetBoxSlotAtIndex(compatiblePokemon, openSlot);
            addedCount++;
        }

        if (addedCount > 0) PopulateFromSave();

        return new PokemonBoxAdditionSummary(
            requestedPokemon.Count,
            addedCount,
            incompatibleCount,
            capacitySkippedCount,
            AvailableSlotCount);
    }

    private void PopulateFromSave()
    {
        _pokemonList = _game.SaveFile.BoxData
            .Select(p => new Pokemon(p, _game))
            .ToList();

        BySpecies = _pokemonList
            .Where(p => p.Species != Species.None)
            .GroupBy(p => p.Species)
            .ToImmutableSortedDictionary(
                key => key.Key.Species,
                value => value.OrderByDescending(p => p.Level).ToList());
    }

    public void AddOrUpdate(UniqueId id, Pokemon pokemon)
    {
        var existing = _pokemonList.FirstOrDefault(p => p.UniqueId.Equals(id));

        if (existing is null) HandleAdd(pokemon);
        else HandleUpdate(existing, pokemon);
    }

    private void HandleAdd(Pokemon pokemon)
    {
        var added = AddOnEmptySlot(pokemon);
        if (!added) throw new InvalidOperationException("Pokemon box is full.");
    }

    private void HandleUpdate(Pokemon existing, Pokemon pokemon)
    {
        var index = _pokemonList.IndexOf(existing);
        _pokemonList[index] = pokemon;

        Commit();
        PopulateFromSave();
    }
}

public readonly record struct PokemonBoxAdditionSummary(
    int RequestedCount,
    int AddedCount,
    int IncompatibleCount,
    int CapacitySkippedCount,
    int AvailableSlotCount);
