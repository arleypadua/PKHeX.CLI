using AwesomeAssertions;
using PKHeX.Core;
using PKHeX.Facade.Extensions;
using PKHeX.Facade.Repositories;

namespace PKHeX.Facade.Tests;

public class MoveRepositoryTests
{
    [Theory]
    [SupportedSaveFiles]
    public void ShouldReturnEveryMoveThatPkhexMarksLearnable(string saveFile)
    {
        var pokemon = Game.LoadFrom(saveFile).Trainer.Party.Pokemons.First();
        var legality = pokemon.Legality();
        var legalMoveInfo = new LegalMoveInfo();
        legalMoveInfo.ReloadMoves(legality);

        var expectedMoveIds = Enumerable.Range(1, (int)Move.MAX_COUNT)
            .Select(Convert.ToUInt16)
            .Where(legalMoveInfo.CanLearn)
            .ToHashSet();

        MoveRepository.Instance.PossibleMovesFor(pokemon)
            .Select(move => move.Id)
            .Should().BeEquivalentTo(expectedMoveIds);
    }

    [Theory]
    [Games(GameVersion.GP)]
    public void ShouldSuggestUniqueLegalMoves(Game game)
    {
        var pokemon = game.Trainer.Party.Pokemons.First();
        var possibleMoveIds = MoveRepository.Instance.PossibleMovesFor(pokemon)
            .Select(move => move.Id)
            .ToHashSet();

        var suggestedMoves = MoveRepository.Instance.SuggestedMovesFor(pokemon);

        suggestedMoves.Should().OnlyHaveUniqueItems();
        suggestedMoves.Should().HaveCountLessThanOrEqualTo(4);
        suggestedMoves.Should().OnlyContain(move => possibleMoveIds.Contains(move.Id));
    }
}
