using PKHeX.Core;

namespace PKHeX.Facade;

// dry-new: Core exposes format-specific setters but does not provide a serialized capability contract for facade consumers.
public sealed class TrainerEditor
{
    private readonly Game _game;
    private readonly Lazy<TrainerCapabilities> _capabilities;

    public TrainerEditor(Game game)
    {
        _game = game;
        _capabilities = new Lazy<TrainerCapabilities>(DetectCapabilities);
    }

    public TrainerCapabilities Capabilities => _capabilities.Value;

    public void SetName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        RequireCapability(Capabilities.CanEditName, "name");

        ApplyVerifiedMutation(
            saveFile => saveFile.OT = name,
            saveFile => string.Equals(saveFile.OT, name, StringComparison.Ordinal),
            "trainer name",
            nameof(name));
    }

    public void SetGender(Gender gender)
    {
        ArgumentNullException.ThrowIfNull(gender);
        if (gender != Gender.Male && gender != Gender.Female)
            throw new ArgumentOutOfRangeException(nameof(gender), "Only male and female trainer genders are supported.");

        RequireCapability(Capabilities.CanEditGender, "gender");
        ApplyVerifiedMutation(
            saveFile => saveFile.Gender = gender.ToByte(),
            saveFile => saveFile.Gender == gender.ToByte(),
            "trainer gender",
            nameof(gender));
    }

    public void SetMoney(uint amount)
    {
        if (amount > _game.SaveFile.MaxMoney)
            throw new ArgumentOutOfRangeException(nameof(amount), $"Money cannot exceed {_game.SaveFile.MaxMoney}.");

        RequireCapability(Capabilities.CanEditMoney, "money");
        ApplyVerifiedMutation(
            saveFile => saveFile.Money = amount,
            saveFile => saveFile.Money == amount,
            "money",
            nameof(amount));
    }

    public void SetPlayTime(TrainerPlayTime playTime)
    {
        RequireCapability(Capabilities.CanEditPlayTime, "play time");

        ApplyVerifiedMutation(
            saveFile => SetPlayTime(saveFile, playTime),
            saveFile => GetPlayTime(saveFile) == playTime,
            "play time",
            nameof(playTime));
    }

    private TrainerCapabilities DetectCapabilities() => new(
        CanRoundTrip(
            saveFile => saveFile.OT = GetProbeTrainerName(),
            saveFile => string.Equals(saveFile.OT, GetProbeTrainerName(), StringComparison.Ordinal)),
        CanRoundTrip(
            saveFile => saveFile.Gender = GetProbeGender(),
            saveFile => saveFile.Gender == GetProbeGender()),
        CanRoundTrip(
            saveFile => saveFile.Money = GetProbeMoney(),
            saveFile => saveFile.Money == GetProbeMoney()),
        CanRoundTrip(
            saveFile => SetPlayTime(saveFile, GetProbePlayTime()),
            saveFile => GetPlayTime(saveFile) == GetProbePlayTime()));

    private bool CanRoundTrip(Action<SaveFile> updateSaveFile, Func<SaveFile, bool> matchesExpectedValue)
    {
        try
        {
            return matchesExpectedValue(RoundTrip(updateSaveFile));
        }
        catch
        {
            return false;
        }
    }

    private void ApplyVerifiedMutation(
        Action<SaveFile> updateSaveFile,
        Func<SaveFile, bool> matchesExpectedValue,
        string fieldName,
        string parameterName)
    {
        var verifiedSaveFile = RoundTrip(updateSaveFile);
        if (!matchesExpectedValue(verifiedSaveFile))
            throw new ArgumentException($"The save format cannot represent the requested {fieldName}.", parameterName);

        _game.SaveFile.CopyChangesFrom(verifiedSaveFile);
    }

    private SaveFile RoundTrip(Action<SaveFile> updateSaveFile)
    {
        var probeSaveFile = _game.SaveFile.Clone();
        updateSaveFile(probeSaveFile);
        var serializedBytes = Game.Serialize(probeSaveFile);

        return SaveUtil.GetSaveFile(serializedBytes, probeSaveFile.Metadata.FilePath)
            ?? throw new InvalidOperationException("The edited save could not be reloaded for verification.");
    }

    private string GetProbeTrainerName()
    {
        var currentName = _game.SaveFile.OT;
        if (currentName.Length < 2)
            return string.Equals(currentName, "A", StringComparison.Ordinal) ? "B" : "A";

        var rotatedName = string.Concat(currentName[1..], currentName[0]);
        return rotatedName != currentName ? rotatedName : currentName[..^1];
    }

    private byte GetProbeGender() => _game.SaveFile.Gender == Gender.Male.ToByte()
        ? Gender.Female.ToByte()
        : Gender.Male.ToByte();

    private uint GetProbeMoney() => _game.SaveFile.Money == 0 ? 1u : 0u;

    private TrainerPlayTime GetProbePlayTime() => GetPlayTime(_game.SaveFile) == default
        ? new TrainerPlayTime(hours: 1, minutes: 1, seconds: 1)
        : default;

    private static TrainerPlayTime GetPlayTime(SaveFile saveFile) => new(
        saveFile.PlayedHours,
        saveFile.PlayedMinutes,
        saveFile.PlayedSeconds);

    private static void SetPlayTime(SaveFile saveFile, TrainerPlayTime playTime)
    {
        ValidatePlayTime(saveFile, playTime);
        saveFile.PlayedMinutes = playTime.Minutes;
        saveFile.PlayedSeconds = playTime.Seconds;
        saveFile.PlayedHours = playTime.Hours;

        if (saveFile is SAV1 generation1SaveFile)
            generation1SaveFile.PlayedMaximum = playTime.Hours == byte.MaxValue;
    }

    private static void ValidatePlayTime(SaveFile saveFile, TrainerPlayTime playTime)
    {
        if (saveFile is SAV1 &&
            (playTime.Hours > byte.MaxValue ||
             (playTime.Hours == byte.MaxValue && (playTime.Minutes != 0 || playTime.Seconds != 0))))
        {
            throw new ArgumentOutOfRangeException(
                nameof(playTime),
                "Generation 1 maximum play time must use zero minutes and seconds.");
        }
    }

    private void RequireCapability(bool canEdit, string fieldName)
    {
        if (!canEdit)
            throw new NotSupportedException($"Trainer {fieldName} cannot be edited for {_game.SaveFile.Version}.");
    }
}
