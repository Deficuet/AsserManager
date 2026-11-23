using AssetManager.TypeTree;
using AssetsTools.NET;
using AssetsTools.NET.Cpp2IL;
using Cpp2IL.Core;
using Cpp2IL.Core.Api;
using Cpp2IL.Core.InstructionSets;
using Cpp2IL.Core.Model.Contexts;
using LibCpp2IL;
using System;
using System.Collections.Generic;
using System.Reflection;
using ARUnityVersion = AssetRipper.Primitives.UnityVersion;
using ATUnityVersion = AssetsTools.NET.Extra.UnityVersion;

namespace AssetManager.Cpp2IL
{
    /// <summary>
    /// Owns one LibCpp2IL runtime and exposes the metadata needed by the
    /// generator. Cpp2IL.Core and AssetsTools.NET.Cpp2IL share the same
    /// static LibCpp2IL state, so only one manager may be active.
    /// </summary>
    public sealed class Cpp2ILManager : IDisposable
    {
        private static readonly Dictionary<string, (string CSharp, string TypeTree)> EnumUnderlyingTypes =
            new Dictionary<string, (string CSharp, string TypeTree)>(StringComparer.Ordinal)
            {
                ["System.SByte"] = ("sbyte", "SInt8"),
                ["System.Byte"] = ("byte", "UInt8"),
                ["System.Int16"] = ("short", "SInt16"),
                ["System.UInt16"] = ("ushort", "UInt16"),
                ["System.Int32"] = ("int", "int"),
                ["System.UInt32"] = ("uint", "unsigned int"),
                ["System.Int64"] = ("long", "SInt64"),
                ["System.UInt64"] = ("ulong", "UInt64")
            };

        private static Cpp2ILManager _active;
        private static bool _instructionSetsRegistered;

        private readonly ApplicationAnalysisContext _application;
        private readonly Cpp2IlTempGenerator _templateGenerator;
        private readonly ATUnityVersion _unityVersion;
        private readonly Dictionary<string, Cpp2ILTypeInfo> _typeInfoCache;
        private readonly Dictionary<string, Cpp2ILEnumInfo> _enumCache;
        private bool _disposed;

        public Cpp2ILManager(
            string gameAssemblyPath,
            string metadataPath,
            string unityVersion)
        {
            if (_active != null)
            {
                throw new InvalidOperationException(
                    "Another Cpp2ILManager is already active. " +
                    "LibCpp2IL has process-wide state."
                );
            }

            _active = this;
            _typeInfoCache = new Dictionary<string, Cpp2ILTypeInfo>(StringComparer.Ordinal);
            _enumCache = new Dictionary<string, Cpp2ILEnumInfo>(StringComparer.Ordinal);

            try
            {
                RegisterInstructionSets();
                var arUnityVersion = ARUnityVersion.Parse(unityVersion);
                Cpp2IlApi.InitializeLibCpp2Il(
                    gameAssemblyPath,
                    metadataPath,
                    arUnityVersion
                );
                _application = Cpp2IlApi.CurrentAppContext
                    ?? throw new InvalidOperationException(
                        "Cpp2IL initialization did not create an application context."
                    );
                _unityVersion = new ATUnityVersion(unityVersion);
                _templateGenerator = new Cpp2IlTempGenerator(
                    metadataPath,
                    gameAssemblyPath,
                    ownsLibCpp2IlState: false
                );
                _templateGenerator.AttachToInitializedRuntime(_unityVersion);
            }
            catch
            {
                if (_active == this)
                {
                    _active = null;
                }
                Cpp2IlApi.ResetInternalState();
                throw;
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _typeInfoCache.Clear();
            _enumCache.Clear();
            _templateGenerator?.Dispose();
            Cpp2IlApi.ResetInternalState();

            if (_active == this)
            {
                _active = null;
            }
        }

        public bool TryGetTypeInfo(
            string assemblyName,
            string @namespace,
            string className,
            out Cpp2ILTypeInfo typeInfo)
        {
            typeInfo = null;
            if (!TryResolveType(assemblyName, @namespace, className, out TypeAnalysisContext type))
            {
                return false;
            }

            typeInfo = BuildStandaloneTypeInfo(type);
            return true;
        }

        public bool TryGetTypeInfoByFullName(
            string assemblyName,
            string fullName,
            out Cpp2ILTypeInfo typeInfo)
        {
            typeInfo = null;
            string normalizedAssemblyName = NormalizeAssemblyName(assemblyName);
            if (!_application.AssembliesByName.TryGetValue(
                normalizedAssemblyName,
                out AssemblyAnalysisContext assembly))
            {
                return false;
            }

            TypeAnalysisContext type = assembly.GetTypeByFullName(fullName);
            if (type == null)
            {
                return false;
            }

            typeInfo = BuildStandaloneTypeInfo(type);
            return true;
        }

        public bool TryBuildCustomTypeChain(
            AssetTypeTemplateField monoBehaviourBaseField,
            string assemblyName,
            string @namespace,
            string className,
            out Cpp2ILTypeChain chain)
        {
            chain = null;
            if (!TryResolveType(assemblyName, @namespace, className, out TypeAnalysisContext target))
            {
                return false;
            }

            var types = new List<TypeAnalysisContext>();
            TypeAnalysisContext current = target;
            Cpp2ILBaseKind rootKind = Cpp2ILBaseKind.Unsupported;
            string rootBaseTypeFullName = string.Empty;

            while (true)
            {
                TypeAnalysisContext baseType = current.BaseType;
                if (baseType == null)
                {
                    return false;
                }

                string baseFullName = baseType.FullName;
                if (baseFullName == "UnityEngine.MonoBehaviour")
                {
                    rootKind = Cpp2ILBaseKind.MonoBehaviour;
                    rootBaseTypeFullName = baseFullName;
                    types.Add(current);
                    break;
                }
                if (baseFullName == "UnityEngine.ScriptableObject")
                {
                    rootKind = Cpp2ILBaseKind.ScriptableObject;
                    rootBaseTypeFullName = baseFullName;
                    types.Add(current);
                    break;
                }
                if (baseFullName == "UnityEngine.Object")
                {
                    rootKind = Cpp2ILBaseKind.UnityObject;
                    rootBaseTypeFullName = baseFullName;
                    types.Add(current);
                    break;
                }
                if (baseFullName == "System.Object")
                {
                    return false;
                }

                types.Add(current);
                current = baseType;
            }

            types.Reverse();

            if ((rootKind == Cpp2ILBaseKind.MonoBehaviour
                    || rootKind == Cpp2ILBaseKind.ScriptableObject)
                && monoBehaviourBaseField == null)
            {
                return false;
            }

            var builtTypes = new List<Cpp2ILTypeInfo>(types.Count);
            Cpp2ILTypeInfo parentInfo = null;
            foreach (TypeAnalysisContext type in types)
            {
                Cpp2ILTypeInfo info = GetOrBuildChainTypeInfo(
                    type,
                    rootKind,
                    monoBehaviourBaseField,
                    parentInfo
                );
                info.BaseType = parentInfo;
                builtTypes.Add(info);
                parentInfo = info;
            }

            chain = new Cpp2ILTypeChain(
                rootKind,
                rootBaseTypeFullName,
                builtTypes
            );
            return true;
        }

        public bool TryGetEnumInfo(
            string assemblyName,
            string @namespace,
            string className,
            out Cpp2ILEnumInfo enumInfo)
        {
            enumInfo = null;
            if (!TryResolveType(assemblyName, @namespace, className, out TypeAnalysisContext type))
            {
                return false;
            }

            enumInfo = BuildEnumInfo(type);
            return enumInfo != null;
        }

        private Cpp2ILTypeInfo BuildStandaloneTypeInfo(TypeAnalysisContext type)
        {
            string key = GetCacheKey(type, Cpp2ILBaseKind.UnityObject);
            if (_typeInfoCache.TryGetValue(key, out Cpp2ILTypeInfo cached))
            {
                return cached;
            }

            var rootField = new AssetTypeTemplateField
            {
                Name = "Base",
                Type = "Object",
                Children = new List<AssetTypeTemplateField>()
            };
            Cpp2ILTypeInfo info = BuildTypeInfo(
                type,
                Cpp2ILBaseKind.UnityObject,
                rootField,
                0,
                null
            );
            _typeInfoCache[key] = info;
            return info;
        }

        private Cpp2ILTypeInfo GetOrBuildChainTypeInfo(
            TypeAnalysisContext type,
            Cpp2ILBaseKind rootKind,
            AssetTypeTemplateField monoBehaviourBaseField,
            Cpp2ILTypeInfo parentInfo)
        {
            string key = GetCacheKey(type, rootKind);
            if (_typeInfoCache.TryGetValue(key, out Cpp2ILTypeInfo cached))
            {
                return cached;
            }

            AssetTypeTemplateField baseField;
            int firstMemberOrder;
            if (parentInfo != null)
            {
                baseField = parentInfo.FullTemplateField.Clone();
                firstMemberOrder = parentInfo.FullTemplateField.Children.Count;
            }
            else if (rootKind == Cpp2ILBaseKind.MonoBehaviour
                || rootKind == Cpp2ILBaseKind.ScriptableObject)
            {
                baseField = monoBehaviourBaseField.Clone();
                firstMemberOrder = monoBehaviourBaseField.Children.Count;
            }
            else
            {
                baseField = new AssetTypeTemplateField
                {
                    Name = "Base",
                    Type = "Object",
                    Children = new List<AssetTypeTemplateField>()
                };
                firstMemberOrder = 0;
            }

            Cpp2ILTypeInfo info = BuildTypeInfo(
                type,
                rootKind,
                baseField,
                firstMemberOrder,
                parentInfo
            );
            _typeInfoCache[key] = info;
            return info;
        }

        private Cpp2ILTypeInfo BuildTypeInfo(
            TypeAnalysisContext type,
            Cpp2ILBaseKind rootKind,
            AssetTypeTemplateField baseField,
            int firstMemberOrder,
            Cpp2ILTypeInfo parentInfo)
        {
            List<AssetTypeTemplateField> ownFields =
                _templateGenerator.GetTemplateFieldsForType(
                    type.DeclaringAssembly.Name,
                    type.Namespace,
                    type.Name,
                    _unityVersion
                );

            AssetTypeTemplateField fullTemplate = baseField.Clone();
            fullTemplate.Children.AddRange(ownFields);

            if (!TypeTreeUtils.TryConvertCustomTypeTree(fullTemplate, out TypeTreeResult typeTree))
            {
                throw new InvalidOperationException(
                    $"Failed to convert custom type tree for '{type.FullName}'."
                );
            }

            Cpp2ILEnumInfo enumInfo = BuildEnumInfo(type);
            return new Cpp2ILTypeInfo(
                type.DeclaringAssembly.Name,
                type.Namespace,
                type.Name,
                type.FullName,
                type.BaseType?.FullName ?? string.Empty,
                rootKind,
                BuildFieldInfos(type),
                ownFields,
                fullTemplate,
                firstMemberOrder,
                typeTree,
                enumInfo != null,
                enumInfo
            );
        }

        private List<Cpp2ILFieldInfo> BuildFieldInfos(TypeAnalysisContext type)
        {
            var fields = new List<Cpp2ILFieldInfo>();
            foreach (FieldAnalysisContext field in type.Fields)
            {
                TypeAnalysisContext fieldType;
                try
                {
                    fieldType = field.FieldType;
                }
                catch
                {
                    continue;
                }

                TypeAnalysisContext elementType = UnwrapCollectionElement(fieldType, out bool isArray, out bool isList);
                TypeAnalysisContext enumType = elementType ?? fieldType;
                Cpp2ILEnumInfo enumInfo = BuildEnumInfo(enumType);
                fields.Add(new Cpp2ILFieldInfo(
                    field.Name,
                    fieldType.FullName,
                    elementType?.FullName ?? string.Empty,
                    isArray,
                    isList,
                    enumInfo != null,
                    enumInfo
                ));
            }
            return fields;
        }

        private static TypeAnalysisContext UnwrapCollectionElement(
            TypeAnalysisContext type,
            out bool isArray,
            out bool isList)
        {
            isArray = false;
            isList = false;

            if (type is ArrayTypeAnalysisContext || type is SzArrayTypeAnalysisContext)
            {
                isArray = true;
                return ((WrappedTypeAnalysisContext)type).ElementType;
            }

            if (type is GenericInstanceTypeAnalysisContext generic
                && generic.GenericType.FullName == "System.Collections.Generic.List`1"
                && generic.GenericArguments.Count == 1)
            {
                isList = true;
                return generic.GenericArguments[0];
            }

            return null;
        }

        private Cpp2ILEnumInfo BuildEnumInfo(TypeAnalysisContext type)
        {
            if (type == null || !type.IsEnumType)
            {
                return null;
            }

            if (_enumCache.TryGetValue(type.FullName, out Cpp2ILEnumInfo cached))
            {
                return cached;
            }

            string underlyingFullName = type.EnumUnderlyingType?.FullName ?? string.Empty;
            if (!EnumUnderlyingTypes.TryGetValue(
                underlyingFullName,
                out (string CSharp, string TypeTree) underlying))
            {
                return null;
            }

            bool isFlags = false;
            bool flagsKnown = false;
            try
            {
                if (_application.MetadataVersion >= 29)
                {
                    type.AnalyzeCustomAttributeData();
                }
                isFlags = type.HasCustomAttributeWithFullName(
                    "System.FlagsAttribute"
                );
                flagsKnown = true;
            }
            catch
            {
                flagsKnown = false;
            }

            var values = new List<Cpp2ILEnumValue>();
            foreach (FieldAnalysisContext field in type.Fields)
            {
                if (!field.IsStatic
                    || (field.Attributes & FieldAttributes.Literal) == 0)
                {
                    continue;
                }

                values.Add(new Cpp2ILEnumValue(
                    field.Name,
                    field.ConstantValue
                ));
            }

            var info = new Cpp2ILEnumInfo(
                type.FullName,
                type.Name,
                type.Namespace,
                underlying.CSharp,
                underlying.TypeTree,
                isFlags,
                flagsKnown,
                values
            );
            _enumCache[type.FullName] = info;
            return info;
        }

        private bool TryResolveType(
            string assemblyName,
            string @namespace,
            string className,
            out TypeAnalysisContext type)
        {
            type = null;
            string normalizedAssemblyName = NormalizeAssemblyName(assemblyName);
            if (!_application.AssembliesByName.TryGetValue(
                normalizedAssemblyName,
                out AssemblyAnalysisContext assembly))
            {
                return false;
            }

            string fullName = string.IsNullOrEmpty(@namespace)
                ? className
                : @namespace + "." + className;
            type = assembly.GetTypeByFullName(fullName);
            return type != null;
        }

        private static void RegisterInstructionSets()
        {
            if (_instructionSetsRegistered)
            {
                return;
            }

            InstructionSetRegistry.RegisterInstructionSet<X86InstructionSet>(
                DefaultInstructionSets.X86_64
            );
            InstructionSetRegistry.RegisterInstructionSet<X86InstructionSet>(
                DefaultInstructionSets.X86_32
            );
            InstructionSetRegistry.RegisterInstructionSet<ArmV7InstructionSet>(
                DefaultInstructionSets.ARM_V7
            );
            InstructionSetRegistry.RegisterInstructionSet<Arm64InstructionSet>(
                DefaultInstructionSets.ARM_V8
            );
            InstructionSetRegistry.RegisterInstructionSet<WasmInstructionSet>(
                DefaultInstructionSets.WASM
            );
            _instructionSetsRegistered = true;
        }

        private static string NormalizeAssemblyName(string assemblyName)
        {
            if (assemblyName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                return assemblyName.Substring(0, assemblyName.Length - 4);
            }
            if (assemblyName.EndsWith(".winmd", StringComparison.OrdinalIgnoreCase))
            {
                return assemblyName.Substring(0, assemblyName.Length - 6);
            }
            return assemblyName;
        }

        private static string GetCacheKey(
            TypeAnalysisContext type,
            Cpp2ILBaseKind rootKind)
        {
            return type.DeclaringAssembly.Name
                + "\0"
                + type.FullName
                + "\0"
                + rootKind;
        }
    }
}
