using AssetManager.Deserialize.Deserializer;
using AssetManager.Tools;
using AssetManager.TypeTree;
using System;

namespace AssetManager.Deserialize.Controller
{
    internal interface IPrimitiveController<T> : IDeserializeController<T>
    {
        bool TryGetElementSize(TypeTreeBranch elementNode, out int byteSize);

        T[] ReadArray(
            EndianBinaryReader reader,
            int count,
            TypeTreeBranch elementNode,
            int byteSize);
    }

    internal sealed class PrimitiveDeserializeController<T>
        : DeserializeControllerBase<T>, IPrimitiveController<T>
    {
        private readonly NodeDataType _dataType;
        private readonly int _size;
        private readonly bool _isChar;
        private readonly Func<EndianBinaryReader, TypeTreeBranch, T> _read;

        public PrimitiveDeserializeController(
            NodeDataType dataType,
            int size,
            Func<EndianBinaryReader, TypeTreeBranch, T> read,
            bool isChar = false)
        {
            _dataType = dataType;
            _size = size;
            _read = read;
            _isChar = isChar;
        }

        protected override T ReadCore(
            EndianBinaryReader reader,
            TypeTreeBranch node)
        {
            if (_isChar)
            {
                if (node.DataType != NodeDataType.Char
                    && node.DataType != NodeDataType.WideChar)
                {
                    throw new DeserializeException(
                        $"Expected char type tree node, got {node.DataType} "
                        + $"at '{node.Name}'."
                    );
                }

                if (node.Node.ByteSize != 1 && node.Node.ByteSize != 2)
                {
                    throw new DeserializeException(
                        $"Expected char node width 1 or 2, got "
                        + $"{node.Node.ByteSize} at '{node.Name}'."
                    );
                }
            }
            else
            {
                if (node.DataType != _dataType)
                {
                    throw new DeserializeException(
                        $"Expected type tree node {_dataType}, got "
                        + $"{node.DataType} at '{node.Name}'."
                    );
                }

                if (node.Node.ByteSize != _size)
                {
                    throw new DeserializeException(
                        $"Expected width {_size} for {_dataType} at "
                        + $"'{node.Name}', got {node.Node.ByteSize}."
                    );
                }
            }

            return _read(reader, node);
        }

        public bool TryGetElementSize(
            TypeTreeBranch elementNode,
            out int byteSize)
        {
            if (_isChar)
            {
                if ((elementNode.DataType == NodeDataType.Char
                        || elementNode.DataType == NodeDataType.WideChar)
                    && (elementNode.Node.ByteSize == 1
                        || elementNode.Node.ByteSize == 2))
                {
                    byteSize = elementNode.Node.ByteSize;
                    return true;
                }
            }
            else if (elementNode.DataType == _dataType
                && elementNode.Node.ByteSize == _size)
            {
                byteSize = _size;
                return true;
            }

            byteSize = 0;
            return false;
        }

        public T[] ReadArray(
            EndianBinaryReader reader,
            int count,
            TypeTreeBranch elementNode,
            int byteSize)
        {
            if (count < 0)
            {
                throw new DeserializeException(
                    $"Negative array length at '{elementNode.Name}'."
                );
            }

            if (byteSize <= 0 || count > int.MaxValue / byteSize)
            {
                throw new DeserializeException(
                    $"Array length {count} overflows element width "
                    + $"{byteSize} at '{elementNode.Name}'."
                );
            }

            int byteCount = count * byteSize;
            byte[] buffer = new byte[byteCount];
            TypeTreeReadUtils.ReadExactly(reader, buffer, byteCount);

            if (typeof(T) == typeof(bool))
            {
                bool[] values = new bool[count];
                for (int i = 0; i < count; ++i)
                {
                    values[i] = buffer[i] != 0;
                }

                return (T[])(object)values;
            }

            if (typeof(T) == typeof(char) && byteSize == 1)
            {
                char[] values = new char[count];
                for (int i = 0; i < count; ++i)
                {
                    values[i] = (char)buffer[i];
                }

                return (T[])(object)values;
            }

            T[] result = new T[count];
            Buffer.BlockCopy(buffer, 0, result, 0, byteCount);
            return result;
        }
    }

    internal static class PrimitiveDeserializeControllers
    {
        public static bool TryCreate<T>(
            out IDeserializeController<T> controller)
        {
            object result = null;

            if (typeof(T) == typeof(sbyte))
            {
                result = new PrimitiveDeserializeController<sbyte>(
                    NodeDataType.Int8,
                    1,
                    static (reader, node) => node.ReadSByte(reader)
                );
            }
            else if (typeof(T) == typeof(byte))
            {
                result = new PrimitiveDeserializeController<byte>(
                    NodeDataType.UInt8,
                    1,
                    static (reader, node) => node.ReadByte(reader)
                );
            }
            else if (typeof(T) == typeof(short))
            {
                result = new PrimitiveDeserializeController<short>(
                    NodeDataType.Int16,
                    2,
                    static (reader, node) => node.ReadShort(reader)
                );
            }
            else if (typeof(T) == typeof(ushort))
            {
                result = new PrimitiveDeserializeController<ushort>(
                    NodeDataType.UInt16,
                    2,
                    static (reader, node) => node.ReadUShort(reader)
                );
            }
            else if (typeof(T) == typeof(int))
            {
                result = new PrimitiveDeserializeController<int>(
                    NodeDataType.Int32,
                    4,
                    static (reader, node) => node.ReadInt(reader)
                );
            }
            else if (typeof(T) == typeof(uint))
            {
                result = new PrimitiveDeserializeController<uint>(
                    NodeDataType.UInt32,
                    4,
                    static (reader, node) => node.ReadUInt(reader)
                );
            }
            else if (typeof(T) == typeof(long))
            {
                result = new PrimitiveDeserializeController<long>(
                    NodeDataType.Int64,
                    8,
                    static (reader, node) => node.ReadLong(reader)
                );
            }
            else if (typeof(T) == typeof(ulong))
            {
                result = new PrimitiveDeserializeController<ulong>(
                    NodeDataType.UInt64,
                    8,
                    static (reader, node) => node.ReadULong(reader)
                );
            }
            else if (typeof(T) == typeof(float))
            {
                result = new PrimitiveDeserializeController<float>(
                    NodeDataType.Float,
                    4,
                    static (reader, node) => node.ReadFloat(reader)
                );
            }
            else if (typeof(T) == typeof(double))
            {
                result = new PrimitiveDeserializeController<double>(
                    NodeDataType.Double,
                    8,
                    static (reader, node) => node.ReadDouble(reader)
                );
            }
            else if (typeof(T) == typeof(bool))
            {
                result = new PrimitiveDeserializeController<bool>(
                    NodeDataType.Bool,
                    1,
                    static (reader, node) => node.ReadBool(reader)
                );
            }
            else if (typeof(T) == typeof(char))
            {
                result = new PrimitiveDeserializeController<char>(
                    NodeDataType.Char,
                    2,
                    static (reader, node) => node.ReadChar(reader),
                    isChar: true
                );
            }

            controller = result as IDeserializeController<T>;
            return controller != null;
        }
    }
}
