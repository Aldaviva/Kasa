using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Kasa;

static class IO {
    public static async Task ReadExactAsync(this Stream stream,
                                            byte[] buffer, int offset, int count,
                                            CancellationToken cancellationToken = default) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (buffer is null) throw new ArgumentNullException(nameof(buffer));

        for (int totalRead = 0; totalRead < count;) {
            int read = await stream.ReadAsync(buffer, offset + totalRead, count - totalRead, cancellationToken);
            if (read == 0) throw new EndOfStreamException();
            totalRead += read;
        }
    }
}
