using AssetManager.Tools;
using System.Collections.Generic;
using TypeTreeType = global::AssetManager.TypeTree.TypeTree;

namespace AssetManager.Deserialize.Deserializer
{
    internal sealed class ObjectDeserializer
    {
        private readonly EndianBinaryReader _reader;
        private readonly IReadOnlyList<TypeTreeType> _children;
        private readonly IReadOnlyDictionary<string, int> _positionByName;
        private int _index;

        public ObjectDeserializer(
            EndianBinaryReader reader,
            TypeTreeType objectNode,
            IReadOnlyDictionary<string, int> positionByName)
        {
            _reader = reader;
            _children = objectNode.Children;
            _positionByName = positionByName;
        }

        public bool TryReadNext(
            out int parameterPosition,
            out TypeTreeType node)
        {
            while (_index < _children.Count)
            {
                TypeTreeType child = _children[_index++];
                if (_positionByName.TryGetValue(
                    child.Name,
                    out parameterPosition))
                {
                    node = child;
                    return true;
                }

                child.Skip(_reader);
                child.CheckAlignmentWith(_reader);
            }

            parameterPosition = -1;
            node = null;
            return false;
        }
    }
}
