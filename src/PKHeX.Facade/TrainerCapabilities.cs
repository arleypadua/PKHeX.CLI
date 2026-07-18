namespace PKHeX.Facade;

// dry-new: writable trainer fields vary by save format; this reports the round-trip-proven facade surface.
public sealed record TrainerCapabilities(
    bool CanEditName,
    bool CanEditGender,
    bool CanEditMoney,
    bool CanEditPlayTime);
