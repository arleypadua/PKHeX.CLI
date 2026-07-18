using PKHeX.Facade;
using PKHeX.Facade.Repositories;

namespace PKHeX.Web.Services;

public class GameService(
    AnalyticsService analytics)
{
    private readonly GameSession _session = new();

    public Game? Game => _session.ActiveGame;
    public Game LoadedGame => Game ?? throw new NullReferenceException("Expected game to be loaded, but it was null.");
    public Game? DraftGame => _session.DraftGame;
    public string? FileName => _session.FileName;
    public bool IsLoaded => Game is not null;
    public bool HasDraft => _session.HasDraft;

    public event EventHandler? OnGameLoaded;
    public event EventHandler? OnActiveGameChanged;

    public void Load(byte[] bytes, string fileName)
    {
        var effectiveFileName = string.IsNullOrWhiteSpace(fileName) ? FileName : fileName;
        _session.Load(bytes, effectiveFileName);

        OnGameLoaded?.Invoke(this, EventArgs.Empty);

        analytics.TrackGameLoaded(LoadedGame);
    }

    public void LoadBlank(GameVersionDefinition version)
    {
        _session.LoadBlank(version);

        OnGameLoaded?.Invoke(this, EventArgs.Empty);

        analytics.TrackGameLoaded(LoadedGame);
    }

    public Game CreateDraft() => _session.CreateDraft();

    public void ApplyDraft()
    {
        _session.ApplyDraft();
        OnActiveGameChanged?.Invoke(this, EventArgs.Empty);
    }

    public void DiscardDraft() => _session.DiscardDraft();

    public void RestoreOriginal()
    {
        _session.RestoreOriginal();
        OnActiveGameChanged?.Invoke(this, EventArgs.Empty);
    }

    public Stream Export()
    {
        var bytes = _session.Export();
        return new MemoryStream(bytes);
    }
}
