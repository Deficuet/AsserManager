using System;
using System.IO;
using AssetManager.Tools;

namespace AssetManager.Deserialize.Deserializer
{
    internal static class TypeTreeReadUtils
    {
        public static void ReadExactly(
            EndianBinaryReader reader,
            byte[] buffer,
            int count)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (count < 0 || count > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            int offset = 0;
            while (offset < count)
            {
                int read = reader.Read(buffer, offset, count - offset);
                if (read <= 0)
                {
                    throw new EndOfStreamException(
                        "Unexpected end of stream while reading a type tree node."
                    );
                }

                offset += read;
            }
        }
    }
}
