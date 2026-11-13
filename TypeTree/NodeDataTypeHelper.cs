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

        public static bool EvalIsSInt(this TypeTree node)
        {
            return SIntTypeNameSet.Contains(node.Type) && node.Children.Count == 0;
        }

        public static bool EvalIsUInt(this TypeTree node)
        {
            return UIntTypeNameSet.Contains(node.Type) && node.Children.Count == 0;
        }

        public static bool EvalIsDecimal(this TypeTree node)
        {
            return DecimalTypeNameSet.Contains(node.Type) && node.Children.Count == 0;
        }

        public static bool EvalIsBool(this TypeTree node)
        {
            return node.Type == "bool" && node.Node.ByteSize == 1 && node.Children.Count == 0;
        }

        public static bool EvalIsChar(this TypeTree node)
        {
            return node.Type == "char" && node.Children.Count == 0
                && (node.Node.ByteSize == 1 || node.Node.ByteSize == 2);
        }

        public static bool EvalIsGuid(this TypeTree node)
        {
            return node.Type == "GUID" && node.Node.ByteSize == 16 && node.Children.Count == 4;
        }

        public static bool EvalIsHash128(this TypeTree node)
        {
            return node.Type == "Hash128" && node.Node.ByteSize == 16 && node.Children.Count == 16;
        }

        public static bool EvalIsArray(this TypeTree node)
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

        public static bool EvalIsString(this TypeTree node)
        {
            if (node.Type != "string")
            {
                return false;
            }
            if (!node.EvalIsArray())
            {
                return false;
            }
            var elementType = node.Children[0].Children[1].DataType;
            return elementType.IsCharType();
        }

        public static bool EvalIsPair(this TypeTree node)
        {
            return node.Type == "pair" && node.Children.Count == 2;
        }

        public static bool EvalIsMap(this TypeTree node)
        {
            return node.Type == "map"
                && node.EvalIsArray()
                && node.Children[0].Children[1].DataType == NodeDataType.Pair;
        }

        public static bool EvalIsTypeless(this TypeTree node)
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

        public static bool EvalIsByteArray(this TypeTree node)
        {
            return node.EvalIsArray() && node.Children[0].Children[1].DataType == NodeDataType.UInt8;
        }

        public static NodeDataType MatchDataType(TypeTree node)
        {
            if (node.EvalIsSInt())
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
            else if (node.EvalIsUInt())
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
            else if (node.EvalIsDecimal())
            {
                return node.Node.ByteSize switch
                {
                    4 => NodeDataType.Float,
                    8 => NodeDataType.Double,
                    _ => NodeDataType.Class
                };
            }
            else if (node.EvalIsBool())
            {
                return NodeDataType.Bool;
            }
            else if (node.EvalIsChar())
            {
                return node.Node.ByteSize switch
                {
                    1 => NodeDataType.Char,
                    2 => NodeDataType.WideChar,
                    _ => NodeDataType.Class
                };
            }
            else if (node.EvalIsString())
            {
                return NodeDataType.String;
            }
            else if (node.EvalIsMap())
            {
                return NodeDataType.Map;
            }
            else if (node.EvalIsPair())
            {
                return NodeDataType.Pair;
            }
            else if (node.EvalIsHash128())
            {
                return NodeDataType.Hash128;
            }
            else if (node.EvalIsGuid())
            {
                return NodeDataType.Guid;
            }
            else if (node.EvalIsTypeless())
            {
                return NodeDataType.Typeless;
            }
            else if (node.EvalIsByteArray())
            {
                return NodeDataType.ByteArray;
            }
            else if (node.EvalIsArray())
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
