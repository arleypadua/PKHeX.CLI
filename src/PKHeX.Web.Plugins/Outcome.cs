namespace PKHeX.Web.Plugins;

public abstract class Outcome
{
    // only allow this library to specify valid implementations
    private protected Outcome()
    {
    }

    public static readonly Outcome Void = new VoidOutcome();

    public static Notification Notify(string message, string? description = null,
        Notification.NotificationType type = Notification.NotificationType.None) => new Notification
    {
        Message = message,
        Description = description,
        Type = type
    };

    public class Notification : Outcome
    {
        public required string Message { get; set; }
        public string? Description { get; set; }
        public NotificationType Type { get; set; } = NotificationType.None;

        public enum NotificationType
        {
            None = 0,
            Info = 1,
            Success = 2,
            Warning = 3,
            Error = 4
        }
    }

    private class VoidOutcome : Outcome
    {
    }
}

public static class ResultExtensions
{
    public static Task<Outcome> Completed<T>(this T result) where T : Outcome => Task.FromResult<Outcome>(result);
}