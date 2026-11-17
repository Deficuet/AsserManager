using AssetManager.Deserialize.Controller;
using AssetManager.Tools;
using AssetManager.TypeTree;
using PolyType;
using System;
using System.Collections.Generic;

namespace AssetManager.Deserialize
{
    public sealed class DeserializeManager
    {
        private readonly ITypeShapeProvider _typeShapeProvider;
        private readonly Dictionary<Type, IDeserializeController> _controllers;
        private readonly DeserializeControllerVisitor _visitor;

        public DeserializeManager(ITypeShapeProvider typeShapeProvider)
        {
            _typeShapeProvider = typeShapeProvider
                ?? throw new ArgumentNullException(nameof(typeShapeProvider));
            _controllers = new Dictionary<Type, IDeserializeController>();
            _visitor = new DeserializeControllerVisitor(this);
        }

        public T Deserialize<T>(
            EndianBinaryReader reader,
            TypeTreeBranch rootNode)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            if (rootNode == null)
            {
                throw new ArgumentNullException(nameof(rootNode));
            }

            return GetController<T>().Deserialize(reader, rootNode);
        }

        internal IDeserializeController<T> GetController<T>()
        {
            Type type = typeof(T);
            if (_controllers.TryGetValue(
                type,
                out IDeserializeController existing))
            {
                return (IDeserializeController<T>)existing;
            }

            ITypeShape shape = _typeShapeProvider.GetTypeShape(type)
                ?? throw new DeserializeException(
                    $"No type shape is available for {type}."
                );

            if (shape is not ITypeShape<T> typedShape)
            {
                throw new DeserializeException(
                    $"Type shape for {type} has an unexpected generic type."
                );
            }

            IDeserializeController<T> controller = CreateController(typedShape);
            if (!typedShape.IsContextual)
            {
                _controllers.Add(type, controller);
            }

            return controller;
        }

        private IDeserializeController<T> CreateController<T>(
            ITypeShape<T> shape)
        {
            object result = shape.Accept(_visitor);
            if (result is IDeserializeController<T> controller)
            {
                return controller;
            }

            throw new DeserializeException(
                $"Type shape for {typeof(T)} did not produce a deserialize "
                + "controller."
            );
        }
    }
}
