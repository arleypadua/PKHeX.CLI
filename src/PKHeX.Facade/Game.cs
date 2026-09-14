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
    }

    public SpeciesRepository SpeciesRepository { get; }
    public PokemonRepository PokemonRepository { get; }
    public LocationRepository LocationRepository { get; }
    public ItemRepository ItemRepository { get; }
    public Trainer Trainer { get; }
    public BattlePoints BattlePoints { get; }

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

    public byte[] ToByteArray()
    {
        // make sure pending changes make its way to the bytes of the save
        Trainer.Commit();

        if (SaveFile is SAV7b _7b) _7b.FixStoragePreWrite();

        return SaveFile.Write(
            setting: SaveFile.Metadata.GetSuggestedFlags(Path.GetExtension(SaveFile.Metadata.FileName))
        ).ToArray();
    }

    public static Game LoadFrom(string path) =>
        LoadFrom(() => SaveUtil.GetSaveFile(path), path);

    public static Game LoadFrom(byte[] bytes, string? path = null) =>
        LoadFrom(() => SaveUtil.GetSaveFile(bytes, path), path);

    /**
     * A save format is picked by file size before anything is parsed, so a file that merely matches a
     * known size is decoded as if it were the real thing. Its contents then fail in whatever way the
     * garbage happens to break first - an unknown species id out of a repository, an offset past the
     * end of a buffer, and so on.
     *
     * All of those mean the same thing to a caller: this file is not a save we can load. Reporting them
     * as GameNotLoadedException gives callers one failure to handle, and keeps the original cause as
     * the inner exception so a real decoding bug is still diagnosable.
     */
    private static Game LoadFrom(Func<SaveFile?> getSaveFile, string? path)
    {
        try
        {
            var saveFile = getSaveFile()
                           ?? throw new GameNotLoadedException(path);

            return new Game(saveFile);
        }
        catch (Exception e) when (e is not (
            GameNotLoadedException or IOException or UnauthorizedAccessException or OutOfMemoryException))
        {
            throw new GameNotLoadedException(path, e);
        }
    }

    public static Game EmptyOf(
        GameVersionDefinition version,
        string? trainerName = null) => new(BlankSaveFile.Get(version.Version, trainerName ?? "PKHeXWeb"));
}

public class GameNotLoadedException(string? path = null, Exception? innerException = null)
    : Exception($"The file {path ?? "N/A"} did not load into a save file.", innerException);