using PKHeX.Core;
using PKHeX.Facade.Abstractions;
using PKHeX.Facade.Pokemons;

namespace PKHeX.Facade;

public class PokemonParty(Game game) : IMutablePokemonCollection
{
    private readonly IList<PKM> _partyData = game.SaveFile.PartyData;
    public IList<Pokemon> Pokemons => _partyData
        .Select(pkm => new Pokemon(pkm, game))
        .ToList();

    public void Commit()
    {
        try
        {
            game.SaveFile.PartyData = _partyData;
        }
        catch (ArgumentOutOfRangeException)
        {
            if (!PartyMatchesSaveFile())
                throw;
        }
    }

    private bool PartyMatchesSaveFile()
    {
        var intendedParty = _partyData.Where(pokemon => pokemon.Species != 0).ToList();
        var savedParty = game.SaveFile.PartyData;
        if (savedParty.Count != intendedParty.Count)
            return false;

        for (var partyIndex = 0; partyIndex < savedParty.Count; partyIndex++)
        {
            if (savedParty[partyIndex].GetType() != intendedParty[partyIndex].GetType() ||
                !savedParty[partyIndex].Data.SequenceEqual(intendedParty[partyIndex].Data))
                return false;
        }

        return true;
    }

    public void AddOrUpdate(UniqueId id, Pokemon pokemon)
    {
        var existing = _partyData.FirstOrDefault(p => UniqueId.From(p).Equals(id));

        if (existing is null)
            throw new InvalidOperationException("Adding pokemons to the party is not supported.");

        var index = _partyData.IndexOf(existing);
        _partyData[index] = pokemon.Pkm;

        Commit();
    }
}
