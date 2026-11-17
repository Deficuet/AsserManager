using AssetManager.Tools;
using AssetManager.TypeTree;
using TypeTreeType = global::AssetManager.TypeTree.TypeTree;

namespace AssetManager.Deserialize.Controller
{
    internal interface IDeserializeController
    {
        object Deserialize(EndianBinaryReader reader, TypeTreeType node);
    }

    internal interface IDeserializeController<T> : IDeserializeController
    {
        new T Deserialize(EndianBinaryReader reader, TypeTreeType node);
    }
}
