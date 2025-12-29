namespace libthumbnailer;

public static class MjpegFrameExtractor
{
    // JPEG start/end markers
    private static readonly byte[] JpegSOI = { 0xFF, 0xD8 };
    private static readonly byte[] JpegEOI = { 0xFF, 0xD9 };

    /// <summary>
    /// Reads an MJPEG stream (concatenated JPEG images) and yields each frame as a MemoryStream.
    /// </summary>
    public static IEnumerable<MemoryStream> SplitJpegStream(Stream input, int bufferSize = 8192)
    {
        if (!input.CanRead)
            throw new ArgumentException("Input stream must be readable.", nameof(input));

        var buffer = new List<byte>(bufferSize);
        var temp = new byte[bufferSize];

        int bytesRead;
        while ((bytesRead = input.Read(temp, 0, temp.Length)) > 0)
        {
            buffer.AddRange(temp.AsSpan(0, bytesRead).ToArray());

            int start = 0;
            while (true)
            {
                // find SOI
                var soi = IndexOf(buffer, JpegSOI, start);
                if (soi < 0) { break; }

                // find EOI after SOI
                var eoi = IndexOf(buffer, JpegEOI, soi + 2);
                if (eoi < 0) { break; }

                var length = (eoi + 2) - soi;
                var jpegBytes = buffer.GetRange(soi, length).ToArray();

                yield return new MemoryStream(jpegBytes, writable: false);

                // remove all bytes up to end of this JPEG
                buffer.RemoveRange(0, soi + length);
                start = 0;
            }
        }
    }

    private static int IndexOf(List<byte> source, byte[] pattern, int start)
    {
        for (int i = start; i <= source.Count - pattern.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < pattern.Length; j++)
            {
                if (source[i + j] != pattern[j]) { match = false; break; }
            }
            if (match) return i;
        }
        return -1;
    }
}