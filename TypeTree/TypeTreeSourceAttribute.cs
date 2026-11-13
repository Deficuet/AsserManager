using System;

namespace AssetManager.TypeTree
{
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct,
        AllowMultiple = false,
        Inherited = false)]
    public class TypeTreeSourceAttribute : Attribute
    {
        public string AssemblyName { get; }
        public string Namespace { get; }
        public string ClassName { get; }

        public TypeTreeSourceAttribute(string assemblyName, string @namespace, string className)
        {
            AssemblyName = assemblyName;
            Namespace = @namespace;
            ClassName = className;
        }
    }
}
