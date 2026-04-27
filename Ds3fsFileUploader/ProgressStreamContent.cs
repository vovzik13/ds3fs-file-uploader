using System.Net;

namespace Ds3fsFileUploader;

public class ProgressStreamContent : StreamContent
{
    private readonly Stream              _stream;
    private readonly Action<long, long>? _progressCallback;
    private readonly long                _totalBytes;
    private readonly CancellationToken   _cancellationToken;

    public ProgressStreamContent(Stream stream, Action<long, long>? progressCallback, long totalBytes, CancellationToken cancellationToken = default) : base(stream)
    {
        _stream            = stream;
        _progressCallback  = progressCallback;
        _totalBytes        = totalBytes;
        _cancellationToken = cancellationToken;
    }

    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        var buffer    = new byte[81920];
        var totalRead = 0L;
        int read;

        while ((read = await _stream.ReadAsync(buffer, _cancellationToken)) > 0)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            await stream.WriteAsync(buffer.AsMemory(0, read), _cancellationToken);
            totalRead += read;
            _progressCallback?.Invoke(totalRead, _totalBytes);
        }
    }
}