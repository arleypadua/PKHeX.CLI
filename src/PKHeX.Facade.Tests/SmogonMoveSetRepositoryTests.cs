using System.Net;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using PKHeX.Core;
using PKHeX.Facade.Repositories;

namespace PKHeX.Facade.Tests;

public class SmogonMoveSetRepositoryTests
{
    [Theory]
    [Games(GameVersion.GP)]
    public async Task ShouldChooseLegalMovesFromCompetitiveAlternatives(Game game)
    {
        var pokemon = game.Trainer.Party.Pokemons.First();
        var legalMoves = MoveRepository.Instance.PossibleMovesFor(pokemon).Take(4).ToArray();
        var responseBody = CompetitiveResponseFor(pokemon.Species.Name,
        [
            legalMoves[0].Name,
            new[] { "Definitely Not A Move", legalMoves[1].Name },
            legalMoves[2].Name,
            legalMoves[3].Name,
        ]);
        var repository = RepositoryReturning(HttpStatusCode.OK, responseBody);

        var suggestion = await repository.SuggestMovesFor(pokemon);

        suggestion.UsedCompetitivePreset.Should().BeTrue();
        suggestion.Moves.Select(move => move.Id)
            .Should().Equal(legalMoves.Select(move => move.Id));
    }

    [Theory]
    [Games(GameVersion.GP)]
    public async Task ShouldFallBackToLegalSuggestionsWhenCompetitiveDataIsUnavailable(Game game)
    {
        var pokemon = game.Trainer.Party.Pokemons.First();
        var repository = RepositoryReturning(HttpStatusCode.NotFound, string.Empty);

        var suggestion = await repository.SuggestMovesFor(pokemon);

        suggestion.UsedCompetitivePreset.Should().BeFalse();
        suggestion.Moves.Should().Equal(MoveRepository.Instance.SuggestedMovesFor(pokemon));
    }

    [Theory]
    [Games(GameVersion.GP)]
    public async Task ShouldFallBackWhenCompetitiveDocumentHasInvalidStructure(Game game)
    {
        var pokemon = game.Trainer.Party.Pokemons.First();
        var responseBody = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            [pokemon.Species.Name] = Array.Empty<object>(),
        });
        var repository = RepositoryReturning(HttpStatusCode.OK, responseBody);

        var suggestion = await repository.SuggestMovesFor(pokemon);

        suggestion.UsedCompetitivePreset.Should().BeFalse();
        suggestion.Moves.Should().Equal(MoveRepository.Instance.SuggestedMovesFor(pokemon));
    }

    [Theory]
    [Games(GameVersion.GP)]
    public async Task ShouldFallBackWhenCompetitiveRequestTimesOut(Game game)
    {
        var pokemon = game.Trainer.Party.Pokemons.First();
        var repository = RepositoryUsing(new ThrowingHttpMessageHandler(
            new TaskCanceledException("The request timed out.", new TimeoutException())));

        var suggestion = await repository.SuggestMovesFor(pokemon);

        suggestion.UsedCompetitivePreset.Should().BeFalse();
        suggestion.Moves.Should().Equal(MoveRepository.Instance.SuggestedMovesFor(pokemon));
    }

    [Theory]
    [Games(GameVersion.GP)]
    public async Task ShouldNotUseBaseSpeciesWhenFormSpecificSetsExist(Game game)
    {
        var pokemon = game.Trainer.Party.Pokemons.First();
        var formName = pokemon.Form.Form.Name;
        formName.Should().NotBeNullOrWhiteSpace();
        var legalMoves = MoveRepository.Instance.PossibleMovesFor(pokemon).Take(4).ToArray();
        var responseBody = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            [$"{pokemon.Species.Name}-{formName}"] = new Dictionary<string, object>
            {
                ["Form Set"] = new { moves = new object[] { "Definitely Not A Move" } },
            },
            [pokemon.Species.Name] = new Dictionary<string, object>
            {
                ["Base Set"] = new { moves = legalMoves.Select(move => move.Name).ToArray() },
            },
        });
        var repository = RepositoryReturning(HttpStatusCode.OK, responseBody);

        var suggestion = await repository.SuggestMovesFor(pokemon);

        suggestion.UsedCompetitivePreset.Should().BeFalse();
        suggestion.Moves.Should().Equal(MoveRepository.Instance.SuggestedMovesFor(pokemon));
    }

    [Theory]
    [Games(GameVersion.GP)]
    public async Task ShouldMatchCanonicalSpeciesAndFormKeys(Game game)
    {
        var pokemon = game.Trainer.Party.Pokemons.First();
        var formName = pokemon.Form.Form.Name;
        formName.Should().NotBeNullOrWhiteSpace();
        var legalMoves = MoveRepository.Instance.PossibleMovesFor(pokemon).Take(4).ToArray();
        var responseBody = CompetitiveResponseFor(
            $"{pokemon.Species.Name} {formName}".ToUpperInvariant(),
            legalMoves.Select(move => (object)move.Name).ToArray());
        var repository = RepositoryReturning(HttpStatusCode.OK, responseBody);

        var suggestion = await repository.SuggestMovesFor(pokemon);

        suggestion.UsedCompetitivePreset.Should().BeTrue();
        suggestion.Moves.Select(move => move.Id)
            .Should().Equal(legalMoves.Select(move => move.Id));
    }

    [Theory]
    [Games(GameVersion.GP)]
    public async Task ShouldOnlyRequestFormatsForTheSaveContext(Game game)
    {
        var pokemon = game.Trainer.Party.Pokemons.First();
        var messageHandler = new StubHttpMessageHandler(HttpStatusCode.NotFound, string.Empty);
        var repository = RepositoryUsing(messageHandler);

        await repository.SuggestMovesFor(pokemon);

        messageHandler.RequestUris
            .Select(uri => Path.GetFileNameWithoutExtension(uri.AbsolutePath))
            .Should().Equal("gen7letsgoou");
    }

    private static string CompetitiveResponseFor(string speciesName, object[] moveSlots) =>
        JsonSerializer.Serialize(new Dictionary<string, object>
        {
            [speciesName] = new Dictionary<string, object>
            {
                ["Standard"] = new { moves = moveSlots },
            },
        });

    private static SmogonMoveSetRepository RepositoryReturning(HttpStatusCode statusCode, string responseBody)
    {
        return RepositoryUsing(new StubHttpMessageHandler(statusCode, responseBody));
    }

    private static SmogonMoveSetRepository RepositoryUsing(HttpMessageHandler messageHandler)
    {
        var httpClient = new HttpClient(messageHandler);
        return new SmogonMoveSetRepository(httpClient);
    }

    private sealed class StubHttpMessageHandler(HttpStatusCode statusCode, string responseBody) : HttpMessageHandler
    {
        public List<Uri> RequestUris { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUris.Add(request.RequestUri!);
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
                RequestMessage = request,
            });
        }
    }

    private sealed class ThrowingHttpMessageHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromException<HttpResponseMessage>(exception);
    }
}
