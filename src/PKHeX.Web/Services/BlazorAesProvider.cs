using System.Security.Cryptography;
using PKHeX.Core.Saves.Encryption.Providers;

namespace PKHeX.Web.Services;

public class BlazorAesProvider(JsService jsService) : IAesCryptographyProvider
{
    public IAesCryptographyProvider.IAes Create(byte[] key, IAesCryptographyProvider.Options options) =>
        new CryptoJsAes(jsService, key, options);

    private class CryptoJsAes(JsService jsService, byte[] key, IAesCryptographyProvider.Options options)
        : IAesCryptographyProvider.IAes
    {
        public void EncryptEcb(ReadOnlySpan<byte> origin, Span<byte> destination) =>
            jsService.EncryptEcb(origin, destination, key);

        public void DecryptEcb(ReadOnlySpan<byte> origin, Span<byte> destination) =>
            jsService.DecryptEcb(origin, destination, key);

        public ICryptoTransform CreateDecryptor(byte[] key, byte[] iv)
        {
            // todo: to be implemented for HOME
            throw new NotImplementedException();
        }

        public ICryptoTransform CreateEncryptor(byte[] key, byte[] iv)
        {
            // todo: to be implemented for HOME
            throw new NotImplementedException();
        }

        public void Dispose()
        {
        }
    }
}