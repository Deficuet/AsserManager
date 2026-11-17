using AssetManager.Tools;
using PolyType;
using System;
using TypeTreeType = global::AssetManager.TypeTree.TypeTree;

namespace AssetManager.Deserialize.Controller
{
    internal sealed class SurrogateDeserializeController<T, TSurrogate>
        : DeserializeControllerBase<T>
    {
        private readonly Func<IDeserializeController<TSurrogate>>
            _surrogateControllerFactory;

        private readonly IMarshaler<T, TSurrogate> _marshaler;

        private IDeserializeController<TSurrogate> _surrogateController;

        public SurrogateDeserializeController(
            Func<IDeserializeController<TSurrogate>> surrogateControllerFactory,
            IMarshaler<T, TSurrogate> marshaler)
        {
            _surrogateControllerFactory = surrogateControllerFactory;
            _marshaler = marshaler;
        }

        protected override T ReadCore(
            EndianBinaryReader reader,
            TypeTreeType node)
        {
            _surrogateController ??= _surrogateControllerFactory();

            TSurrogate surrogate = _surrogateController.Deserialize(
                reader,
                node
            );
            return _marshaler.Unmarshal(surrogate);
        }
    }
}
