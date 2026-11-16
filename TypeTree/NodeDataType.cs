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
        Pos2,       // Vector2f
        Pos3,       // Vector3f
        Pos4,       // Vector4f or Quaternionf
        ColorRGBA,  // four float channels
        Matrix4x4f,
        PPtr,

        Hash128,
        Guid,

        ByteArray,
        Typeless,
        String,

        Array,
        Map,

        Pair,
        Class,

        Unknown
    }
}
