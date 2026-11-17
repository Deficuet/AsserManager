# Asset Manager

An asset manager for Unity objects. Can be used without Unity environment.

## Deserialization

`DeserializeManager` is the public entry point. It accepts a PolyType
`ITypeShapeProvider`, caches deserialization controllers per CLR type, and
deserializes a type tree node:

```csharp
var manager = new DeserializeManager(
    UObjectShapeProvider.GeneratedTypeShapeProvider
);

AnimatorController controller = manager.Deserialize<AnimatorController>(
    reader,
    rootNode
);
```

Supported behavior:

- Built-in and custom type tree nodes are read from `EndianBinaryReader`.
- Only rank-1 arrays are supported.
- Primitive arrays and strings use a bulk-read fast path when the element node
  has no alignment flag.
- `Hash128` is read as 16 raw bytes without a length prefix.
- `MultiDictionary<K, V>` is deserialized through its `MapPair<K, V>[]`
  surrogate.
- Missing required members throw `DeserializeException`; unknown type tree
  fields are skipped.
- Every consumed type tree node checks its alignment flag before returning.
