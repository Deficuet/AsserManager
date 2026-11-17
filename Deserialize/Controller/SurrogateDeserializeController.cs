using AssetManager.Tools;
using AssetManager.TypeTree;
using PolyType;
using System;

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
            TypeTreeBranch node)
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
