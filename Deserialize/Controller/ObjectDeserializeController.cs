using AssetManager.Deserialize.Deserializer;
using AssetManager.Tools;
using PolyType.Abstractions;
using System;
using System.Collections.Generic;
using TypeTreeType = global::AssetManager.TypeTree.TypeTree;

namespace AssetManager.Deserialize.Controller
{
    internal interface IParameterController<TArgumentState>
        where TArgumentState : IArgumentState
    {
        string Name { get; }

        int Position { get; }

        bool IsRequired { get; }

        void DeserializeInto(
            ref TArgumentState state,
            EndianBinaryReader reader,
            TypeTreeType node);
    }

    internal sealed class ParameterController<TArgumentState, TParameterType>
        : IParameterController<TArgumentState>
        where TArgumentState : IArgumentState
    {
        private readonly Setter<TArgumentState, TParameterType> _setter;
        private readonly Func<IDeserializeController<TParameterType>>
            _controllerFactory;

        private IDeserializeController<TParameterType> _controller;

        public ParameterController(
            string name,
            int position,
            bool isRequired,
            Setter<TArgumentState, TParameterType> setter,
            Func<IDeserializeController<TParameterType>> controllerFactory)
        {
            Name = name;
            Position = position;
            IsRequired = isRequired;
            _setter = setter;
            _controllerFactory = controllerFactory;
        }

        public string Name { get; }

        public int Position { get; }

        public bool IsRequired { get; }

        public void DeserializeInto(
            ref TArgumentState state,
            EndianBinaryReader reader,
            TypeTreeType node)
        {
            _controller ??= _controllerFactory();

            TParameterType value = _controller.Deserialize(reader, node);
            _setter(ref state, value);
        }
    }

    internal sealed class DefaultConstructorController<T>
        : DeserializeControllerBase<T>
    {
        private readonly Func<T> _constructor;

        public DefaultConstructorController(Func<T> constructor)
        {
            _constructor = constructor;
        }

        protected override T ReadCore(
            EndianBinaryReader reader,
            TypeTreeType node)
        {
            node.Skip(reader);
            return _constructor();
        }
    }

    internal sealed class ObjectDeserializeController<
        TDeclaringType,
        TArgumentState>
        : DeserializeControllerBase<TDeclaringType>
        where TArgumentState : IArgumentState
    {
        private readonly Func<TArgumentState> _argumentStateConstructor;
        private readonly Constructor<TArgumentState, TDeclaringType>
            _parameterizedConstructor;
        private readonly IParameterController<TArgumentState>[] _parameters;
        private readonly Dictionary<string, int> _positionByName;

        public ObjectDeserializeController(
            Func<TArgumentState> argumentStateConstructor,
            Constructor<TArgumentState, TDeclaringType>
                parameterizedConstructor,
            IParameterController<TArgumentState>[] parameters)
        {
            _argumentStateConstructor = argumentStateConstructor;
            _parameterizedConstructor = parameterizedConstructor;
            _parameters = parameters;
            _positionByName = new Dictionary<string, int>(
                StringComparer.Ordinal
            );

            for (int i = 0; i < parameters.Length; ++i)
            {
                IParameterController<TArgumentState> parameter = parameters[i] 
                    ?? throw new DeserializeException(
                        $"Missing parameter controller at position {i} for "
                        + $"{typeof(TDeclaringType)}."
                    );

                if (!_positionByName.TryAdd(parameter.Name, parameter.Position))
                {
                    throw new DeserializeException(
                        $"Duplicate constructor parameter '{parameter.Name}' "
                        + $"for {typeof(TDeclaringType)}."
                    );
                }
            }
        }

        protected override TDeclaringType ReadCore(
            EndianBinaryReader reader,
            TypeTreeType node)
        {
            TArgumentState state = _argumentStateConstructor();
            try
            {
                var objectDeserializer = new ObjectDeserializer(
                    reader,
                    node,
                    _positionByName
                );

                while (objectDeserializer.TryReadNext(
                    out int position,
                    out TypeTreeType child))
                {
                    _parameters[position].DeserializeInto(
                        ref state,
                        reader,
                        child
                    );
                }

                if (!state.AreRequiredArgumentsSet)
                {
                    List<string> missing = new();
                    for (int i = 0; i < _parameters.Length; ++i)
                    {
                        if (_parameters[i].IsRequired
                            && !state.IsArgumentSet(i))
                        {
                            missing.Add(_parameters[i].Name);
                        }
                    }

                    throw new DeserializeException(
                        $"Missing required members for "
                        + $"{typeof(TDeclaringType)}: "
                        + $"{string.Join(", ", missing)}."
                    );
                }

                return _parameterizedConstructor(ref state);
            }
            finally
            {
                state.Return();
            }
        }
    }
}
