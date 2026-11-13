using System;

namespace AssetManager.TypeTree
{
    [AttributeUsage(
        AttributeTargets.Field | AttributeTargets.Property,
        AllowMultiple = false,
        Inherited = false)]
    public class TypeTreeMemberInfoAttribute : Attribute
    {
        public string TypeName { get; }
        public string Name { get; }

        public TypeTreeMemberInfoAttribute(string typeName, string name)
        {
            TypeName = typeName;
            Name = name;
        }
    }
}
