using AntDesign;
using PKHeX.Facade;
using PKHeX.Web.Services;

namespace PKHeX.Web.Pages;

public partial class Trainer : IDisposable
{
    private Game? _game;
    private bool _hasTrainerDraft;
    private bool _isRestoreOriginalConfirmationVisible;
    private string _trainerName = string.Empty;
    private int _draftPlayTimeHours;
    private int _draftPlayTimeMinutes;
    private int _draftPlayTimeSeconds;
    private string? _operationMessage;
    private AlertType _operationAlertType = AlertType.Success;

    private TrainerCapabilities Capabilities => _game?.Trainer.Editor.Capabilities ?? new(false, false, false, false);

    protected override void OnInitialized()
    {
        GameService.OnGameLoaded += HandleActiveGameChanged;
        GameService.OnActiveGameChanged += HandleActiveGameChanged;
        RefreshFromActiveGame();
    }

    private void HandleActiveGameChanged(object? sender, EventArgs eventArgs)
    {
        RefreshFromActiveGame();
        _ = InvokeAsync(StateHasChanged);
    }

    private void RefreshFromActiveGame()
    {
        _game = GameService.IsLoaded ? GameService.LoadedGame : null;
        _hasTrainerDraft = false;
        _isRestoreOriginalConfirmationVisible = false;

        if (_game is null)
        {
            _trainerName = string.Empty;
            _draftPlayTimeHours = 0;
            _draftPlayTimeMinutes = 0;
            _draftPlayTimeSeconds = 0;
            return;
        }

        SyncDraftValues();
    }

    private void StartEditing()
    {
        if (_hasTrainerDraft || GameService.HasDraft) return;

        _game = GameService.CreateDraft();
        _hasTrainerDraft = true;
        SyncDraftValues();
        ClearOperationMessage();
    }

    private void HandleNameChanged(string name)
    {
        if (!_hasTrainerDraft || _game is null || !Capabilities.CanEditName) return;

        TryUpdateDraft(() => _game.Trainer.Editor.SetName(name));
        _trainerName = _game.Trainer.Name;
    }

    private Task HandleGenderChanged(Gender gender)
    {
        if (!_hasTrainerDraft || _game is null || !Capabilities.CanEditGender) return Task.CompletedTask;

        TryUpdateDraft(() => _game.Trainer.Editor.SetGender(gender));
        return Task.CompletedTask;
    }

    private void HandleMoneyChanged(uint amount)
    {
        if (!_hasTrainerDraft || _game is null || !Capabilities.CanEditMoney) return;

        TryUpdateDraft(() => _game.Trainer.Editor.SetMoney(amount));
    }

    private void HandlePlayTimeHoursChanged(int hours) => UpdatePlayTime(hours, _draftPlayTimeMinutes, _draftPlayTimeSeconds);

    private void HandlePlayTimeMinutesChanged(int minutes) => UpdatePlayTime(_draftPlayTimeHours, minutes, _draftPlayTimeSeconds);

    private void HandlePlayTimeSecondsChanged(int seconds) => UpdatePlayTime(_draftPlayTimeHours, _draftPlayTimeMinutes, seconds);

    private void UpdatePlayTime(int hours, int minutes, int seconds)
    {
        if (!_hasTrainerDraft || _game is null || !Capabilities.CanEditPlayTime) return;

        TryUpdateDraft(() => _game.Trainer.Editor.SetPlayTime(new TrainerPlayTime(hours, minutes, seconds)));
        SyncDraftValues();
    }

    private void TryUpdateDraft(Action updateDraft)
    {
        try
        {
            updateDraft();
            ClearOperationMessage();
        }
        catch (ArgumentException exception)
        {
            SetOperationMessage(exception.Message, AlertType.Error);
        }
        catch (InvalidOperationException exception)
        {
            SetOperationMessage(exception.Message, AlertType.Error);
        }
    }

    private void ApplyChanges()
    {
        if (!_hasTrainerDraft || !GameService.HasDraft) return;

        GameService.ApplyDraft();
        SetOperationMessage("Trainer draft applied to this session.", AlertType.Success);
    }

    private void DiscardChanges()
    {
        if (_hasTrainerDraft && GameService.HasDraft)
            GameService.DiscardDraft();

        RefreshFromActiveGame();
        SetOperationMessage("Trainer draft discarded. No changes were applied.", AlertType.Info);
    }

    private void ShowRestoreOriginalConfirmation()
    {
        if (GameService.HasDraft) return;

        _isRestoreOriginalConfirmationVisible = true;
    }

    private void HideRestoreOriginalConfirmation() => _isRestoreOriginalConfirmationVisible = false;

    private void RestoreOriginal()
    {
        if (GameService.HasDraft) return;

        GameService.RestoreOriginal();
        SetOperationMessage("The original uploaded save has been restored.", AlertType.Success);
    }

    private void SyncDraftValues()
    {
        if (_game is null) return;

        _trainerName = _game.Trainer.Name;
        var playTime = _game.Trainer.PlayTime;
        _draftPlayTimeHours = playTime.Hours;
        _draftPlayTimeMinutes = playTime.Minutes;
        _draftPlayTimeSeconds = playTime.Seconds;
    }

    private void SetOperationMessage(string message, AlertType alertType)
    {
        _operationMessage = message;
        _operationAlertType = alertType;
    }

    private void ClearOperationMessage() => _operationMessage = null;

    public void Dispose()
    {
        GameService.OnGameLoaded -= HandleActiveGameChanged;
        GameService.OnActiveGameChanged -= HandleActiveGameChanged;

        if (_hasTrainerDraft && GameService.HasDraft)
            GameService.DiscardDraft();
    }
}
