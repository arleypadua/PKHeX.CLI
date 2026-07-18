using PKHeX.Core;

namespace PKHeX.Facade.Pokemons;

/// <summary>
/// Represents the stats of a Pokemon
/// </summary>
/// <param name="Pokemon">The Pokemon in which we are seeing the statuses</param>
/// <param name="Type">Type of status</param>
/// <param name="VirtualStats">
/// Sometimes stats are not persisted in the save file, but can be calculated at runtime.
/// When that's the case, it is a virtual stats, and any changes won't take effect
/// </param>
public record Stats(PKM Pokemon, Stats.StatsType Type, bool VirtualStats = false)
{
    private IAwakened? _awakened => Pokemon is IAwakened awakened ? awakened : null;

    public int Attack
    {
        get => Type switch
        {
            StatsType.Base => Pokemon.Stat_ATK,
            StatsType.EV => Pokemon.EV_ATK,
            StatsType.IV => Pokemon.IV_ATK,
            StatsType.AV => _awakened?.AV_ATK ?? 0,
            _ => throw new InvalidOperationException($"stats type {Type} not supported"),
        };
        set => _ = Type switch
        {
            StatsType.Base => Pokemon.Stat_ATK = value,
            StatsType.EV => Pokemon.EV_ATK = value,
            StatsType.IV => Pokemon.IV_ATK = value,
            StatsType.AV => _awakened!.AV_ATK = (byte)value,
            _ => throw new InvalidOperationException($"stats type {Type} not supported"),
        };
    }

    public int Defense
    {
        get => Type switch
        {
            StatsType.Base => Pokemon.Stat_DEF,
            StatsType.EV => Pokemon.EV_DEF,
            StatsType.IV => Pokemon.IV_DEF,
            StatsType.AV => _awakened?.AV_DEF ?? 0,
            _ => throw new InvalidOperationException($"stats type {Type} not supported"),
        };
        set => _ = Type switch
        {
            StatsType.Base => Pokemon.Stat_DEF = value,
            StatsType.EV => Pokemon.EV_DEF = value,
            StatsType.IV => Pokemon.IV_DEF = value,
            StatsType.AV => _awakened!.AV_DEF = (byte)value,
            _ => throw new InvalidOperationException($"stats type {Type} not supported"),
        };
    }

    public int SpecialAttack
    {
        get => Type switch
        {
            StatsType.Base => Pokemon.Stat_SPA,
            StatsType.EV => Pokemon.EV_SPA,
            StatsType.IV => Pokemon.IV_SPA,
            StatsType.AV => _awakened?.AV_SPA ?? 0,
            _ => throw new InvalidOperationException($"stats type {Type} not supported"),
        };
        set => _ = Type switch
        {
            StatsType.Base => Pokemon.Stat_SPA = value,
            StatsType.EV => Pokemon.EV_SPA = value,
            StatsType.IV => Pokemon.IV_SPA = value,
            StatsType.AV => _awakened!.AV_SPA = (byte)value,
            _ => throw new InvalidOperationException($"stats type {Type} not supported"),
        };
    }

    public int SpecialDefense
    {
        get => Type switch
        {
            StatsType.Base => Pokemon.Stat_SPD,
            StatsType.EV => Pokemon.EV_SPD,
            StatsType.IV => Pokemon.IV_SPD,
            StatsType.AV => _awakened?.AV_SPD ?? 0,
            _ => throw new InvalidOperationException($"stats type {Type} not supported"),
        };
        set => _ = Type switch
        {
            StatsType.Base => Pokemon.Stat_SPD = value,
            StatsType.EV => Pokemon.EV_SPD = value,
            StatsType.IV => Pokemon.IV_SPD = value,
            StatsType.AV => _awakened!.AV_SPD = (byte)value,
            _ => throw new InvalidOperationException($"stats type {Type} not supported"),
        };
    }

    public int Health
    {
        get => Type switch
        {
            StatsType.Base => Pokemon.Stat_HPMax,
            StatsType.EV => Pokemon.EV_HP,
            StatsType.IV => Pokemon.IV_HP,
            StatsType.AV => _awakened?.AV_HP ?? 0,
            _ => throw new InvalidOperationException($"stats type {Type} not supported"),
        };
        set => _ = Type switch
        {
            StatsType.Base => Pokemon.Stat_HPMax = value,
            StatsType.EV => Pokemon.EV_HP = value,
            StatsType.IV => Pokemon.IV_HP = value,
            StatsType.AV => _awakened!.AV_HP = (byte)value,
            _ => throw new InvalidOperationException($"stats type {Type} not supported"),
        };
    }

    public int Speed
    {
        get => Type switch
        {
            StatsType.Base => Pokemon.Stat_SPE,
            StatsType.EV => Pokemon.EV_SPE,
            StatsType.IV => Pokemon.IV_SPE,
            StatsType.AV => _awakened?.AV_SPE ?? 0,
            _ => throw new InvalidOperationException($"stats type {Type} not supported"),
        };
        set => _ = Type switch
        {
            StatsType.Base => Pokemon.Stat_SPE = value,
            StatsType.EV => Pokemon.EV_SPE = value,
            StatsType.IV => Pokemon.IV_SPE = value,
            StatsType.AV => _awakened!.AV_SPE = (byte)value,
            _ => throw new InvalidOperationException($"stats type {Type} not supported"),
        };
    }

    public int? CombatPower
    {
        get
        {
            if (Type is not StatsType.Base) return null;
            return Pokemon is ICombatPower combatPower
                ? combatPower.Stat_CP
                : null;
        }
        set
        {
            if (Type is not StatsType.Base) throw new InvalidOperationException($"stats type {Type} not supported");
            if (Pokemon is ICombatPower combatPower)
            {
                combatPower.Stat_CP = value ?? 0;
            }
        }
    }

    public int Total => Attack + Defense + SpecialAttack + SpecialDefense + Health + Speed;

    /// <summary>
    /// Gets the maximum value representable by this Pokemon's format for the selected stat type.
    /// </summary>
    public int MaximumValue => Type switch
    {
        StatsType.EV => Pokemon.MaxEV,
        StatsType.IV => Pokemon.MaxIV,
        _ => throw new InvalidOperationException($"stats type {Type} does not expose a maximum value"),
    };

    /// <summary>
    /// Applies PKHeX Core's format-aware maximum EV distribution.
    /// The distribution respects aggregate EV limits but does not guarantee encounter legality.
    /// </summary>
    public void SetToMaximum()
    {
        if (Type is not StatsType.EV)
            throw new InvalidOperationException($"stats type {Type} does not support maximum EV distribution");

        Span<int> effortValues = stackalloc int[6];
        EffortValues.SetMax(effortValues, Pokemon);
        Pokemon.SetEVs(effortValues);
    }

    public enum StatsType
    {
        Base,
        EV,
        IV,
        AV,
    }

    public static Stats EvFrom(PKM pokemon) => new(pokemon, StatsType.EV);
    public static Stats IvFrom(PKM pokemon) => new(pokemon, StatsType.IV);
    public static Stats AvFrom(PKM pokemon) => new(pokemon, StatsType.AV);

    public static Stats BaseFrom(PKM pokemon)
    {
        var virtualStats = !pokemon.PartyStatsPresent;
        if (virtualStats)
        {
            var pokemonToBeUsed = pokemon.Clone();
            pokemonToBeUsed.ResetPartyStats();
            return new Stats(pokemonToBeUsed, StatsType.Base, true);
        }

        return new Stats(pokemon, StatsType.Base, virtualStats);
    }
}
