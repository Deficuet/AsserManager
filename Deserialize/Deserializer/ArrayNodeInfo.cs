using AssetManager.TypeTree;

namespace AssetManager.Deserialize.Deserializer
{
    internal readonly struct ArrayNodeInfo
    {
        private ArrayNodeInfo(
            TypeTreeBranch arrayNode,
            TypeTreeBranch sizeNode,
            TypeTreeBranch elementNode)
        {
            ArrayNode = arrayNode;
            SizeNode = sizeNode;
            ElementNode = elementNode;
        }

        public TypeTreeBranch ArrayNode { get; }

        public TypeTreeBranch SizeNode { get; }

        public TypeTreeBranch ElementNode { get; }

        public static ArrayNodeInfo Create(TypeTreeBranch node)
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
            TypeTreeBranch rootNode,
            TypeTreeBranch arrayNode)
        {
            if (arrayNode.Children.Count < 2)
            {
                throw new DeserializeException(
                    $"Array node '{rootNode.Name}' has an invalid layout."
                );
            }

            TypeTreeBranch sizeNode = arrayNode.Children[0];
            TypeTreeBranch elementNode = arrayNode.Children[1];
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
