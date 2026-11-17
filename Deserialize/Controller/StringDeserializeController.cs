using AssetManager.Deserialize.Deserializer;
using AssetManager.Tools;
using AssetManager.TypeTree;
using System.Text;

namespace AssetManager.Deserialize.Controller
{
    internal sealed class StringDeserializeController
        : DeserializeControllerBase<string>
    {
        protected override string ReadCore(
            EndianBinaryReader reader,
            TypeTreeBranch node)
        {
            if (node.DataType != NodeDataType.String)
            {
                throw new DeserializeException(
                    $"Expected string type tree node, got {node.DataType} "
                    + $"at '{node.Name}'."
                );
            }

            if (node.Children.Count == 0)
            {
                throw new DeserializeException(
                    $"String node '{node.Name}' has no array child."
                );
            }

            TypeTreeBranch arrayNode = node.Children[0];
            if (arrayNode.Children.Count < 2)
            {
                throw new DeserializeException(
                    $"String node '{node.Name}' has an invalid array child."
                );
            }

            TypeTreeBranch sizeNode = arrayNode.Children[0];
            TypeTreeBranch charNode = arrayNode.Children[1];
            int size = sizeNode.ReadInt(reader);
            if (size < 0)
            {
                throw new DeserializeException(
                    $"Negative string length at '{node.Name}'."
                );
            }

            if (size == 0)
            {
                arrayNode.CheckAlignmentWith(reader);
                return string.Empty;
            }

            int charWidth = charNode.Node.ByteSize;
            if (!charNode.DataType.IsCharType()
                || (charWidth != 1 && charWidth != 2))
            {
                throw new DeserializeException(
                    $"String node '{node.Name}' has an invalid char child."
                );
            }

            if ((charNode.Node.MetaFlags & 0x4000) == 0)
            {
                if (size > int.MaxValue / charWidth)
                {
                    throw new DeserializeException(
                        $"String length {size} overflows at '{node.Name}'."
                    );
                }

                int byteCount = size * charWidth;
                byte[] buffer = new byte[byteCount];
                TypeTreeReadUtils.ReadExactly(reader, buffer, byteCount);
                arrayNode.CheckAlignmentWith(reader);

                return charWidth == 1
                    ? Encoding.UTF8.GetString(buffer)
                    : Encoding.Unicode.GetString(buffer);
            }

            if (charWidth == 1)
            {
                byte[] buffer = new byte[size];
                for (int i = 0; i < size; ++i)
                {
                    buffer[i] = charNode.ReadByte(reader);
                }

                arrayNode.CheckAlignmentWith(reader);
                return Encoding.UTF8.GetString(buffer);
            }

            char[] chars = new char[size];
            for (int i = 0; i < size; ++i)
            {
                chars[i] = charNode.ReadChar(reader);
            }

            arrayNode.CheckAlignmentWith(reader);
            return new string(chars);
        }
    }
}
