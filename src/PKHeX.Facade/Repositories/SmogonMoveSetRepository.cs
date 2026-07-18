using System.Globalization;
using System.Text;
using System.Text.Json;
using PKHeX.Core;
using PKHeX.Facade.Pokemons;

namespace PKHeX.Facade.Repositories;

// dry-new: Competitive presets come from an external document and have no existing repository abstraction.
public sealed class SmogonMoveSetRepository(HttpClient httpClient)
{
    private const string DataBaseUri = "https://pkmn.github.io/smogon/data/sets/";

    public async Task<MoveSetSuggestion> SuggestMovesFor(
        Pokemon pokemon,
        CancellationToken cancellationToken = default)
    {
        var legalSuggestions = MoveRepository.Instance.SuggestedMovesFor(pokemon);
        var documents = await Task.WhenAll(FormatIdsFor(pokemon).Select(formatId =>
            FetchFormat(formatId, cancellationToken)));

        try
        {
            foreach (var document in documents)
            {
                if (document is null)
                    continue;

                var competitiveMoves = FindCompetitiveMoves(document.RootElement, pokemon);
                if (competitiveMoves.Count == 0)
                    continue;

                var completedMoves = CompleteWithLegalSuggestions(competitiveMoves, legalSuggestions);
                return new MoveSetSuggestion(completedMoves, true);
            }
        }
        finally
        {
            foreach (var document in documents)
                document?.Dispose();
        }

        return new MoveSetSuggestion(legalSuggestions, false);
    }

    private async Task<JsonDocument?> FetchFormat(
        string formatId,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync(
                $"{DataBaseUri}{formatId}.json",
                cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            await using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    private static IReadOnlyList<MoveDefinition> FindCompetitiveMoves(
        JsonElement root,
        Pokemon pokemon)
    {
        if (root.ValueKind != JsonValueKind.Object)
            return [];

        var legalMoveIds = MoveRepository.Instance.PossibleMovesFor(pokemon)
            .Select(move => move.Id)
            .ToHashSet();
        var formName = pokemon.Form.Form.Name;
        var showdownFormName = ShowdownParsing.GetShowdownFormName((ushort)pokemon.Species.Id, formName);
        if (!string.IsNullOrWhiteSpace(showdownFormName) &&
            TryGetSpeciesSets(root, $"{pokemon.Species.Name}-{showdownFormName}", out var formSets))
            return FindLegalMovesInSets(formSets, legalMoveIds);

        return TryGetSpeciesSets(root, pokemon.Species.Name, out var baseSets)
            ? FindLegalMovesInSets(baseSets, legalMoveIds)
            : [];
    }

    private static bool TryGetSpeciesSets(
        JsonElement root,
        string speciesName,
        out JsonElement sets)
    {
        if (root.TryGetProperty(speciesName, out sets))
            return true;

        var normalizedSpeciesName = NormalizeSpeciesName(speciesName);
        foreach (var property in root.EnumerateObject())
        {
            if (NormalizeSpeciesName(property.Name) != normalizedSpeciesName)
                continue;

            sets = property.Value;
            return true;
        }

        sets = default;
        return false;
    }

    private static string NormalizeSpeciesName(string speciesName)
    {
        var normalizedName = new StringBuilder(speciesName.Length);
        foreach (var character in speciesName.Normalize(NormalizationForm.FormD))
        {
            if (character is '♀' or '♂')
                normalizedName.Append(character == '♀' ? 'f' : 'm');
            else if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark &&
                     char.IsLetterOrDigit(character))
                normalizedName.Append(char.ToLowerInvariant(character));
        }

        return normalizedName.ToString();
    }

    private static IReadOnlyList<MoveDefinition> FindLegalMovesInSets(
        JsonElement sets,
        IReadOnlySet<ushort> legalMoveIds)
    {
        if (sets.ValueKind != JsonValueKind.Object)
            return [];

        foreach (var set in sets.EnumerateObject())
        {
            if (set.Value.ValueKind != JsonValueKind.Object ||
                !set.Value.TryGetProperty("moves", out var moveSlots) ||
                moveSlots.ValueKind != JsonValueKind.Array)
                continue;

            var selectedMoves = SelectLegalMoves(moveSlots, legalMoveIds);
            if (selectedMoves.Count > 0)
                return selectedMoves;
        }

        return [];
    }

    private static IReadOnlyList<MoveDefinition> SelectLegalMoves(
        JsonElement moveSlots,
        IReadOnlySet<ushort> legalMoveIds)
    {
        var selectedMoves = new List<MoveDefinition>(4);
        foreach (var moveSlot in moveSlots.EnumerateArray())
        {
            foreach (var moveName in MoveNamesFrom(moveSlot))
            {
                if (!MoveRepository.Instance.TryGetMoveByName(moveName, out var move) ||
                    !legalMoveIds.Contains(move.Id) ||
                    selectedMoves.Contains(move))
                    continue;

                selectedMoves.Add(move);
                break;
            }
        }

        return selectedMoves;
    }

    private static IEnumerable<string> MoveNamesFrom(JsonElement moveSlot)
    {
        if (moveSlot.ValueKind == JsonValueKind.String)
        {
            yield return moveSlot.GetString()!;
            yield break;
        }

        if (moveSlot.ValueKind != JsonValueKind.Array)
            yield break;

        foreach (var alternative in moveSlot.EnumerateArray())
        {
            if (alternative.ValueKind == JsonValueKind.String)
                yield return alternative.GetString()!;
        }
    }

    private static IReadOnlyList<MoveDefinition> CompleteWithLegalSuggestions(
        IReadOnlyList<MoveDefinition> competitiveMoves,
        IReadOnlyList<MoveDefinition> legalSuggestions) => competitiveMoves
        .Concat(legalSuggestions)
        .Distinct()
        .Take(4)
        .ToArray();

    private static IEnumerable<string> FormatIdsFor(Pokemon pokemon)
    {
        var generation = pokemon.Game.SaveFile.Generation;
        if (pokemon.Game.SaveFile.Context == EntityContext.Gen8b)
        {
            yield return "gen8bdspou";
            yield return "gen8bdspubers";
            yield break;
        }

        if (pokemon.Game.SaveFile.Context == EntityContext.Gen7b)
        {
            yield return "gen7letsgoou";
            yield break;
        }

        yield return $"gen{generation}ou";
        yield return $"gen{generation}ubers";
    }
}

public sealed record MoveSetSuggestion(
    IReadOnlyList<MoveDefinition> Moves,
    bool UsedCompetitivePreset);
