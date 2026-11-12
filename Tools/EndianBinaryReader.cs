using System;
using System.Buffers.Binary;
using System.IO;

namespace AssetManager.Tools
{
    public class EndianBinaryReader : BinaryReader
    {
        private readonly long offset;
        public ByteOrder Endian;

        public EndianBinaryReader(Stream stream, ByteOrder endian = ByteOrder.BigEndian) : base(stream)
        {
            offset = stream.Position;
            Endian = endian;
        }

        public long Position
        {
            get => BaseStream.Position - offset;
            set => BaseStream.Position = value + offset;
        }

        public void AlignStream(int alignment = 4)
        {
            var currentPos = Position;
            var mod = currentPos % alignment;
            if (mod != 0)
            {
                Position = currentPos + alignment - mod;
            }
        }

        public override short ReadInt16()
        {
            if (Endian == ByteOrder.BigEndian)
            {
                Span<byte> buf = stackalloc byte[sizeof(short)];
                Read(buf);
                return BinaryPrimitives.ReadInt16BigEndian(buf);
            }
            return base.ReadInt16();
        }

        public override int ReadInt32()
        {
            if (Endian == ByteOrder.BigEndian)
            {
                Span<byte> buf = stackalloc byte[sizeof(int)];
                Read(buf);
                return BinaryPrimitives.ReadInt32BigEndian(buf);
            }
            return base.ReadInt32();
        }

        public override long ReadInt64()
        {
            if (Endian == ByteOrder.BigEndian)
            {
                Span<byte> buf = stackalloc byte[sizeof(long)];
                Read(buf);
                return BinaryPrimitives.ReadInt64BigEndian(buf);
            }
            return base.ReadInt64();
        }

        public override ushort ReadUInt16()
        {
            if (Endian == ByteOrder.BigEndian)
            {
                Span<byte> buf = stackalloc byte[sizeof(ushort)];
                Read(buf);
                return BinaryPrimitives.ReadUInt16BigEndian(buf);
            }
            return base.ReadUInt16();
        }

        public override uint ReadUInt32()
        {
            if (Endian == ByteOrder.BigEndian)
            {
                Span<byte> buf = stackalloc byte[sizeof(uint)];
                Read(buf);
                return BinaryPrimitives.ReadUInt32BigEndian(buf);
            }
            return base.ReadUInt32();
        }

        public override ulong ReadUInt64()
        {
            if (Endian == ByteOrder.BigEndian)
            {
                Span<byte> buf = stackalloc byte[sizeof(ulong)];
                Read(buf);
                return BinaryPrimitives.ReadUInt64BigEndian(buf);
            }
            return base.ReadUInt64();
        }

#if NETFRAMEWORK
    public override float ReadSingle()
    {
        if (Endian == EndianType.BigEndian)
        {
            Read(buffer, 0, 4);
            buffer.AsSpan(0, 4).Reverse();
            return BitConverter.ToSingle(buffer, 0);
        }
        return base.ReadSingle();
    }

    public override double ReadDouble()
    {
        if (Endian == EndianType.BigEndian)
        {
            Read(buffer, 0, 8);
            buffer.AsSpan().Reverse();
            return BitConverter.ToDouble(buffer, 0);
        }
        return base.ReadDouble();
    }
#else
        public override float ReadSingle()
        {
            if (Endian == ByteOrder.BigEndian)
            {
                Span<byte> buf = stackalloc byte[sizeof(float)];
                Read(buf);
                return BitConverter.Int32BitsToSingle(
                    BinaryPrimitives.ReadInt32BigEndian(buf));
            }
            return base.ReadSingle();
        }

        public override double ReadDouble()
        {
            if (Endian == ByteOrder.BigEndian)
            {
                Span<byte> buf = stackalloc byte[sizeof(double)];
                Read(buf);
                return BitConverter.Int64BitsToDouble(
                    BinaryPrimitives.ReadInt64BigEndian(buf));
            }
            return base.ReadDouble();
        }
#endif
    }
}
