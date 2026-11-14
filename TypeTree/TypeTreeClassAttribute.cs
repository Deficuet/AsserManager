using System;

namespace AssetManager.TypeTree
{
    [AttributeUsage(
        AttributeTargets.Class
            | AttributeTargets.Struct
            | AttributeTargets.Field
            | AttributeTargets.Property,
        AllowMultiple = false,
        Inherited = false)]
    public class TypeTreeClassAttribute : Attribute
    {
        public string TypeName { get; }

        public TypeTreeClassAttribute(string typeName)
        {
            TypeName = typeName;
        }
    }
}
