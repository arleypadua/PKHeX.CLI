using Microsoft.AspNetCore.Components;
using PKHeX.Facade;
using PKHeX.Facade.Repositories;
using PKHeX.Facade.Pokemons;
using PKHeX.Web.Extensions;

namespace PKHeX.Web.Services;

public class EncounterService : IDisposable
{
    private readonly GameService _gameService;
    private readonly NavigationManager _navigation;

    public EncounterService(GameService gameService, NavigationManager navigation)
    {
        _gameService = gameService;
        _navigation = navigation;
        _gameService.OnGameLoaded += InitializeOnGameLoad;
        _gameService.OnActiveGameChanged += InitializeOnGameLoad;
    }

    private Game Game => _gameService.Game ?? throw new NullReferenceException("Expected a game to be loaded");
    private PokemonRepository Repository => Game.PokemonRepository;

    public List<Encounter> Encounters { get; private set; } = [];
    public GameVersionDefinition? SelectedGameVersion { get; set; }
    public SpeciesDefinition? SelectedSpecies { get; set; }
    public Encounter? SelectedEncounter { get; private set; }

    public bool AllowedToSearch => SelectedGameVersion is not null && SelectedSpecies is not null;

    public void Search()
    {
        if (!AllowedToSearch)
            throw new InvalidOperationException("Can only execute a search when required parameters are set.");

        Encounters = Repository.FindEncounter(SelectedGameVersion!.Version, SelectedSpecies!.Species).ToList();
    }

    public Pokemon? CreateCompatiblePokemon(SpeciesDefinition species)
    {
        return Repository.FindEncounter(Game.GameVersionApproximation.Version, species.Species)
            .FirstOrDefault()
            ?.ConvertToPokemon();
    }

    public bool HasCompatibleEncounter(SpeciesDefinition species)
        => Repository.FindEncounter(Game.GameVersionApproximation.Version, species.Species).Any();

    public void SelectEncounter(Encounter encounter)
    {
        SelectedEncounter = encounter;
        _navigation.NavigateToSelectedEncounter();
    }

    public void ResetSelectedEncounter()
    {
        SelectedEncounter = null;
    }

    private void InitializeOnGameLoad(object? sender, EventArgs e)
    {
        SelectedGameVersion = GameVersionRepository.Instance
            .GetAvailableFor(_gameService.LoadedGame.Generation, _gameService.LoadedGame.SaveVersion.Version)
            .FirstOrDefault(v => v.Equals(_gameService.LoadedGame.GameVersionApproximation));

        SelectedSpecies = null;
        SelectedEncounter = null;
        Encounters.Clear();
    }

    public void Dispose()
    {
        _gameService.OnGameLoaded -= InitializeOnGameLoad;
        _gameService.OnActiveGameChanged -= InitializeOnGameLoad;
    }
}
