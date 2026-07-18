namespace PKHeX.Facade;

// dry-new: PKHeX.Core play-time blocks are format internals; this is the stable facade value contract.
public readonly record struct TrainerPlayTime
{
    public TrainerPlayTime(int hours, int minutes, int seconds)
    {
        if (hours < 0)
            throw new ArgumentOutOfRangeException(nameof(hours));
        if (minutes is < 0 or > 59)
            throw new ArgumentOutOfRangeException(nameof(minutes));
        if (seconds is < 0 or > 59)
            throw new ArgumentOutOfRangeException(nameof(seconds));

        Hours = hours;
        Minutes = minutes;
        Seconds = seconds;
    }

    public int Hours { get; }
    public int Minutes { get; }
    public int Seconds { get; }
}
