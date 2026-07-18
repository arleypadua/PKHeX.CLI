using PKHeX.Core;
using PKHeX.Facade.Extensions;
using PKHeX.Facade.Pokemons;

namespace PKHeX.Facade.Repositories;

public class MoveRepository
{
    public static readonly MoveRepository Instance = new();

    private readonly Dictionary<ushort, MoveDefinition> _moves;
    private readonly Dictionary<string, MoveDefinition> _movesByName;

    private MoveRepository()
    {
        _moves = GameInfo.Strings.movelist
            .Select((moveName, id) => (id: Convert.ToUInt16(id), moveName))
            .ToDictionary(x => Convert.ToUInt16(x.id), x => new MoveDefinition(Convert.ToUInt16(x.id), x.moveName));
        _movesByName = _moves.Values
            .Where(move => move != MoveDefinition.None)
            .ToDictionary(move => NormalizeName(move.Name), StringComparer.OrdinalIgnoreCase);
    }

    public MoveDefinition GetMove(ushort id) => _moves[id];

    public List<MoveDefinition> PossibleMovesFor(Pokemon pokemon)
    {
        var legalMoveInfo = new LegalMoveInfo();
        legalMoveInfo.ReloadMoves(pokemon.Legality());

        return _moves
            .Where(move => legalMoveInfo.CanLearn(move.Key))
            .Select(x => x.Value)
            .OrderBy(x => x.Name)
            .ToList();
    }

    public IReadOnlyList<MoveDefinition> SuggestedMovesFor(Pokemon pokemon)
    {
        Span<ushort> suggestedMoveIds = stackalloc ushort[4];
        pokemon.Pkm.GetMoveSet(suggestedMoveIds);
        var legalMoveIds = PossibleMovesFor(pokemon).Select(move => move.Id).ToHashSet();

        return suggestedMoveIds
            .ToArray()
            .Where(moveId => moveId != MoveDefinition.None.Id && legalMoveIds.Contains(moveId))
            .Distinct()
            .Select(GetMove)
            .ToArray();
    }

    public bool TryGetMoveByName(string moveName, out MoveDefinition move)
    {
        var normalizedName = NormalizeName(moveName);
        if (normalizedName.StartsWith("hiddenpower", StringComparison.Ordinal))
            normalizedName = "hiddenpower";

        return _movesByName.TryGetValue(normalizedName, out move!);
    }

    private static string NormalizeName(string name) =>
        string.Concat(name.Where(char.IsLetterOrDigit)).ToLowerInvariant();
}

public record MoveDefinition(ushort Id, string Name)
{
    public static readonly MoveDefinition None = new((ushort)Move.None, $"({Move.None})");

    public virtual bool Equals(MoveDefinition? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
};
