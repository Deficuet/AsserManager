using AssetsTools.NET;
using AssetManager.Tools;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AssetManager.TypeTree
{
    public sealed class TypeTree
    {
        public TypeTreeNode Node { get; }

        public string Type { get; }

        public string Name { get; }

        public List<TypeTree> Children { get; } = new List<TypeTree>();

        private NodeDataType _dataType = NodeDataType.Unknown;

        private TypeTree(TypeTreeNode node, TypeTreeResult result)
        {
            Node = node;
            Type = result.GetTypeString(node);
            Name = result.GetNameString(node);
        }

        public NodeDataType DataType
        {
            get
            {
                if (_dataType == NodeDataType.Unknown)
                {
                    _dataType = NodeDataTypeHelper.MatchDataType(this);
                }
                return _dataType;
            }
        }

        public static TypeTree Build(TypeTreeResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            var nodes = result.Nodes;
            if (nodes == null || nodes.Count == 0)
            {
                throw new ArgumentException(
                    "A stripped type tree contains no nodes and cannot be built.",
                    nameof(result)
                );
            }

            if (nodes[0] == null)
            {
                throw new ArgumentException(
                    "Type tree node 0 is null.",
                    nameof(result)
                );
            }
            if (nodes[0].Level != 0)
            {
                throw new ArgumentException(
                    "The first type tree node must be the root node at level 0.",
                    nameof(result)
                );
            }

            for (int i = 1; i < nodes.Count; ++i)
            {
                TypeTreeNode node = nodes[i] 
                    ?? throw new ArgumentException(
                        $"Type tree node {i} is null.",
                        nameof(result)
                    );
                if (node.Level == 0)
                {
                    throw new ArgumentException(
                        $"Type tree node {i} is at level 0, but only the first node may be a root.",
                        nameof(result)
                    );
                }
                if (node.Level > nodes[i - 1].Level + 1)
                {
                    throw new ArgumentException(
                        $"Type tree node {i} is at level {node.Level}, which skips a level " +
                        $"after node {i - 1} at level {nodes[i - 1].Level}.",
                        nameof(result)
                    );
                }
            }

            TypeTree root = null;
            Stack<TypeTree> stack = new();
            foreach (TypeTreeNode node in nodes)
            {
                var currentNode = new TypeTree(node, result);
                while (stack.Count > 0 && stack.Peek().Node.Level >= node.Level)
                {
                    stack.Pop();
                }
                if (stack.Count > 0)
                {
                    stack.Peek().Children.Add(currentNode);
                }
                else
                {
                    root = currentNode;
                }
                stack.Push(currentNode);
            }
            return root;
        }

        public void CheckAlignmentWith(EndianBinaryReader reader, int alignment = 4)
        {
            if ((Node.MetaFlags & 0x4000) != 0)
            {
                reader.AlignStream(alignment);
            }
        }

        private T FinishRead<T>(EndianBinaryReader reader, T result)
        {
            CheckAlignmentWith(reader);
            return result;
        }

        public sbyte ReadSByte(EndianBinaryReader reader)
        {
            return FinishRead(reader, reader.ReadSByte());
        }

        public short ReadShort(EndianBinaryReader reader)
        {
            return FinishRead(reader, reader.ReadInt16());
        }

        public int ReadInt(EndianBinaryReader reader)
        {
            return FinishRead(reader, reader.ReadInt32());
        }

        public long ReadLong(EndianBinaryReader reader)
        {
            return FinishRead(reader, reader.ReadInt64());
        }

        public byte ReadByte(EndianBinaryReader reader)
        {
            return FinishRead(reader, reader.ReadByte());
        }

        public ushort ReadUShort(EndianBinaryReader reader)
        {
            return FinishRead(reader, reader.ReadUInt16());
        }

        public uint ReadUInt(EndianBinaryReader reader)
        {
            return FinishRead(reader, reader.ReadUInt32());
        }

        public ulong ReadULong(EndianBinaryReader reader)
        {
            return FinishRead(reader, reader.ReadUInt64());
        }

        public float ReadFloat(EndianBinaryReader reader)
        {
            return FinishRead(reader, reader.ReadSingle());
        }

        public double ReadDouble(EndianBinaryReader reader)
        {
            return FinishRead(reader, reader.ReadDouble());
        }

        public bool ReadBool(EndianBinaryReader reader)
        {
            return FinishRead(reader, reader.ReadBoolean());
        }

        public char ReadChar(EndianBinaryReader reader)
        {
            char c = DataType switch
            {
                NodeDataType.Char => Convert.ToChar(reader.ReadByte()),
                NodeDataType.WideChar => Convert.ToChar(reader.ReadUInt16()),
                _ => throw new NotSupportedException("Char must be 1 or 2 bytes wide")
            };
            return FinishRead(reader, c);
        }

        public byte[] ReadBytes(EndianBinaryReader reader)
        {
            var sizeNode = Children[0];
            var size = sizeNode.ReadInt(reader);
            if (size == 0)
            {
                return Array.Empty<byte>();
            }
            var buffer = new byte[size];
            reader.Read(buffer, 0, size);
            return FinishRead(reader, buffer);
        }

        private static string ReadSpanString(EndianBinaryReader reader, Span<byte> span, int byteSize)
        {
            reader.Read(span);
            return byteSize switch
            {
                1 => Encoding.UTF8.GetString(span),
                2 => Encoding.Unicode.GetString(span),
                _ => throw new NotSupportedException("Char of a string must be 1 or 2 bytes wide")
            };
        }

        public string ReadString(EndianBinaryReader reader)
        {
            var sizeNode = Children[0];
            var charNode = Children[1];
            var size = sizeNode.ReadInt(reader);
            if (size == 0)
            {
                return FinishRead(reader, "");
            }
            var strByteSize = size * charNode.Node.ByteSize;
            string ret;
            if (strByteSize <= 512)
            {
                Span<byte> strBuf = stackalloc byte[strByteSize];
                ret = ReadSpanString(reader, strBuf, charNode.Node.ByteSize);
            }
            else
            {
                var rented = ArrayPool<byte>.Shared.Rent(strByteSize);
                try
                {
                    var strBuf = rented.AsSpan(0, strByteSize);
                    ret = ReadSpanString(reader, strBuf, charNode.Node.ByteSize);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(rented);
                }
            }
            return FinishRead(reader, ret);
        }

        public void Skip(EndianBinaryReader reader)
        {
            if (DataType.IsValueType())
            {
                if (Node.ByteSize == -1)
                {
                    throw new InvalidDataException("Cannot skip value type with unknown size");
                }
                reader.Position += Node.ByteSize;
                CheckAlignmentWith(reader);
            }
            else if (DataType.IsArrayType())
            {
                var arrayNode = DataType switch
                {
                    NodeDataType.Typeless => this,
                    _ => Children[0]
                };
                var size = arrayNode.Children[0].ReadInt(reader);
                var elementNode = arrayNode.Children[1];
                for (int i = 0; i < size; ++i)
                {
                    elementNode.Skip(reader);
                }
                arrayNode.CheckAlignmentWith(reader);
            }
            else if (DataType.IsCompositeType())
            {
                foreach (var child in Children)
                {
                    child.Skip(reader);
                }
                CheckAlignmentWith(reader);
            }
        }
    }
}
