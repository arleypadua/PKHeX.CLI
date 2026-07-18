using PKHeX.Core;
using PKHeX.Facade.Repositories;

namespace PKHeX.Facade;

// dry-new: Core exposes format-specific dex APIs; this facade is the stable UI-facing capability boundary.
public sealed class Pokedex(Game game)
{
    private const string FormCapabilityMessage = "Form-specific Pokédex seen/caught editing is not supported for this save format.";
    private readonly Game _game = game;

    public PokedexCapabilities Capabilities => SupportsNativeSeenCaught(_game.SaveFile)
        ? PokedexCapabilities.SpeciesSeenCaught
        : PokedexCapabilities.Unsupported;

    public bool SupportsFormSeenCaught => Capabilities.SupportsFormSeenCaught;

    public bool SupportsSpeciesSeenCaught(SpeciesDefinition? species) =>
        species is not null &&
        species.Species != Species.None &&
        SupportsNativeSeenCaught(_game.SaveFile) &&
        _game.SaveFile.Personal.IsPresentInGame(species.ShortId, 0) &&
        HasNativeDexEntry(_game.SaveFile, species.ShortId);

    public PokedexState GetState(SpeciesDefinition species) => GetState(species, FormDefinition.Default);

    public PokedexState GetState(SpeciesDefinition species, FormDefinition form)
    {
        ArgumentNullException.ThrowIfNull(species);
        EnsureSpeciesOperationSupported(form);
        ValidateSpecies(species);
        return ReadState(_game.SaveFile, species.ShortId);
    }

    public void SetState(SpeciesDefinition species, PokedexState state)
    {
        ArgumentNullException.ThrowIfNull(species);
        EnsureSpeciesOperationSupported(FormDefinition.Default);
        ValidateSpecies(species);
        ValidateState(state);

        var candidateGame = _game.CreateDraft();
        ApplyState(candidateGame.SaveFile, species.ShortId, state);

        var serializedCandidate = candidateGame.SerializeWithoutTrainerCommit();
        var persistedGame = Game.LoadFrom(serializedCandidate, candidateGame.SaveFile.Metadata.FilePath);
        var persistedState = ReadState(persistedGame.SaveFile, species.ShortId);
        if (persistedState != state)
            throw new InvalidOperationException($"Pokédex state did not survive save serialization. Expected {state}; reloaded {persistedState}.");

        _game.SaveFile.CopyChangesFrom(candidateGame.SaveFile);
        if (ReadState(_game.SaveFile, species.ShortId) != state)
            throw new InvalidOperationException("Pokédex state could not be applied to the active game.");
    }

    private void EnsureSpeciesOperationSupported(FormDefinition form)
    {
        if (!SupportsNativeSeenCaught(_game.SaveFile))
            throw new PokedexOperationNotSupportedException("Species seen/caught editing is not supported for this save format.");
        if (form.Id != FormDefinition.Default.Id)
            throw new PokedexOperationNotSupportedException(FormCapabilityMessage);
    }

    private void ValidateSpecies(SpeciesDefinition species)
    {
        if (species.Species == Species.None || !_game.SaveFile.Personal.IsPresentInGame(species.ShortId, 0))
            throw new ArgumentOutOfRangeException(nameof(species), "The species is not available in this save format.");
        if (_game.SaveFile is SAV8SWSH swordShield && !swordShield.Zukan.GetEntry(species.ShortId, out _))
            throw new PokedexOperationNotSupportedException("The species has no Pokédex entry in this save format.");
    }

    private static void ValidateState(PokedexState state)
    {
        if (state.IsCaught && !state.IsSeen)
            throw new ArgumentException("A caught Pokédex entry must also be seen.", nameof(state));
    }

    private static PokedexState ReadState(SaveFile saveFile, ushort species) =>
        new(saveFile.GetSeen(species), saveFile.GetCaught(species));

    private static bool SupportsNativeSeenCaught(SaveFile saveFile) => saveFile is
        SAV1 or SAV2 or SAV3 or SAV4 or SAV7b or SAV8SWSH;

    private static bool HasNativeDexEntry(SaveFile saveFile, ushort species) => saveFile is not SAV8SWSH swordShield ||
        swordShield.Zukan.GetEntry(species, out _);

    private static void ApplyState(SaveFile saveFile, ushort species, PokedexState state)
    {
        switch (saveFile)
        {
            case SAV1 or SAV2 or SAV3:
                saveFile.SetSeen(species, state.IsSeen);
                saveFile.SetCaught(species, state.IsCaught);
                return;
            case SAV4 generation4:
                if (!state.IsSeen)
                    generation4.Dex.ClearSeen(species);
                else
                    generation4.Dex.ModifyAll(species, Zukan4.SetDexArgs.SeenAll);
                generation4.Dex.SetCaught(species, state.IsCaught);
                return;
            case SAV7b letsGo:
                letsGo.Zukan.SetSeen(species, state.IsSeen);
                letsGo.Zukan.SetCaught(species, state.IsCaught);
                return;
            case SAV8SWSH swordShield:
                if (!state.IsSeen)
                    swordShield.Zukan.ClearDexEntryAll(species);
                else
                {
                    var gender = swordShield.Personal[species].RandomGender() & 1;
                    var language = swordShield.Language > 0 ? swordShield.Language : (int)LanguageID.English;
                    swordShield.Zukan.SetSeenRegion(species, 0, gender, true);
                    swordShield.Zukan.SetFormDisplayed(species);
                    swordShield.Zukan.SetGenderDisplayed(species, (uint)gender);
                    swordShield.Zukan.SetIsLanguageObtained(species, language, state.IsCaught);
                }
                swordShield.Zukan.SetCaught(species, state.IsCaught);
                return;
            default:
                throw new PokedexOperationNotSupportedException("Species seen/caught editing is not supported for this save format.");
        }
    }
}

public sealed record PokedexCapabilities(bool SupportsSpeciesSeenCaught, bool SupportsFormSeenCaught)
{
    public static PokedexCapabilities Unsupported { get; } = new(false, false);
    public static PokedexCapabilities SpeciesSeenCaught { get; } = new(true, false);
}

public readonly record struct PokedexState(bool IsSeen, bool IsCaught)
{
    public static PokedexState NotSeen { get; } = new(false, false);
    public static PokedexState Seen { get; } = new(true, false);
    public static PokedexState Caught { get; } = new(true, true);
}

public sealed class PokedexOperationNotSupportedException(string message) : InvalidOperationException(message);
