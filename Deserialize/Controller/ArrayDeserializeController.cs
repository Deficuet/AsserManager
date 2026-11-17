using AssetManager.Deserialize.Deserializer;
using AssetManager.Tools;
using AssetManager.TypeTree;
using System;

namespace AssetManager.Deserialize.Controller
{
    internal sealed class ArrayDeserializeController<TEnumerable, TElement>
        : DeserializeControllerBase<TEnumerable>
    {
        private readonly Func<IDeserializeController<TElement>>
            _elementControllerFactory;

        private IDeserializeController<TElement> _elementController;

        public ArrayDeserializeController(
            Func<IDeserializeController<TElement>> elementControllerFactory)
        {
            _elementControllerFactory = elementControllerFactory;
        }

        protected override TEnumerable ReadCore(
            EndianBinaryReader reader,
            TypeTreeBranch node)
        {
            ArrayNodeInfo info = ArrayNodeInfo.Create(node);
            int count = info.SizeNode.ReadInt(reader);
            if (count < 0)
            {
                throw new DeserializeException(
                    $"Negative array length at '{node.Name}'."
                );
            }

            _elementController ??= _elementControllerFactory();

            TElement[] values;
            if (!info.ElementNode.ShouldAlignStream
                && _elementController
                    is IPrimitiveController<TElement> primitive
                && primitive.TryGetElementSize(
                    info.ElementNode,
                    out int byteSize))
            {
                values = primitive.ReadArray(
                    reader,
                    count,
                    info.ElementNode,
                    byteSize
                );
            }
            else
            {
                values = new TElement[count];
                for (int i = 0; i < count; ++i)
                {
                    values[i] = _elementController.Deserialize(
                        reader,
                        info.ElementNode
                    );
                }
            }

            info.ArrayNode.CheckAlignmentWith(reader);
            return (TEnumerable)(object)values;
        }
    }
}
