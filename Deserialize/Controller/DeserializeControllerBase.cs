using AssetManager.Tools;
using AssetManager.TypeTree;
using System;

namespace AssetManager.Deserialize.Controller
{
    internal abstract class DeserializeControllerBase<T> : IDeserializeController<T>
    {
        public T Deserialize(EndianBinaryReader reader, TypeTreeBranch node)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            T value = ReadCore(reader, node);
            node.CheckAlignmentWith(reader);
            return value;
        }

        object IDeserializeController.Deserialize(
            EndianBinaryReader reader,
            TypeTreeBranch node)
        {
            return Deserialize(reader, node);
        }

        protected abstract T ReadCore(
            EndianBinaryReader reader,
            TypeTreeBranch node);
    }
}
