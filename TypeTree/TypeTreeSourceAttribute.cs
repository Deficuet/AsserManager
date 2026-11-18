using System;

namespace AssetManager.TypeTree
{
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct,
        AllowMultiple = false,
        Inherited = false)]
    public class TypeTreeSourceAttribute : Attribute
    {
        public string UnityVersion { get; }
        public string AssemblyName { get; }
        public string Namespace { get; }
        public string ClassName { get; }

        public TypeTreeSourceAttribute(string unityVersion, string assemblyName, string @namespace, string className)
        {
            UnityVersion = unityVersion;
            AssemblyName = assemblyName;
            Namespace = @namespace;
            ClassName = className;
        }
    }
}
