namespace AssetManager.TypeTree
{
    public enum NodeDataType : byte
    {
        Int8,
        Int16,
        Int32,
        Int64,
        //Int128,

        UInt8,
        UInt16,
        UInt32,
        UInt64,
        //UInt128,

        Float,
        Double,
        //Decimal,

        Char,
        WideChar,

        Bool,

        Pos2f,       // Vector2f
        Pos3f,       // Vector3f
        Pos4f,       // Vector4f or Quaternionf
        ColorRGBA4f,    // four float channels
        ColorRGBA32,    // packed rgba
        Matrix4x4f,
        Hash128,
        Guid,

        ByteArray,
        Typeless,
        String,

        Array,
        Map,

        Pair,

        PPtr,
        Class,

        Unknown
    }
}
