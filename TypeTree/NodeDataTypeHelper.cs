using System.Collections.Generic;

namespace AssetManager.TypeTree
{
    public static class NodeDataTypeHelper
    {
        private static readonly HashSet<string> SIntTypeNameSet = new()
        {
            "SInt8",
            "SInt16", "short",
            "SInt32", "int",
            "SInt64", "long long"
        };

        private static readonly HashSet<string> UIntTypeNameSet = new()
        {
            "UInt8",
            "UInt16", "unsigned short",
            "UInt32", "unsigned int", "Type*",
            "UInt64", "unsigned long long", "FileSize"
        };

        private static readonly HashSet<string> DecimalTypeNameSet = new()
        {
            "float",
            "double"
        };

        public static bool IsSInt(this TypeTree node)
        {
            return SIntTypeNameSet.Contains(node.Type) && node.Children.Count == 0;
        }

        public static bool IsUInt(this TypeTree node)
        {
            return UIntTypeNameSet.Contains(node.Type) && node.Children.Count == 0;
        }

        public static bool IsDecimal(this TypeTree node)
        {
            return DecimalTypeNameSet.Contains(node.Type) && node.Children.Count == 0;
        }

        public static bool IsBool(this TypeTree node)
        {
            return node.Type == "bool" && node.Node.ByteSize == 1 && node.Children.Count == 0;
        }

        public static bool IsChar(this TypeTree node)
        {
            return node.Type == "char" && node.Children.Count == 0
                && (node.Node.ByteSize == 1 || node.Node.ByteSize == 2);
        }

        public static bool IsGuid(this TypeTree node)
        {
            return node.Type == "GUID" && node.Node.ByteSize == 16 && node.Children.Count == 4;
        }

        public static bool IsHash128(this TypeTree node)
        {
            return node.Type == "Hash128" && node.Node.ByteSize == 16 && node.Children.Count == 16;
        }

        public static bool IsArray(this TypeTree node)
        {
            if (node.Children.Count != 1)
            {
                return false;
            }
            var arrayNode = node.Children[0];
            if (arrayNode.Name != "Array" || arrayNode.Children.Count != 2)
            {
                return false;
            }
            return arrayNode.Children[0].DataType == NodeDataType.Int32;
        }

        public static bool IsString(this TypeTree node)
        {
            if (node.Type != "string")
            {
                return false;
            }
            if (!node.IsArray())
            {
                return false;
            }
            var elementType = node.Children[0].Children[1].DataType;
            return elementType.IsCharType();
        }

        public static bool IsPair(this TypeTree node)
        {
            return node.Type == "pair" && node.Children.Count == 2;
        }

        public static bool IsMap(this TypeTree node)
        {
            return node.Type == "map"
                && node.IsArray()
                && node.Children[0].Children[1].DataType == NodeDataType.Pair;
        }

        public static bool IsTypeless(this TypeTree node)
        {
            if (node.Type != "TypelessData")
            {
                return false;
            }
            if (node.Children.Count != 2)
            {
                return false;
            }
            if (node.Children[0].DataType != NodeDataType.Int32)
            {
                return false;
            }
            var elementType = node.Children[1].DataType;
            return elementType.IsSIntType() || elementType.IsUIntType();
        }

        public static bool IsByteArray(this TypeTree node)
        {
            return node.IsArray() && node.Children[0].Children[1].DataType == NodeDataType.UInt8;
        }

        public static NodeDataType MatchDataType(TypeTree node)
        {
            if (node.IsSInt())
            {
                return node.Node.ByteSize switch
                {
                    1 => NodeDataType.Int8,
                    2 => NodeDataType.Int16,
                    4 => NodeDataType.Int32,
                    8 => NodeDataType.Int64,
                    _ => NodeDataType.Class
                };
            }
            else if (node.IsUInt())
            {
                return node.Node.ByteSize switch
                {
                    1 => NodeDataType.UInt8,
                    2 => NodeDataType.UInt16,
                    4 => NodeDataType.UInt32,
                    8 => NodeDataType.UInt64,
                    _ => NodeDataType.Class
                };
            }
            else if (node.IsDecimal())
            {
                return node.Node.ByteSize switch
                {
                    4 => NodeDataType.Float,
                    8 => NodeDataType.Double,
                    _ => NodeDataType.Class
                };
            }
            else if (node.IsBool())
            {
                return NodeDataType.Bool;
            }
            else if (node.IsChar())
            {
                return node.Node.ByteSize switch
                {
                    1 => NodeDataType.Char,
                    2 => NodeDataType.WideChar,
                    _ => NodeDataType.Class
                };
            }
            else if (node.IsString())
            {
                return NodeDataType.String;
            }
            else if (node.IsMap())
            {
                return NodeDataType.Map;
            }
            else if (node.IsPair())
            {
                return NodeDataType.Pair;
            }
            else if (node.IsHash128())
            {
                return NodeDataType.Hash128;
            }
            else if (node.IsTypeless())
            {
                return NodeDataType.Typeless;
            }
            else if (node.IsByteArray())
            {
                return NodeDataType.ByteArray;
            }
            else if (node.IsArray())
            {
                return NodeDataType.Array;
            }
            else
            {
                return NodeDataType.Class;
            }
        }

        public static bool IsSIntType(this NodeDataType type)
        {
            return type < NodeDataType.UInt8;
        }

        public static bool IsUIntType(this NodeDataType type)
        {
            return type >= NodeDataType.UInt8 && type < NodeDataType.Float;
        }

        public static bool IsDecimalType(this NodeDataType type)
        {
            return type >= NodeDataType.Float && type < NodeDataType.Char;
        }

        public static bool IsCharType(this NodeDataType type)
        {
            return type >= NodeDataType.Char && type < NodeDataType.Bool;
        }

        public static bool IsValueType(this NodeDataType type)
        {
            return type <= NodeDataType.ByteArray;
        }

        public static bool IsArrayType(this NodeDataType type)
        {
            return type >= NodeDataType.ByteArray && type < NodeDataType.Pair;
        }

        public static bool IsCompositeArrayType(this NodeDataType type)
        {
            return type >= NodeDataType.Array && type < NodeDataType.Pair;
        }

        public static bool IsCompositeType(this NodeDataType type)
        {
            return type >= NodeDataType.Pair && type < NodeDataType.Unknown;
        }
    }
}
