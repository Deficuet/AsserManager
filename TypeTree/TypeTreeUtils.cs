using AssetsTools.NET;
using AssetsTools.NET.Cpp2IL;
using AssetsTools.NET.Extra;
using System;
using System.IO;
using System.Text;

namespace AssetManager.TypeTree
{
    public static class TypeTreeUtils
    {
        public static bool TryGetBuiltinTypeTree(
            ClassDatabaseFile classDb,
            AssetClassID classId,
            out TypeTreeResult result,
            bool preferEditor = false)
        {
            result = default!;
            // Convert ClassDatabase directly to TypeTreeType.
            TypeTreeType typeTree =
                ClassDatabaseToTypeTree.Convert(
                    classDb,
                    (int)classId,
                    preferEditor
                );
            if (typeTree is null)
            {
                return false;
            }

            // Build the exact Common String Table for this TPK version.
            byte[] commonStrings = BuildCommonStringBuffer(classDb);
            result = new TypeTreeResult(typeTree, commonStrings);
            return true;
        }

        private static byte[] BuildCommonStringBuffer(ClassDatabaseFile classDb)
        {
            using var stream = new MemoryStream();

            foreach (ushort index in classDb.CommonStringBufferIndices)
            {
                string value = classDb.StringTable.GetString(index);

                if (value.Length == 0)
                    continue;

                byte[] bytes = Encoding.UTF8.GetBytes(value);

                // ClassDatabaseToTypeTree currently computes common-string
                // offsets using string.Length, so its common strings are
                // expected to be ASCII.
                if (bytes.Length != value.Length)
                {
                    throw new NotSupportedException(
                        $"Non-ASCII common string: {value}"
                    );
                }

                stream.Write(bytes, 0, bytes.Length);
                stream.WriteByte(0);
            }

            return stream.ToArray();
        }

        public static bool TryGetCustomMonoBehaviourTypeTree(
            AssetTypeTemplateField monoBehaviourBaseField,
            UnityVersion unityVersion,
            Cpp2IlTempGenerator generator,
            CustomClassSource source,
            out TypeTreeResult result,
            ushort scriptIndex = 0xffff)
        {
            result = default!;

            AssetTypeTemplateField generatedTemplate;

            try
            {
                generatedTemplate = generator.GetTemplateField(
                    monoBehaviourBaseField,
                    source.AssemblyName,
                    source.Namespace,
                    source.ClassName,
                    unityVersion
                );
            }
            catch
            {
                return false;
            }

            TypeTreeType typeTree =
                TemplateFieldToTypeTree.Convert(
                    generatedTemplate,
                    (int)AssetClassID.MonoBehaviour,
                    scriptIndex,
                    commonStrings: null
                );

            // Passing null makes AssetsTools.NET fall back to its built-in
            // common string table when resolving node strings.
            result = new TypeTreeResult(typeTree);
            return true;
        }
    }
}
