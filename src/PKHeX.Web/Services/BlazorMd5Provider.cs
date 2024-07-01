using PKHeX.Core.Saves.Encryption.Providers;

namespace PKHeX.Web.Services;

public class BlazorMd5Provider(JsService jsService) : IMd5Provider
{
    public IMd5Provider.IMd5Hash Create() => new Md5Hasher(jsService);

    private class Md5Hasher(JsService jsService) : IMd5Provider.IMd5Hash
    {
        private readonly List<byte> _data = new();
        public void AppendData(ReadOnlySpan<byte> data)
        {
            _data.AddRange(data.ToArray());
        }

        public void GetCurrentHash(Span<byte> hash)
        {
            var hashArray = jsService.Md5Hash(_data.ToArray());
            hashArray.CopyTo(hash);
        }
        
        public void Dispose() { }
    }
}