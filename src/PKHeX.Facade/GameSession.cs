using System.Security.Cryptography;
using PKHeX.Facade.Repositories;

namespace PKHeX.Facade;

// dry-new: Game owns one save facade; session lifecycle and immutable upload retention are separate responsibilities.
public sealed class GameSession
{
    private const string NoGameLoadedMessage = "No game is loaded.";
    private const string DuplicateDraftMessage = "A game draft is already active.";
    private const string NoDraftMessage = "No game draft is active.";
    private const string RestoreWithDraftMessage = "Apply or discard the active game draft before restoring the original save.";
    private const string RestoreMismatchMessage = "Restored save does not match the original upload.";

    private byte[]? _originalBytes;
    private byte[]? _originalSerializedBytes;

    public Game? ActiveGame { get; private set; }
    public Game? DraftGame { get; private set; }
    public string? FileName { get; private set; }
    public string? OriginalSha256 { get; private set; }
    public bool HasDraft => DraftGame is not null;

    public void Load(byte[] bytes, string? fileName = null)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        var originalBytes = bytes.ToArray();
        var activeGame = Game.LoadFrom(originalBytes.ToArray(), fileName);
        var originalSerializedBytes = activeGame.ToByteArray();
        var originalSha256 = ComputeSha256(originalBytes);

        ActiveGame = activeGame;
        DraftGame = null;
        FileName = fileName;
        OriginalSha256 = originalSha256;
        _originalBytes = originalBytes;
        _originalSerializedBytes = originalSerializedBytes;
    }

    public void LoadBlank(GameVersionDefinition version)
    {
        ArgumentNullException.ThrowIfNull(version);

        var blankBytes = Game.EmptyOf(version).ToByteArray();
        Load(blankBytes, version.Name);
    }

    public Game CreateDraft()
    {
        var activeGame = RequireActiveGame();
        if (DraftGame is not null)
            throw new InvalidOperationException(DuplicateDraftMessage);

        var draftGame = activeGame.CreateDraft();
        DraftGame = draftGame;
        return draftGame;
    }

    public void ApplyDraft()
    {
        var draftGame = DraftGame ?? throw new InvalidOperationException(NoDraftMessage);
        var appliedBytes = draftGame.ToByteArray();
        var appliedGame = Game.LoadFrom(appliedBytes, FileName);

        ActiveGame = appliedGame;
        DraftGame = null;
    }

    public void DiscardDraft()
    {
        if (DraftGame is null)
            throw new InvalidOperationException(NoDraftMessage);

        DraftGame = null;
    }

    public void RestoreOriginal()
    {
        RequireActiveGame();
        if (DraftGame is not null)
            throw new InvalidOperationException(RestoreWithDraftMessage);

        var originalBytes = _originalBytes ?? throw new InvalidOperationException(NoGameLoadedMessage);
        if (!string.Equals(ComputeSha256(originalBytes), OriginalSha256, StringComparison.Ordinal))
            throw new InvalidOperationException(RestoreMismatchMessage);

        var restoredGame = Game.LoadFrom(originalBytes.ToArray(), FileName);
        var restoredSerializedBytes = restoredGame.ToByteArray();
        ActiveGame = restoredGame;
        DraftGame = null;
        _originalSerializedBytes = restoredSerializedBytes;
    }

    public byte[] Export()
    {
        var serializedBytes = RequireActiveGame().ToByteArray();
        return _originalBytes is not null &&
               _originalSerializedBytes is not null &&
               serializedBytes.AsSpan().SequenceEqual(_originalSerializedBytes)
            ? _originalBytes.ToArray()
            : serializedBytes;
    }

    private Game RequireActiveGame() =>
        ActiveGame ?? throw new InvalidOperationException(NoGameLoadedMessage);

    private static string ComputeSha256(byte[] bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes));
}
