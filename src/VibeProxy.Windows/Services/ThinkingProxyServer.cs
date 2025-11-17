using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VibeProxy.Windows.Services;

public sealed class ThinkingProxyServer : IDisposable
{
    private const int ProxyPort = 8317;
    private const int TargetPort = 8318;

    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private bool _disposed;

    public event EventHandler<bool>? StatusChanged;

    public bool IsRunning { get; private set; }

    public int ListeningPort => ProxyPort;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (IsRunning)
        {
            return;
        }

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _listener = new TcpListener(IPAddress.Loopback, ProxyPort)
        {
            Server = { NoDelay = true }
        };

        try
        {
            _listener.Start();
            IsRunning = true;
            StatusChanged?.Invoke(this, true);
        }
        catch
        {
            _cts.Dispose();
            _cts = null;
            _listener = null;
            throw;
        }

        _ = AcceptLoopAsync(_cts.Token);
        await Task.CompletedTask;
    }

    public Task StopAsync()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _listener?.Stop();
        _listener = null;
        if (IsRunning)
        {
            IsRunning = false;
            StatusChanged?.Invoke(this, false);
        }

        return Task.CompletedTask;
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        if (_listener is null)
        {
            return;
        }

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
                _ = Task.Run(async () => await HandleClientAsync(client, cancellationToken).ConfigureAwait(false), cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // expected on shutdown
        }
        catch (ObjectDisposedException)
        {
            // listener disposed during shutdown
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using var clientScope = client;
        using var clientStream = clientScope.GetStream();

        try
        {
            var requestData = await ReadHttpRequestAsync(clientStream, cancellationToken).ConfigureAwait(false);
            if (requestData is null)
            {
                await SendErrorAsync(clientStream, 400, "Invalid Request", cancellationToken).ConfigureAwait(false);
                return;
            }

            var (method, path, version, headers, body) = requestData.Value;
            var bodyText = Encoding.UTF8.GetString(body);
            var shouldTransform = string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase);
            var (modifiedBody, transformed) = shouldTransform ? ThinkingModelTransformer.Apply(bodyText) : (bodyText, false);
            var payloadBytes = Encoding.UTF8.GetBytes(modifiedBody);

            await ForwardRequestAsync(method, path, version, headers, payloadBytes, transformed, clientStream, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or SocketException)
        {
            // connection dropped - nothing else to do
        }
    }

    private static async Task<(string Method, string Path, string Version, List<KeyValuePair<string, string>> Headers, byte[] Body)?> ReadHttpRequestAsync(Stream stream, CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(8192);
        try
        {
            using var ms = new MemoryStream();
            int headerEnd = -1;
            while (headerEnd < 0)
            {
                var bytesRead = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    return null;
                }

                ms.Write(buffer, 0, bytesRead);
                headerEnd = FindHeaderTerminator(ms.GetBuffer(), (int)ms.Length);
            }

            var requestBytes = ms.ToArray();
            var headerBytesLength = headerEnd + 4; // include CRLFCRLF
            var headerText = Encoding.ASCII.GetString(requestBytes, 0, headerBytesLength);

            var headerLines = headerText.Split(new[] { "\r\n" }, StringSplitOptions.None);
            if (headerLines.Length == 0)
            {
                return null;
            }

            var requestLineParts = headerLines[0].Split(' ');
            if (requestLineParts.Length < 3)
            {
                return null;
            }

            var method = requestLineParts[0];
            var path = requestLineParts[1];
            var version = requestLineParts[2];

            var headers = new List<KeyValuePair<string, string>>();
            foreach (var line in headerLines[1..])
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    break;
                }

                var separatorIndex = line.IndexOf(':');
                if (separatorIndex < 0)
                {
                    continue;
                }

                var name = line[..separatorIndex].Trim();
                var value = line[(separatorIndex + 1)..].Trim();
                headers.Add(new KeyValuePair<string, string>(name, value));
            }

            var bodyLength = 0;
            foreach (var header in headers)
            {
                if (header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(header.Value, out var parsed))
                {
                    bodyLength = parsed;
                    break;
                }
            }

            var alreadyBufferedBody = requestBytes.Length - headerBytesLength;
            if (alreadyBufferedBody < bodyLength)
            {
                var remaining = bodyLength - alreadyBufferedBody;
                while (remaining > 0)
                {
                    var read = await stream.ReadAsync(buffer.AsMemory(0, Math.Min(buffer.Length, remaining)), cancellationToken).ConfigureAwait(false);
                    if (read == 0)
                    {
                        break;
                    }

                    ms.Write(buffer, 0, read);
                    remaining -= read;
                }

                requestBytes = ms.ToArray();
            }

            var body = bodyLength > 0
                ? requestBytes[headerBytesLength..(headerBytesLength + bodyLength)]
                : Array.Empty<byte>();

            return (method, path, version, headers, body);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static int FindHeaderTerminator(byte[] buffer, int length)
    {
        for (var i = 3; i < length; i++)
        {
            if (buffer[i - 3] == '\r' && buffer[i - 2] == '\n' && buffer[i - 1] == '\r' && buffer[i] == '\n')
            {
                return i - 3;
            }
        }

        return -1;
    }

    private static async Task ForwardRequestAsync(
        string method,
        string path,
        string version,
        List<KeyValuePair<string, string>> headers,
        byte[] body,
        bool transformed,
        Stream clientStream,
        CancellationToken cancellationToken)
    {
        using var targetClient = new TcpClient();
        await targetClient.ConnectAsync(IPAddress.Loopback, TargetPort, cancellationToken).ConfigureAwait(false);

        using var targetStream = targetClient.GetStream();

        var builder = new StringBuilder();
        builder.Append(method).Append(' ').Append(path).Append(' ').Append(version).Append("\r\n");

        foreach (var header in headers)
        {
            var lower = header.Key.ToLowerInvariant();
            if (lower is "content-length" or "host" or "connection" or "transfer-encoding")
            {
                continue;
            }

            builder.Append(header.Key).Append(": ").Append(header.Value).Append("\r\n");
        }

        builder.Append("Host: 127.0.0.1:").Append(TargetPort).Append("\r\n");
        builder.Append("Connection: close\r\n");
        builder.Append("Content-Length: ").Append(body.Length).Append("\r\n\r\n");

        var headerBytes = Encoding.ASCII.GetBytes(builder.ToString());
        await targetStream.WriteAsync(headerBytes, cancellationToken).ConfigureAwait(false);
        if (body.Length > 0)
        {
            await targetStream.WriteAsync(body, cancellationToken).ConfigureAwait(false);
        }

        await targetStream.FlushAsync(cancellationToken).ConfigureAwait(false);

        var buffer = ArrayPool<byte>.Shared.Rent(8192);
        try
        {
            int bytesRead;
            while ((bytesRead = await targetStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
            {
                await clientStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static async Task SendErrorAsync(Stream clientStream, int statusCode, string message, CancellationToken cancellationToken)
    {
        var body = Encoding.UTF8.GetBytes(message);
        var header = Encoding.ASCII.GetBytes($"HTTP/1.1 {statusCode} {message}\r\nContent-Type: text/plain\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n");
        await clientStream.WriteAsync(header, cancellationToken).ConfigureAwait(false);
        if (body.Length > 0)
        {
            await clientStream.WriteAsync(body, cancellationToken).ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        StopAsync().GetAwaiter().GetResult();
    }
}
