using AssetManager.Deserialize.Deserializer;
using AssetManager.Tools;
using AssetManager.TypeTree;
using AssetsTools.NET;
using PolyType;

namespace AssetManager.Deserialize.Controller
{
    internal sealed class Hash128DeserializeController
        : DeserializeControllerBase<Hash128>
    {
        private readonly IMarshaler<Hash128, byte[]> _marshaler;

        public Hash128DeserializeController(
            IMarshaler<Hash128, byte[]> marshaler)
        {
            _marshaler = marshaler;
        }

        protected override Hash128 ReadCore(
            EndianBinaryReader reader,
            TypeTreeBranch node)
        {
            byte[] data = new byte[16];

            bool needsAlignment = false;
            for (int i = 0; i < node.Children.Count; ++i)
            {
                if (node.Children[i].ShouldAlignStream)
                {
                    needsAlignment = true;
                    break;
                }
            }

            if (needsAlignment)
            {
                for (int i = 0; i < 16; ++i)
                {
                    data[i] = node.Children[i].ReadByte(reader);
                }
            }
            else
            {
                TypeTreeReadUtils.ReadExactly(reader, data, 16);
            }

            return _marshaler.Unmarshal(data);
        }
    }
}
