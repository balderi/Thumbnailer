namespace libthumbnailer
{
    public static class PngStreamSplitter
    {
        // PNG header bytes
        private static readonly byte[] PngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

        // The 12-byte chunk that ends every PNG: IEND + CRC
        private static readonly byte[] PngIend =
        [
            0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44,
            0xAE, 0x42, 0x60, 0x82
        ];

        /// <summary>
        /// Reads concatenated PNGs from <paramref name="input"/> and yields each
        /// complete PNG as a fresh MemoryStream.
        /// </summary>
        public static IEnumerable<MemoryStream> SplitPngStream(Stream input, int bufferSize = 8192)
        {
            if (!input.CanRead)
                throw new ArgumentException("Input stream must be readable.", nameof(input));

            var buffer = new List<byte>(bufferSize);
            var temp = new byte[bufferSize];

            int bytesRead;
            while ((bytesRead = input.Read(temp, 0, temp.Length)) > 0)
            {
                buffer.AddRange(temp.AsSpan(0, bytesRead).ToArray());

                // Look for a complete PNG in our buffer
                var headerPos = 0;
                while ((headerPos = IndexOf(buffer, PngHeader, headerPos)) >= 0)
                {
                    var iendPos = IndexOf(buffer, PngIend, headerPos + PngHeader.Length);
                    if (iendPos < 0)
                    {
                        // Haven't got the end of the PNG yet; break to read more data
                        break;
                    }

                    var pngLength = (iendPos + PngIend.Length) - headerPos;
                    var pngBytes = buffer.Skip(headerPos).Take(pngLength).ToArray();

                    // Yield the PNG as a MemoryStream
                    yield return new MemoryStream(pngBytes, writable: false);

                    // Remove everything up to the end of this PNG from the buffer
                    buffer.RemoveRange(0, headerPos + pngLength);

                    // Reset headerPos to start searching at beginning again
                    headerPos = 0;
                }
            }
        }

        // Find the first index of 'pattern' in 'source' starting from 'start'
        private static int IndexOf(List<byte> source, byte[] pattern, int start)
        {
            for (var i = start; i <= source.Count - pattern.Length; i++)
            {
                var match = !pattern.Where((t, j) => source[i + j] != t).Any();

                if (match) return i;
            }

            return -1;
        }
    }
}