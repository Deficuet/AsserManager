using AssetManager.TypeTree;
using AssetsTools.NET;
using System;
using System.Collections.Generic;

namespace AssetManager.Cpp2IL
{
    public enum Cpp2ILBaseKind
    {
        MonoBehaviour,
        ScriptableObject,
        UnityObject,
        Unsupported
    }

    public sealed class Cpp2ILEnumValue
    {
        public string Name { get; }

        public object Value { get; }

        public Cpp2ILEnumValue(string name, object value)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = value;
        }
    }

    public sealed class Cpp2ILEnumInfo
    {
        public string FullName { get; }

        public string Name { get; }

        public string Namespace { get; }

        public string UnderlyingCSharpType { get; }

        public string UnderlyingTypeTreeName { get; }

        public bool IsFlags { get; }

        public bool FlagsKnown { get; }

        public IReadOnlyList<Cpp2ILEnumValue> Values { get; }

        public Cpp2ILEnumInfo(
            string fullName,
            string name,
            string @namespace,
            string underlyingCSharpType,
            string underlyingTypeTreeName,
            bool isFlags,
            bool flagsKnown,
            IReadOnlyList<Cpp2ILEnumValue> values)
        {
            FullName = fullName ?? throw new ArgumentNullException(nameof(fullName));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Namespace = @namespace ?? string.Empty;
            UnderlyingCSharpType = underlyingCSharpType
                ?? throw new ArgumentNullException(nameof(underlyingCSharpType));
            UnderlyingTypeTreeName = underlyingTypeTreeName
                ?? throw new ArgumentNullException(nameof(underlyingTypeTreeName));
            IsFlags = isFlags;
            FlagsKnown = flagsKnown;
            Values = values ?? throw new ArgumentNullException(nameof(values));
        }
    }

    public sealed class Cpp2ILFieldInfo
    {
        public string Name { get; }

        public string TypeFullName { get; }

        public string ElementTypeFullName { get; }

        public bool IsArray { get; }

        public bool IsList { get; }

        public bool IsEnum { get; }

        public Cpp2ILEnumInfo EnumInfo { get; }

        public Cpp2ILFieldInfo(
            string name,
            string typeFullName,
            string elementTypeFullName,
            bool isArray,
            bool isList,
            bool isEnum,
            Cpp2ILEnumInfo enumInfo)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            TypeFullName = typeFullName ?? string.Empty;
            ElementTypeFullName = elementTypeFullName ?? string.Empty;
            IsArray = isArray;
            IsList = isList;
            IsEnum = isEnum;
            EnumInfo = enumInfo;
        }
    }

    public sealed class Cpp2ILTypeInfo
    {
        public string AssemblyName { get; }

        public string Namespace { get; }

        public string Name { get; }

        public string FullName { get; }

        public Cpp2ILTypeInfo BaseType { get; internal set; }

        public string BaseTypeFullName { get; }

        public Cpp2ILBaseKind BaseKind { get; }

        public IReadOnlyList<Cpp2ILFieldInfo> Fields { get; }

        public IReadOnlyList<AssetTypeTemplateField> OwnTemplateFields { get; }

        public AssetTypeTemplateField FullTemplateField { get; }

        public int FirstMemberOrder { get; }

        public TypeTreeResult TypeTree { get; }

        public bool IsEnum { get; }

        public Cpp2ILEnumInfo EnumInfo { get; }

        public Cpp2ILTypeInfo(
            string assemblyName,
            string @namespace,
            string name,
            string fullName,
            string baseTypeFullName,
            Cpp2ILBaseKind baseKind,
            IReadOnlyList<Cpp2ILFieldInfo> fields,
            IReadOnlyList<AssetTypeTemplateField> ownTemplateFields,
            AssetTypeTemplateField fullTemplateField,
            int firstMemberOrder,
            TypeTreeResult typeTree,
            bool isEnum,
            Cpp2ILEnumInfo enumInfo)
        {
            AssemblyName = assemblyName ?? throw new ArgumentNullException(nameof(assemblyName));
            Namespace = @namespace ?? string.Empty;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            FullName = fullName ?? throw new ArgumentNullException(nameof(fullName));
            BaseTypeFullName = baseTypeFullName ?? string.Empty;
            BaseKind = baseKind;
            Fields = fields ?? throw new ArgumentNullException(nameof(fields));
            OwnTemplateFields = ownTemplateFields
                ?? throw new ArgumentNullException(nameof(ownTemplateFields));
            FullTemplateField = fullTemplateField
                ?? throw new ArgumentNullException(nameof(fullTemplateField));
            FirstMemberOrder = firstMemberOrder;
            TypeTree = typeTree ?? throw new ArgumentNullException(nameof(typeTree));
            IsEnum = isEnum;
            EnumInfo = enumInfo;
        }
    }

    public sealed class Cpp2ILTypeChain
    {
        public Cpp2ILBaseKind RootKind { get; }

        public string RootBaseTypeFullName { get; }

        public IReadOnlyList<Cpp2ILTypeInfo> Types { get; }

        public Cpp2ILTypeChain(
            Cpp2ILBaseKind rootKind,
            string rootBaseTypeFullName,
            IReadOnlyList<Cpp2ILTypeInfo> types)
        {
            RootKind = rootKind;
            RootBaseTypeFullName = rootBaseTypeFullName ?? string.Empty;
            Types = types ?? throw new ArgumentNullException(nameof(types));
        }
    }
}
