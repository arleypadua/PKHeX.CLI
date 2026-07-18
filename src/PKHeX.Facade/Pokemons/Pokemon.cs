using PKHeX.Core;
using PKHeX.Facade.Repositories;

namespace PKHeX.Facade.Pokemons;

public class Pokemon(PKM pokemon, Game game)
{
    // for some reflection
    public Pokemon() : this(default!, default!)
    {
    }

    public UniqueId UniqueId => UniqueId.From(this);
    public PKM Pkm => pokemon;
    public Game Game => game;

    public ItemDefinition Ball
    {
        get => ItemRepository.GetItem(pokemon.Ball);
        set => pokemon.Ball = Convert.ToByte(value.Id);
    }

    public EntityId Id => new(Pkm.DisplayTID, Pkm.DisplaySID);
    public Owner Owner => new(pokemon);

    public uint PID => Pkm.PID;

    public GameVersionDefinition Version => GameVersionRepository.Instance.Get(Pkm.Version);

    public SpeciesDefinition Species
    {
        get => Game.SpeciesRepository.Get((Species)Pkm.Species);
        set => ChangeSpecies(value);
    }

    public PokemonTypes Types => new(pokemon);
    public string Nickname => pokemon.Nickname;

    public bool NicknameSet =>
        !pokemon.Nickname.Equals(Species.Name, StringComparison.InvariantCultureIgnoreCase);

    public int Level => pokemon.CurrentLevel;

    public uint Experience
    {
        get => pokemon.EXP;
        set => pokemon.EXP = value;
    }

    public PokemonNature Natures => new(pokemon);

    public PokemonForm Form => new(pokemon);

    public Stats EVs => Stats.EvFrom(pokemon);
    public Stats IVs => Stats.IvFrom(pokemon);
    public Stats BaseStats => Stats.BaseFrom(pokemon);
    public Stats? AVs => pokemon is IAwakened ? Stats.AvFrom(pokemon) : null;
    public PokemonMove Move1 => new(pokemon, PokemonMove.MoveIndex.Move1);
    public PokemonMove Move2 => new(pokemon, PokemonMove.MoveIndex.Move2);
    public PokemonMove Move3 => new(pokemon, PokemonMove.MoveIndex.Move3);
    public PokemonMove Move4 => new(pokemon, PokemonMove.MoveIndex.Move4);
    public Gender Gender
    {
        get => Gender.FromByte(pokemon.Gender);
        set => pokemon.SetGender(value.ToByte());
    }

    public bool IsShiny => pokemon.IsShiny;

    public ItemDefinition HeldItem
    {
        get => ItemRepository.GetItem(Convert.ToUInt16(pokemon.HeldItem));
        set => pokemon.HeldItem = value.Id;
    }

    public AbilityDefinition Ability
    {
        get => AbilityRepository.Instance.Get(pokemon.Ability);
        set => pokemon.Ability = value.Id;
    }

    public int Friendship
    {
        get => pokemon.CurrentFriendship;
        set => pokemon.CurrentFriendship = (byte)Math.Clamp(value, 0, 255);
    }

    public PokemonFlags Flags => new(pokemon);
    public MetConditions MetConditions => new(pokemon);
    public Egg Egg => new(pokemon);

    public Dictionary<PokemonMove.MoveIndex, PokemonMove> Moves => new()
    {
        { PokemonMove.MoveIndex.Move1, Move1 },
        { PokemonMove.MoveIndex.Move2, Move2 },
        { PokemonMove.MoveIndex.Move3, Move3 },
        { PokemonMove.MoveIndex.Move4, Move4 },
    };

    public void ChangeLevel(int level)
    {
        var clamped = Math.Clamp(level, 1, 100);
        pokemon.CurrentLevel = Convert.ToByte(clamped);
    }

    /// <summary>
    /// Changes species while normalizing species-dependent Core state.
    /// Identity, ownership, and met data remain unchanged. This does not make the Pokemon legal for its encounter history.
    /// </summary>
    public void ChangeSpecies(SpeciesDefinition species)
    {
        ArgumentNullException.ThrowIfNull(species);
        if (species.Species == SpeciesDefinition.None.Species || !Game.IsAwareOf(species.Species))
            throw new ArgumentOutOfRangeException(nameof(species), "The species is not available in this save format.");

        if (Pkm.Species == species.ShortId)
            return;

        var hasCustomNickname = NicknameSet;
        Pkm.Species = species.ShortId;
        Pkm.Form = 0;
        Pkm.ResetPartyStats();
        if (Pkm is ICombatPower combatPower)
            combatPower.ResetCP();

        if (!hasCustomNickname)
            Pkm.ClearNickname();
    }

    public void ChangeNickname(string nickname)
    {
        pokemon.SetNickname(nickname);
    }

    public void SetShiny(bool shiny)
    {
        pokemon.SetIsShiny(shiny);
    }

    public void ChangeMove(PokemonMove.MoveIndex moveIndex, MoveDefinition newMove)
    {
        ChangeMoves([
            moveIndex == PokemonMove.MoveIndex.Move1 ? newMove.Id : Move1.Move.Id,
            moveIndex == PokemonMove.MoveIndex.Move2 ? newMove.Id : Move2.Move.Id,
            moveIndex == PokemonMove.MoveIndex.Move3 ? newMove.Id : Move3.Move.Id,
            moveIndex == PokemonMove.MoveIndex.Move4 ? newMove.Id : Move4.Move.Id,
        ]);
    }

    public void ChangeMoves(IReadOnlyList<MoveDefinition> moves) =>
        ChangeMoves(moves.Select(move => move.Id).ToArray());

    private void ChangeMoves(IReadOnlyList<ushort> moveIds)
    {
        ushort MoveAt(int index) => index < moveIds.Count ? moveIds[index] : MoveDefinition.None.Id;
        var newMoveSet = new Moveset(MoveAt(0), MoveAt(1), MoveAt(2), MoveAt(3));

        if (newMoveSet.Move1 == MoveDefinition.None.Id && newMoveSet.Move2 == MoveDefinition.None.Id && newMoveSet.Move3 == MoveDefinition.None.Id && newMoveSet.Move4 == MoveDefinition.None.Id)
        {
            return;
        }

        pokemon.SetMoves(newMoveSet);
        pokemon.FixMoves();
    }

    public Pokemon MakeCopy()
    {
        var underlyingPkm = Pkm.Clone();
        underlyingPkm.ClearNickname();

        var isShiny = underlyingPkm.IsShiny;

        // re-roll the pid
        underlyingPkm.PID = EntityPID.GetRandomPID(Random.Shared, underlyingPkm.Species, underlyingPkm.Gender,
            underlyingPkm.Version, underlyingPkm.Nature, underlyingPkm.Form, underlyingPkm.PID);

        if (isShiny)
        {
            // because re-rolling may void the shiny status, we are making it shiny again
            underlyingPkm.SetIsShiny(true);
        }

        return new Pokemon(underlyingPkm, Game);
    }

    public Pokemon Clone() => new(Pkm.Clone(), Game);

    public void ApplyChangesFrom(Pokemon template, bool keepPid = true)
    {
        var pid = PID;
        template.Pkm.Data.CopyTo(Pkm.Data);

        if (keepPid)
        {
            Pkm.PID = pid;
        }
    }

    public File ToFile(bool encrypted = false)
    {
        var bytes = new byte[Pkm.SIZE_PARTY];
        if (encrypted)
        {
            Pkm.WriteEncryptedDataParty(bytes);
        }
        else
        {
            Pkm.WriteDecryptedDataParty(bytes);
        }

        return new File
        {
            Name = Pkm.FileName,
            Bytes = bytes
        };
    }

    public static Pokemon LoadFrom(
        byte[] bytes,
        Game? game = null)
    {
        var format =
            EntityFileExtension.GetContextFromExtension(string.Empty, game?.Generation ?? EntityContext.Gen6);
        var pkm = EntityFormat.GetFromBytes(bytes, prefer: format)
                  ?? throw new InvalidOperationException("The file did not load into a valid pokemon file.");

        var version = GameVersionRepository.Instance.Get(pkm.Version);

        return new Pokemon(pkm, game ?? Game.EmptyOf(version));
    }

    public class File
    {
        public required string Name { get; init; }
        public required byte[] Bytes { get; init; }
    }
}
