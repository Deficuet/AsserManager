using AssetManager.Tools;
using AssetManager.TypeTree;
using System;
using System.Runtime.CompilerServices;

namespace AssetManager.Deserialize.Controller
{
    internal sealed class EnumDeserializeController<TEnum, TUnderlying>
        : DeserializeControllerBase<TEnum>
        where TEnum : struct, Enum
        where TUnderlying : unmanaged
    {
        private readonly Func<IDeserializeController<TUnderlying>>
            _underlyingControllerFactory;

        private IDeserializeController<TUnderlying> _underlyingController;

        public EnumDeserializeController(
            Func<IDeserializeController<TUnderlying>> underlyingControllerFactory)
        {
            _underlyingControllerFactory = underlyingControllerFactory;
        }

        protected override TEnum ReadCore(
            EndianBinaryReader reader,
            TypeTreeBranch node)
        {
            _underlyingController ??= _underlyingControllerFactory();

            TUnderlying underlying = _underlyingController.Deserialize(
                reader,
                node
            );
            return Unsafe.As<TUnderlying, TEnum>(ref underlying);
        }
    }
}
