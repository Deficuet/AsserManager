using AssetsTools.NET;
using PolyType;
using PolyType.Abstractions;
using System;

namespace AssetManager.Deserialize.Controller
{
    internal sealed class DeserializeControllerVisitor : TypeShapeVisitor
    {
        private readonly DeserializeManager _manager;

        public DeserializeControllerVisitor(DeserializeManager manager)
        {
            _manager = manager;
        }

        public override object VisitObject<T>(
            IObjectTypeShape<T> objectShape,
            object state = null)
        {
            if (PrimitiveDeserializeControllers.TryCreate<T>(
                out IDeserializeController<T> primitive))
            {
                return primitive;
            }

            if (typeof(T) == typeof(string))
            {
                return new StringDeserializeController();
            }

            if (objectShape.Constructor == null)
            {
                throw new DeserializeException(
                    $"Type {typeof(T)} has no constructor shape."
                );
            }

            object result = objectShape.Constructor.Accept(this, state);
            if (result is IDeserializeController<T> controller)
            {
                return controller;
            }

            throw new DeserializeException(
                $"Constructor shape for {typeof(T)} did not produce a "
                + "deserialize controller."
            );
        }

        public override object VisitConstructor<TDeclaringType, TArgumentState>(
            IConstructorShape<TDeclaringType, TArgumentState> constructorShape,
            object state = null)
        {
            try
            {
                if (constructorShape.Parameters.Count == 0)
                {
                    Func<TDeclaringType> defaultConstructor =
                        constructorShape.GetDefaultConstructor()
                        ?? throw new DeserializeException(
                            $"Type {typeof(TDeclaringType)} has no default "
                            + "constructor."
                        );

                    return new DefaultConstructorController<TDeclaringType>(
                        defaultConstructor
                    );
                }

                Func<TArgumentState> argumentStateConstructor =
                    constructorShape.GetArgumentStateConstructor();
                Constructor<TArgumentState, TDeclaringType>
                    parameterizedConstructor =
                        constructorShape.GetParameterizedConstructor();

                if (argumentStateConstructor == null)
                {
                    throw new DeserializeException(
                        $"Type {typeof(TDeclaringType)} has no argument state "
                        + "constructor."
                    );
                }

                if (parameterizedConstructor == null)
                {
                    throw new DeserializeException(
                        $"Type {typeof(TDeclaringType)} has no parameterized "
                        + "constructor."
                    );
                }

                var parameters =
                    new IParameterController<TArgumentState>[
                        constructorShape.Parameters.Count
                    ];

                foreach (IParameterShape parameter
                    in constructorShape.Parameters)
                {
                    object parameterResult = parameter.Accept(this);
                    if (parameterResult
                        is not IParameterController<TArgumentState>
                            parameterController)
                    {
                        throw new DeserializeException(
                            $"Parameter '{parameter.Name}' for "
                            + $"{typeof(TDeclaringType)} did not produce a "
                            + "deserialize controller."
                        );
                    }

                    if (parameterController.Position < 0
                        || parameterController.Position >= parameters.Length)
                    {
                        throw new DeserializeException(
                            $"Parameter '{parameterController.Name}' for "
                            + $"{typeof(TDeclaringType)} has invalid position "
                            + $"{parameterController.Position}."
                        );
                    }

                    if (parameters[parameterController.Position] != null)
                    {
                        throw new DeserializeException(
                            $"Duplicate parameter position "
                            + $"{parameterController.Position} for "
                            + $"{typeof(TDeclaringType)}."
                        );
                    }

                    parameters[parameterController.Position] =
                        parameterController;
                }

                return new ObjectDeserializeController<
                    TDeclaringType,
                    TArgumentState>(
                    argumentStateConstructor,
                    parameterizedConstructor,
                    parameters
                );
            }
            catch (Exception exception)
                when (exception is not DeserializeException)
            {
                throw new DeserializeException(
                    $"Failed to create a deserialization constructor for "
                    + $"{typeof(TDeclaringType)}.",
                    exception
                );
            }
        }

        public override object VisitParameter<TArgumentState, TParameterType>(
            IParameterShape<TArgumentState, TParameterType> parameterShape,
            object state = null)
        {
            try
            {
                Setter<TArgumentState, TParameterType> setter =
                    parameterShape.GetSetter();
                return new ParameterController<
                    TArgumentState,
                    TParameterType>(
                    parameterShape.Name,
                    parameterShape.Position,
                    parameterShape.IsRequired,
                    setter,
                    () => _manager.GetController<TParameterType>()
                );
            }
            catch (Exception exception)
                when (exception is not DeserializeException)
            {
                throw new DeserializeException(
                    $"Failed to create a deserialization parameter for "
                    + $"'{parameterShape.Name}'.",
                    exception
                );
            }
        }

        public override object VisitEnumerable<TEnumerable, TElement>(
            IEnumerableTypeShape<TEnumerable, TElement> enumerableShape,
            object state = null)
        {
            if (!typeof(TEnumerable).IsArray || enumerableShape.Rank != 1)
            {
                throw new DeserializeException(
                    $"Only rank-1 arrays are supported, got "
                    + $"{typeof(TEnumerable)}."
                );
            }

            return new ArrayDeserializeController<TEnumerable, TElement>(
                () => _manager.GetController<TElement>()
            );
        }

        public override object VisitEnum<TEnum, TUnderlying>(
            IEnumTypeShape<TEnum, TUnderlying> enumShape,
            object state = null)
        {
            return new EnumDeserializeController<TEnum, TUnderlying>(
                () => _manager.GetController<TUnderlying>()
            );
        }

        public override object VisitSurrogate<T, TSurrogate>(
            ISurrogateTypeShape<T, TSurrogate> surrogateShape,
            object state = null)
        {
            if (typeof(T) == typeof(Hash128)
                && typeof(TSurrogate) == typeof(byte[]))
            {
                return (IDeserializeController<T>)(object)
                    new Hash128DeserializeController(
                        (IMarshaler<Hash128, byte[]>)surrogateShape.Marshaler
                    );
            }

            return new SurrogateDeserializeController<T, TSurrogate>(
                () => _manager.GetController<TSurrogate>(),
                surrogateShape.Marshaler
            );
        }
    }
}
