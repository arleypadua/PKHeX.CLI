using PKHeX.Core;
using PKHeX.Facade.Abstractions;
using PKHeX.Facade.Pokemons;

namespace PKHeX.Facade;

public class Trainer
{
    private readonly Game _game;

    public Trainer(Game game)
    {
        _game = game;

        Id = new EntityId(_game.SaveFile.DisplayTID, _game.SaveFile.DisplaySID);
        Editor = new TrainerEditor(_game);
        Money = new Money(_game);
        Inventories = new Inventories(_game);
        Party = new PokemonParty(_game);
        PokemonBox = new PokemonBox(_game);
    }

    public EntityId Id { get; }
    public TrainerEditor Editor { get; }
    public string Name => _game.SaveFile.OT;
    public Gender Gender => Gender.FromByte(_game.SaveFile.Gender);
    public TrainerPlayTime PlayTime => new(
        _game.SaveFile.PlayedHours,
        _game.SaveFile.PlayedMinutes,
        _game.SaveFile.PlayedSeconds);

    public Money Money { get; }
    public Inventories Inventories { get; private set; }
    public PokemonParty Party { get; private set; }
    public PokemonBox PokemonBox { get; private set; }

    public string? RivalName => _game.SaveFile switch
    {
        SAV1 gen1 => gen1.RivalName,
        SAV2 gen2 => gen2.RivalName,
        SAV3FRLG gen3 => gen3.RivalName,
        SAV4 gen4 => gen4.RivalName,
        SAV5B2W2 gen5 => gen5.RivalName,
        SAV7b gen7 => gen7.Misc.RivalName,
        SAV8BS gen8 => gen8.RivalName,
        _ => null
    };

    public void AddOrUpdate(UniqueId id, Pokemon pokemon, PokemonSource source)
    {
        IMutablePokemonCollection collection = source switch
        {
            PokemonSource.Box => PokemonBox,
            PokemonSource.Party => Party,
            _ => throw new InvalidOperationException($"{source} is not supported when updating pokemon"),
        };

        collection.AddOrUpdate(id, pokemon);
    }

    internal void Commit()
    {
        Party.Commit();
        PokemonBox.Commit();
    }
}
