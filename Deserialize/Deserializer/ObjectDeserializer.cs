using AssetManager.Tools;
using AssetManager.TypeTree;
using System.Collections.Generic;

namespace AssetManager.Deserialize.Deserializer
{
    internal sealed class ObjectDeserializer
    {
        private readonly EndianBinaryReader _reader;
        private readonly IReadOnlyList<TypeTreeBranch> _children;
        private readonly IReadOnlyDictionary<string, int> _positionByName;
        private int _index;

        public ObjectDeserializer(
            EndianBinaryReader reader,
            TypeTreeBranch objectNode,
            IReadOnlyDictionary<string, int> positionByName)
        {
            _reader = reader;
            _children = objectNode.Children;
            _positionByName = positionByName;
        }

        public bool TryReadNext(
            out int parameterPosition,
            out TypeTreeBranch node)
        {
            while (_index < _children.Count)
            {
                TypeTreeBranch child = _children[_index++];
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
