using AssetManager.Tools;
using AssetManager.TypeTree;

namespace AssetManager.Deserialize.Controller
{
    internal interface IDeserializeController
    {
        object Deserialize(EndianBinaryReader reader, TypeTreeBranch node);
    }

    internal interface IDeserializeController<T> : IDeserializeController
    {
        new T Deserialize(EndianBinaryReader reader, TypeTreeBranch node);
    }
}
