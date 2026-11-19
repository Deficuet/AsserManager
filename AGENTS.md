# AssetManager

## Project

- `netstandard2.1` library that reads Unity type trees and binary object data without a Unity runtime.
- Real entry points:
  - `TypeTreeUtils` converts TPK class data and Cpp2IL metadata into `TypeTreeResult`/`TypeTreeBranch`.
  - `DeserializeManager` deserializes a type tree into a CLR type using an externally supplied PolyType `ITypeShapeProvider`.
- `UnityAssetManager` is currently an empty placeholder; do not treat it as the API entry point.
- `AssetClassGenerator` consumes this package. Public API changes can break the generator and its generated code.

## Build / verify

- There is no Linux `dotnet` in this WSL environment. Use the Windows SDK:
  `"/mnt/c/Program Files/dotnet/dotnet.exe" build "D:\self\Programs\AzurPromiliaProject\app\AssetManager\AssetManager.csproj"`
- There are no tests, CI, lint, or formatter checks. Use a clean build as verification; the current baseline is 0 warnings and 0 errors.
- `GeneratePackageOnBuild` is enabled, so a build can also produce a `.nupkg`.
- Match existing C# style: block namespaces, explicit usings (`ImplicitUsings` is disabled), `LangVersion` 10, and UTF-8 BOM + CRLF for `.cs` files.

## Architecture

- `TypeTree/`: type-tree model and classification. `TypeTreeBranch` is the tree node class; `NodeDataTypeHelper` assigns structural `NodeDataType` values.
- `Deserialize/DeserializeManager.cs`: public entry point. Controllers and deserializers under `Deserialize/Controller` and `Deserialize/Deserializer` are `internal`.
- `Deserialize` controllers are built from PolyType shapes, cached by CLR `Type`, and child controllers resolve lazily.
- `Deserialize/Deserializer/ObjectDeserializer.cs` maps constructor parameters to type-tree children by name. Type trees list parent members first; generated shapes may list derived members first, so do not replace this with a positional parameter loop.
- `Classes/IUnityObject.cs` is the base interface used by generated Unity object classes. `UnityObjectContext<T>` pairs a data object with its `AssetsFile` and `AssetFileInfo`.
- `Collections/` contains `MapPair` and `MultiDictionary` in the `AssetManager.Collections` namespace.

## Deserialization rules

- Type shapes are not generated in this repository; callers inject a PolyType provider (for example the UObjects generated provider). Do not add a `UnityEngine` or UObjects reference here.
- Only rank-1 arrays are supported. `MultiDictionary<K, V>` is handled through a `MapPair<K, V>[]` surrogate.
- `Hash128` is read as exactly 16 raw bytes with no length prefix.
- Missing required members throw `DeserializeException`; unknown type-tree children are skipped.
- Object controllers rent `IArgumentState`; always return it in `finally`.

## Gotchas

- Do not reintroduce a class named `TypeTree`; it collides with the `AssetManager.TypeTree` namespace. Use `TypeTreeBranch`.
- Use `TypeTreeBranch.ShouldAlignStream`; do not test `MetaFlags & 0x4000` directly. Alignment must be checked after reading or skipping every consumed node.
- `NodeDataType` has no `ByteArray` member. Array-like nodes classify as `Typeless`, `String`, `Array`, or `Map`; byte and primitive arrays use `Array`/`Typeless` and the array controller fast path.
- `NodeDataType` range predicates in `NodeDataTypeHelper` depend on enum ordering. Adding or moving enum members requires reviewing `IsPrimitiveType`, `IsCompositeValueType`, `IsValueType`, `IsArrayType`, and related helpers.
- `TypeTreeBranch.Skip` uses `IsPrimitiveType()` for fixed-size `ByteSize` skips, but recursively skips `Children` for `IsCompositeValueType()` (Pos/Color/Matrix/Hash128/Guid) and `IsCompositeType()`. Do not collapse this back to `IsValueType()`: Cpp2IL composite value nodes can report `ByteSize = -1`.
- `NodeDataTypeHelper` intentionally avoids `ByteSize` checks for structural Pos/Color/Matrix/PPtr/Guid/Hash128 detection. Cpp2IL trees can report `ByteSize = -1` for class-like nodes.
- `EndianBinaryReader` defaults to `BigEndian`; Unity 2022 asset data used here is little-endian. Pass `ByteOrder.LittleEndian` when constructing it for that data.
- `TypeTreeSourceAttribute` takes `unityVersion` first, then assembly name, namespace, and class name. Generated code from `AssetClassGenerator` relies on this signature.
- Do not use runtime reflection for deserialization; use the public PolyType shape interfaces and shape-provided delegates.

## Boundaries

- Do not add `Deficuet.AssetParser` or Serde; this project is a standalone replacement.
- Do not edit `bin/`, `obj/`, or package output as source.
- Treat changes to public types, `NodeDataType` values, attributes, and `DeserializeManager` as downstream-facing changes; coordinate them with `AssetClassGenerator`.
