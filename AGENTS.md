# AssetManager

## Project

- `netstandard2.1` library that reads Unity type trees and binary object data without a Unity runtime, plus a thin bundle-loading extension layer over AssetsTools.NET.
- Real entry points:
  - `TypeTreeUtils` converts TPK class data and Cpp2IL metadata into `TypeTreeResult`/`TypeTreeBranch`.
  - `DeserializeManager` deserializes a type tree into a CLR type using an externally supplied PolyType `ITypeShapeProvider`.
  - `Cpp2ILManager` owns the shared LibCpp2IL runtime and exposes custom class chains, field metadata, enum data, and type trees through `Cpp2IL.Core` and `AssetsTools.NET.Cpp2IL`.
  - `UnityAssetManagerExtension` loads bundles, either through `AssetsTools.NET.Extra.AssetsManager` or through a caller-supplied `IBundleLoadPipeline`.
- `AssetClassGenerator` and `AzurPromiliaDataAnalysis` consume this package. Public API changes can break them and their generated code.
- This assembly defines bundle-loading contracts only. Concrete bundle formats are implemented by consumers (for example `AzurPromiliaDataAnalysis.BundleFile`).

## Build / verify

- There is no Linux `dotnet` in this WSL environment. Use the Windows SDK:
  `"/mnt/c/Program Files/dotnet/dotnet.exe" build "D:\self\Programs\AzurPromiliaProject\app\AssetManager\AssetManager.csproj"`
- There are no tests, CI, lint, or formatter checks. Use a clean build as verification; the current baseline is 0 warnings and 0 errors.
- `GeneratePackageOnBuild` is enabled, so a build can also produce a `.nupkg`.
- Requires `Deficuet.AssetsTools.NET` **3.0.5-alpha.3 or newer**. That version adds `BundleFileInstance(AssetBundleFile, string, AssetBundleCompressionType)`, `AssetsManager.RegisterBundleFile`, and `AssetBundleHeader.RawEngineVersion`. Building against an older package fails to compile.
- `Cpp2ILManager` additionally needs the `Cpp2IlTempGenerator` attach/per-type API and `Samboy063.Cpp2IL.Core`; keep the `Deficuet.AssetsTools.NET.Cpp2IL` package in sync with that API.
- To verify against a local checkout of the AssetsTools fork, temporarily replace the `PackageReference` with a `ProjectReference` to `AssetsTools.NET.csproj`, build, then restore the package reference. Do not commit the project reference; the published package is the real dependency.
- Match existing C# style: block namespaces, explicit usings (`ImplicitUsings` is disabled), `LangVersion` 10, and UTF-8 BOM + CRLF for `.cs` files.

## Architecture

- `TypeTree/`: type-tree model and classification. `TypeTreeBranch` is the tree node class; `NodeDataTypeHelper` assigns structural `NodeDataType` values.
- `Deserialize/DeserializeManager.cs`: public entry point. Controllers and deserializers under `Deserialize/Controller` and `Deserialize/Deserializer` are `internal`.
- `Deserialize` controllers are built from PolyType shapes, cached by CLR `Type`, and child controllers resolve lazily.
- `Deserialize/Deserializer/ObjectDeserializer.cs` maps constructor parameters to type-tree children by name. Type trees list parent members first; generated shapes may list derived members first, so do not replace this with a positional parameter loop.
- `Classes/IUnityObject.cs` is the base interface used by generated Unity object classes. `UnityObjectContext<T>` pairs a data object with its `AssetsFile` and `AssetFileInfo`.
- `Collections/` contains `MapPair` and `MultiDictionary` in the `AssetManager.Collections` namespace.
- `Cpp2IL/`: `Cpp2ILManager` is the single owner of LibCpp2IL. It initializes `Cpp2IL.Core`, attaches a non-owning `Cpp2IlTempGenerator`, and builds custom inheritance chains. `Cpp2ILTypeInfo` stores the full template, own fields, order, tree, and enum metadata; chain templates are cloned before appending child fields.
- `BundleFile/`: bundle-loading contracts only. `IBundleLoadPipeline` produces a `BundleFileInstance`; `BundleLoadContext` carries `Pipeline`, `DefaultUnityVersion`, and `Key`.
- `UnityAssetManagerExtension`: wraps `AssetsManager`. It resolves `context ?? Context`; when the resolved `Pipeline` is null it forwards to `AssetsManager` untouched, otherwise it runs the pipeline and registers the result with `AssetsManager.RegisterBundleFile`.
- `Logger` (namespace `AssetManager`): static delegate hooks for this assembly's own diagnostics. Hosts install `Logger.Verbose/Debug/Info/Warning/Error` and decide which levels are interesting. Consumers keep their own logging and do not call `Logger`.

## Bundle loading rules

- A pipeline must return a complete `BundleFileInstance`: `Header`, `BlockAndDirInfo`, `Reader`, and a `DataReader` over **already decompressed** data with `DataIsCompressed = false`. Never return a still-compressed reader and rely on `BundleHelper.UnpackBundle`.
- Register pipelines' results through `AssetsManager.RegisterBundleFile`. It inserts into `Bundles`/`BundleLookup` and runs the type tree blob scan; writing `Bundles` directly skips the scan and silently breaks `LoadTypeTreeBlobsFromBundle` dependents.
- Pipelines take ownership of the stream they are given. The extension disposes it only when the pipeline throws.
- Context is replaced as a whole: the per-call `context` wins over the constructor one, and no field-by-field merging happens. Passing `new BundleLoadContext()` deliberately forces the plain AssetsTools path.
- Key precedence: `key` argument on the load call, then `BundleLoadContext.Key`, then whatever the pipeline instance was constructed with. The extension does not interpret keys.
- Version fallback belongs to the pipeline: write the effective version into `AssetBundleHeader.EngineVersion`, and set `RawEngineVersion` **only** when a value was actually replaced (for example a stripped `0.0.0`). Do not pre-populate `RawEngineVersion`.
- The pipeline must preserve raw header flags, including flags it does not understand. Do not clear bits to make the header look ordinary.

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
- `Cpp2IL.Core` and `Cpp2IlTempGenerator` share the static `LibCpp2IlMain` state. Only one `Cpp2ILManager` may be active; it owns initialization and `Cpp2IlApi.ResetInternalState()`. The attached generator must not reset or change the runtime.
- Custom chain templates are cloned before appending child fields. `AssetTypeTemplateField.Clone()` must preserve `Version`; do not mutate a cached parent template.
- `TypeTreeClassAttribute` supports enums. For generated enums it records the underlying type-tree primitive name (`int`, `unsigned int`, `SInt8`, ...), not the C# enum name.
- Do not use runtime reflection for deserialization; use the public PolyType shape interfaces and shape-provided delegates.
- `AssetBundleHeader.GetBundleInfoOffset()` and `GetFileDataOffset()` derive offsets from header field lengths only. They know nothing about extension blocks (an encrypted archive block sits between the header and the blocks info), so they are wrong for such bundles. Track positions from the reader instead of reusing them.
- The `BundleFileInstance(Stream, ...)` constructors re-parse with AssetsTools.NET and therefore cannot read bundles whose blocks info is not at the offset those formulas predict. Use the pre-built `BundleFileInstance(AssetBundleFile, string, AssetBundleCompressionType)` constructor instead.
- `Logger` is internal-only and always forwards; it has no level filtering and no compile-time switches. Do not add `#if` gates or log-level settings to it, and do not use it from the Unity host, which has its own `Logging`.

## Boundaries

- Do not add `Deficuet.AssetParser` or Serde; this project is a standalone replacement.
- Do not implement a concrete bundle format in this assembly. Keep `BundleFile/` to contracts; format-specific code belongs to the consumer.
- Do not edit `bin/`, `obj/`, or package output as source.
- Treat changes to public types, `NodeDataType` values, attributes, `DeserializeManager`, `Cpp2ILManager`, `IBundleLoadPipeline`, `BundleLoadContext`, and `Logger` as downstream-facing changes; coordinate them with `AssetClassGenerator` and `AzurPromiliaDataAnalysis`.
