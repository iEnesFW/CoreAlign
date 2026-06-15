using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;
using CoreAlign.Application.Common.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Infrastructure.Storage;

public sealed class ClamAvVirusScanner : IVirusScanner
{
    public const string ProviderName = "ClamAv";

    private const string HostKey = "VirusScan:ClamAv:Host";
    private const string PortKey = "VirusScan:ClamAv:Port";
    private const string TimeoutKey = "VirusScan:ClamAv:TimeoutSeconds";
    private const string ChunkSizeKey = "VirusScan:ClamAv:ChunkSize";
    private const int DefaultPort = 3310;
    private const int DefaultTimeoutSeconds = 30;
    private const int DefaultChunkSize = 64 * 1024;
    private const int TerminatorChunk = 0;

    private readonly string _host;
    private readonly int _port;
    private readonly int _timeoutSeconds;
    private readonly int _chunkSize;
    private readonly ILogger<ClamAvVirusScanner> _logger;

    public ClamAvVirusScanner(IConfiguration configuration, ILogger<ClamAvVirusScanner> logger)
    {
        _host = configuration[HostKey] ?? "localhost";
        _port = configuration.GetValue<int?>(PortKey) ?? DefaultPort;
        _timeoutSeconds = configuration.GetValue<int?>(TimeoutKey) ?? DefaultTimeoutSeconds;
        _chunkSize = configuration.GetValue<int?>(ChunkSizeKey) ?? DefaultChunkSize;
        _logger = logger;
    }

    public async Task<VirusScanResult> ScanAsync(Stream content, CancellationToken cancellationToken = default)
    {
        if (content is null) throw new ArgumentNullException(nameof(content));

        if (content.CanSeek)
        {
            content.Position = 0;
        }

        using var client = new TcpClient
        {
            ReceiveTimeout = _timeoutSeconds * 1000,
            SendTimeout = _timeoutSeconds * 1000,
        };

        await client.ConnectAsync(_host, _port, cancellationToken);
        await using var stream = client.GetStream();

        var instream = Encoding.ASCII.GetBytes("zINSTREAM\0");
        await stream.WriteAsync(instream, cancellationToken);

        var buffer = new byte[_chunkSize];
        var lengthBuffer = new byte[4];

        while (true)
        {
            var read = await content.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
            if (read <= 0)
            {
                break;
            }

            BinaryPrimitives.WriteInt32BigEndian(lengthBuffer, read);
            await stream.WriteAsync(lengthBuffer, cancellationToken);
            await stream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        BinaryPrimitives.WriteInt32BigEndian(lengthBuffer, TerminatorChunk);
        await stream.WriteAsync(lengthBuffer, cancellationToken);
        await stream.FlushAsync(cancellationToken);

        using var responseStream = new MemoryStream();
        await stream.CopyToAsync(responseStream, cancellationToken);
        var response = Encoding.ASCII.GetString(responseStream.ToArray()).Trim().TrimEnd('\0');

        if (content.CanSeek)
        {
            content.Position = 0;
        }

        return ParseResponse(response);
    }

    public static VirusScanResult ParseResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            throw new InvalidOperationException("Empty response from ClamAV.");
        }

        if (response.EndsWith("OK", StringComparison.Ordinal))
        {
            return VirusScanResult.Clean(ProviderName);
        }

        if (response.EndsWith("FOUND", StringComparison.Ordinal))
        {
            var colon = response.IndexOf(':');
            var space = response.LastIndexOf(' ');
            var threat = colon >= 0 && space > colon
                ? response.Substring(colon + 1, space - colon - 1).Trim()
                : response;
            return VirusScanResult.Infected(ProviderName, threat);
        }

        throw new InvalidOperationException($"Unexpected ClamAV response: {response}");
    }
}
