using System.IO.Compression;
using System.Text;

namespace CoreAlign.Application.GlassEnclosure.Services;

public interface ISceneCompressor
{
    byte[] Compress(string json);
    string Decompress(byte[] bytes);
}

public class BrotliSceneCompressor : ISceneCompressor
{
    public byte[] Compress(string json)
    {
        var raw = Encoding.UTF8.GetBytes(json);
        using var output = new MemoryStream();
        using (var brotli = new BrotliStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            brotli.Write(raw, 0, raw.Length);
        }
        return output.ToArray();
    }

    public string Decompress(byte[] bytes)
    {
        if (bytes.Length == 0) return "{}";
        using var input = new MemoryStream(bytes);
        using var brotli = new BrotliStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        brotli.CopyTo(output);
        return Encoding.UTF8.GetString(output.ToArray());
    }
}
