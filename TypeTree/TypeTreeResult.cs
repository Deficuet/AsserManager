using AssetsTools.NET;
using System;
using System.Collections.Generic;

namespace AssetManager.TypeTree
{
    public sealed class TypeTreeResult
    {
        public TypeTreeType Definition { get; }

        public byte[] CommonStringBufferBytes { get; }

        public TypeTreeResult(
            TypeTreeType definition,
            byte[] commonStringBufferBytes = null
        )
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            CommonStringBufferBytes = commonStringBufferBytes ?? Array.Empty<byte>();
        }

        public List<TypeTreeNode> Nodes => Definition.Nodes;

        public byte[] StringBufferBytes => Definition.StringBufferBytes;

        public string GetNameString(TypeTreeNode node)
        {
            return node.GetNameString(
                StringBufferBytes,
                CommonStringBufferBytes
            );
        }

        public string GetTypeString(TypeTreeNode node)
        {
            return node.GetTypeString(
                StringBufferBytes,
                CommonStringBufferBytes
            );
        }
    }
}
