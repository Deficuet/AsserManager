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
