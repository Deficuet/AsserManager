using AssetManager.TypeTree;
using TypeTreeType = global::AssetManager.TypeTree.TypeTree;

namespace AssetManager.Deserialize.Deserializer
{
    internal readonly struct ArrayNodeInfo
    {
        private ArrayNodeInfo(
            TypeTreeType arrayNode,
            TypeTreeType sizeNode,
            TypeTreeType elementNode)
        {
            ArrayNode = arrayNode;
            SizeNode = sizeNode;
            ElementNode = elementNode;
        }

        public TypeTreeType ArrayNode { get; }

        public TypeTreeType SizeNode { get; }

        public TypeTreeType ElementNode { get; }

        public static ArrayNodeInfo Create(TypeTreeType node)
        {
            if (node.DataType == NodeDataType.Typeless)
            {
                return CreateCore(node, node);
            }

            if (node.DataType == NodeDataType.ByteArray
                || node.DataType == NodeDataType.Array
                || node.DataType == NodeDataType.Map)
            {
                if (node.Children.Count == 0)
                {
                    throw new DeserializeException(
                        $"Array node '{node.Name}' has no array child."
                    );
                }

                return CreateCore(node, node.Children[0]);
            }

            throw new DeserializeException(
                $"Expected array type tree node, got {node.DataType} "
                + $"at '{node.Name}'."
            );
        }

        private static ArrayNodeInfo CreateCore(
            TypeTreeType rootNode,
            TypeTreeType arrayNode)
        {
            if (arrayNode.Children.Count < 2)
            {
                throw new DeserializeException(
                    $"Array node '{rootNode.Name}' has an invalid layout."
                );
            }

            TypeTreeType sizeNode = arrayNode.Children[0];
            TypeTreeType elementNode = arrayNode.Children[1];
            if (sizeNode.DataType != NodeDataType.Int32)
            {
                throw new DeserializeException(
                    $"Array node '{rootNode.Name}' has a non-int size node."
                );
            }

            return new ArrayNodeInfo(arrayNode, sizeNode, elementNode);
        }
    }
}
