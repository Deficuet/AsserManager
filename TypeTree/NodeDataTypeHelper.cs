using System;
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

        private static readonly string[] Pos2FieldNames = { "x", "y" };
        private static readonly string[] Pos3FieldNames = { "x", "y", "z" };
        private static readonly string[] Pos4FieldNames = { "x", "y", "z", "w" };
        private static readonly string[] Color4fFieldNames = { "r", "g", "b", "a" };
        private static readonly string[] Matrix4x4FieldNames =
        {
            "e00", "e01", "e02", "e03",
            "e10", "e11", "e12", "e13",
            "e20", "e21", "e22", "e23",
            "e30", "e31", "e32", "e33"
        };

        private static bool EvalHasFloatChildren(TypeTreeBranch node, string[] fieldNames)
        {
            if (node.Children.Count != fieldNames.Length)
            {
                return false;
            }

            for (int i = 0; i < fieldNames.Length; ++i)
            {
                TypeTreeBranch child = node.Children[i];
                if (child.DataType != NodeDataType.Float
                    || !string.Equals(child.Name, fieldNames[i], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool EvalIsSInt(this TypeTreeBranch node)
        {
            return SIntTypeNameSet.Contains(node.Type) && node is { Children.Count: 0 };
        }

        public static bool EvalIsUInt(this TypeTreeBranch node)
        {
            return UIntTypeNameSet.Contains(node.Type) && node is { Children.Count: 0 };
        }

        public static bool EvalIsDecimal(this TypeTreeBranch node)
        {
            return DecimalTypeNameSet.Contains(node.Type) && node is { Children.Count: 0 };
        }

        public static bool EvalIsBool(this TypeTreeBranch node)
        {
            return node is { Type: "bool", Node.ByteSize: 1, Children.Count: 0 };
        }

        public static bool EvalIsChar(this TypeTreeBranch node)
        {
            return node is { Type: "char", Children.Count: 0 }
                && node.Node.ByteSize is 1 or 2;
        }

        public static bool EvalIsPos2f(this TypeTreeBranch node)
        {
            return EvalHasFloatChildren(node, Pos2FieldNames);
        }

        public static bool EvalIsPos3f(this TypeTreeBranch node)
        {
            return EvalHasFloatChildren(node, Pos3FieldNames);
        }

        public static bool EvalIsPos4f(this TypeTreeBranch node)
        {
            return EvalHasFloatChildren(node, Pos4FieldNames);
        }

        public static bool EvalIsColorRGBA4f(this TypeTreeBranch node)
        {
            return EvalHasFloatChildren(node, Color4fFieldNames);
        }

        public static bool EvalIsColorRGBA32(this TypeTreeBranch node)
        {
            return node is { Children.Count: 1 }
                && node.Children[0] is { Name: "rgba", DataType: NodeDataType.UInt32 };
        }

        public static bool EvalIsMatrix4x4f(this TypeTreeBranch node)
        {
            return EvalHasFloatChildren(node, Matrix4x4FieldNames);
        }

        public static bool EvalIsGuid(this TypeTreeBranch node)
        {
            return node is { Type: "GUID", Children.Count: 4 };
        }

        public static bool EvalIsHash128(this TypeTreeBranch node)
        {
            return node is { Type: "Hash128", Children.Count: 16 };
        }

        public static bool EvalIsArray(this TypeTreeBranch node)
        {
            return node is { Children.Count: 1 }
                && node.Children[0] is { Name: "Array", Children.Count: 2 }
                && node.Children[0].Children[0] is { DataType: NodeDataType.Int32 };
        }

        public static bool EvalIsString(this TypeTreeBranch node)
        {
            return node is { Type: "string" }
                && node.EvalIsArray()
                && node.Children[0].Children[1].DataType.IsCharType();
        }

        public static bool EvalIsPair(this TypeTreeBranch node)
        {
            return node is { Type: "pair", Children.Count: 2 };
        }

        public static bool EvalIsMap(this TypeTreeBranch node)
        {
            return node is { Type: "map" }
                && node.EvalIsArray()
                && node.Children[0].Children[1] is { DataType: NodeDataType.Pair };
        }

        public static bool EvalIsTypeless(this TypeTreeBranch node)
        {
            return node is { Type: "TypelessData", Children.Count: 2 }
                && node.Children[0] is { DataType: NodeDataType.Int32 }
                && (node.Children[1].DataType.IsSIntType()
                    || node.Children[1].DataType.IsUIntType());
        }

        public static bool EvalIsByteArray(this TypeTreeBranch node)
        {
            return node.EvalIsArray()
                && node.Children[0].Children[1] is { DataType: NodeDataType.UInt8 };
        }

        public static bool EvalIsPPtr(this TypeTreeBranch node)
        {
            return node is { Children.Count: 2 }
                && node.Type.StartsWith("PPtr<", StringComparison.Ordinal)
                && node.Type.EndsWith(">", StringComparison.Ordinal)
                && node.Children[0] is { Name: "m_FileID", DataType: NodeDataType.Int32 }
                && node.Children[1] is { Name: "m_PathID", DataType: NodeDataType.Int64 };
        }

        public static NodeDataType MatchDataType(TypeTreeBranch node)
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
            else if (node.EvalIsPos4f())
            {
                return NodeDataType.Pos4f;
            }
            else if (node.EvalIsPos3f())
            {
                return NodeDataType.Pos3f;
            }
            else if (node.EvalIsPos2f())
            {
                return NodeDataType.Pos2f;
            }
            else if (node.EvalIsColorRGBA4f())
            {
                return NodeDataType.ColorRGBA4f;
            }
            else if (node.EvalIsColorRGBA32())
            {
                return NodeDataType.ColorRGBA32;
            }
            else if (node.EvalIsMatrix4x4f())
            {
                return NodeDataType.Matrix4x4f;
            }
            else if (node.EvalIsPPtr())
            {
                return NodeDataType.PPtr;
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
            return type < NodeDataType.ByteArray;
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
