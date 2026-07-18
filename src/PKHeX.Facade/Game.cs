using PKHeX.Core;
using PKHeX.Facade.Repositories;

namespace PKHeX.Facade;

public class Game
{
    public readonly SaveFile SaveFile;

    public Game(SaveFile saveFile)
    {
        SaveFile = saveFile;
        SpeciesRepository = new SpeciesRepository(this);
        PokemonRepository = new PokemonRepository(this);
        LocationRepository = new LocationRepository(this);
        ItemRepository = new ItemRepository(saveFile);

        Trainer = new Trainer(this);
        BattlePoints = BattlePoints.GetInstance(saveFile);
        Pokedex = new Pokedex(this);
    }

    public SpeciesRepository SpeciesRepository { get; }
    public PokemonRepository PokemonRepository { get; }
    public LocationRepository LocationRepository { get; }
    public ItemRepository ItemRepository { get; }
    public Trainer Trainer { get; }
    public BattlePoints BattlePoints { get; }
    public Pokedex Pokedex { get; }

    public GameVersionDefinition SaveVersion => GameVersionRepository.Instance.Get(SaveFile.Version);

    /**
     * Sometimes it is not really possible to pinpoint which version a save file is
     * This will give a guess approximation on which actual version the game relates to
     *
     * If the save file yields an actual game version, it will be returned instead of an approximation.
     */
    public GameVersionDefinition GameVersionApproximation => SaveVersion.Aggregated
        ? GameVersionRepository.Instance.Get(SaveFile.Context.GetSingleGameVersion())
        : SaveVersion;

    public EntityContext Generation => SaveFile.Context;

    public bool IsAwareOf(Species species, byte form = 0) =>
        SaveFile.Personal.IsPresentInGame((ushort)species, form);

    public Game CreateDraft()
    {
        var draftSaveFile = SaveFile.Clone();
        var pendingBox = Trainer.PokemonBox.All;
        for (var boxIndex = 0; boxIndex < pendingBox.Count; boxIndex++)
            draftSaveFile.SetBoxSlotAtIndex(pendingBox[boxIndex].Pkm.Clone(), boxIndex, EntityImportSettings.None);

        var pendingParty = Trainer.Party.Pokemons;
        for (var partyIndex = 0; partyIndex < pendingParty.Count; partyIndex++)
            draftSaveFile.SetPartySlotAtIndex(pendingParty[partyIndex].Pkm.Clone(), partyIndex, EntityImportSettings.None);

        return new Game(draftSaveFile);
    }

    public byte[] ToByteArray()
    {
        // make sure pending changes make its way to the bytes of the save
        Trainer.Commit();

        return Serialize(SaveFile);
    }

    internal byte[] SerializeWithoutTrainerCommit() => Serialize(SaveFile);

    internal static byte[] Serialize(SaveFile saveFile)
    {
        if (saveFile is SAV7b letsGoSaveFile) letsGoSaveFile.FixStoragePreWrite();

        return saveFile.Write(
            setting: saveFile.Metadata.GetSuggestedFlags(Path.GetExtension(saveFile.Metadata.FileName))
        ).ToArray();
    }

    public static Game LoadFrom(string path)
    {
        var saveFile = SaveUtil.GetSaveFile(path)
                       ?? throw new GameNotLoadedException(path);

        return new Game(saveFile);
    }

    public static Game LoadFrom(byte[] bytes, string? path = null)
    {
        var saveFile = SaveUtil.GetSaveFile(bytes, path)
                       ?? throw new GameNotLoadedException(path);

        return new Game(saveFile);
    }

    public static Game EmptyOf(
        GameVersionDefinition version,
        string? trainerName = null) => new(BlankSaveFile.Get(version.Version, trainerName ?? "PKHeXWeb"));
}

public class GameNotLoadedException(string? path = null)
    : Exception($"The file {path ?? "N/A"} did not load into a save file.");
