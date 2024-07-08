﻿using PKHeX.Facade.Repositories;
using PKHeX.Core;

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
    }

    public SpeciesRepository SpeciesRepository { get; }
    public PokemonRepository PokemonRepository { get; }
    public LocationRepository LocationRepository { get; }
    public ItemRepository ItemRepository { get; }
    public Trainer Trainer { get; }

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

        return SaveFile.Write(
            setting: SaveFile.Metadata.GetSuggestedFlags(Path.GetExtension(SaveFile.Metadata.FileName))
        );
    }

    public static Game LoadFrom(string path)
    {
        var saveFile = SaveUtil.GetVariantSAV(path)
                       ?? throw new InvalidOperationException($"The file at {path} did not load into a save file.");

        return new Game(saveFile);
    }

    public static Game LoadFrom(byte[] bytes, string? path = null)
    {
        var saveFile = SaveUtil.GetVariantSAV(bytes, path)
                       ?? throw new InvalidOperationException($"The file at {path} did not load into a save file.");

        return new Game(saveFile);
    }
}