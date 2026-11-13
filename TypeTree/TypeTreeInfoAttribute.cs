using System;

namespace AssetManager.TypeTree
{
    [AttributeUsage(
        AttributeTargets.Field | AttributeTargets.Property,
        AllowMultiple = false,
        Inherited = false)]
    public class TypeTreeInfoAttribute : Attribute
    {
        public string TypeName { get; }
        public string Name { get; }

        public TypeTreeInfoAttribute(string typeName, string name)
        {
            TypeName = typeName;
            Name = name;
        }
    }
}
