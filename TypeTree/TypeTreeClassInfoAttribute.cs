using System;

namespace AssetManager.TypeTree
{
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct,
        AllowMultiple = false,
        Inherited = false)]
    public class TypeTreeClassInfoAttribute : Attribute
    {
        public string TypeName { get; }

        public TypeTreeClassInfoAttribute(string typeName)
        {
            TypeName = typeName;
        }
    }
}
