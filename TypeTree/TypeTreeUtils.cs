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
        public static TypeTreeResult GetBuiltinTypeTree(
            AssetsManager manager,
            UnityVersion unityVersion,
            AssetClassID classId,
            bool preferEditor = false)
        {
            // Extract the ClassDatabase for the requested Unity version.
            ClassDatabaseFile classDb =
                manager.LoadClassDatabaseFromPackage(unityVersion);

            // Convert ClassDatabase directly to TypeTreeType.
            TypeTreeType typeTree =
                ClassDatabaseToTypeTree.Convert(
                    classDb,
                    (int)classId,
                    preferEditor
                ) ?? throw new InvalidOperationException(
                    $"ClassID {classId} was not found in TPK " +
                    $"for Unity {unityVersion}."
                );

            // Build the exact Common String Table for this TPK version.
            byte[] commonStrings = BuildCommonStringBuffer(classDb);

            return new TypeTreeResult(typeTree, commonStrings);
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

        public static TypeTreeResult GetCustomMonoBehaviourTypeTree(
            AssetsManager manager,
            UnityVersion unityVersion,
            string globalMetadataPath,
            string gameAssemblyPath,
            string assemblyName,
            string namespaceName,
            string className,
            ushort scriptIndex = 0xffff)
        {
            // --------------------------------------------------------
            // 1. Load Unity's native MonoBehaviour template from TPK.
            // --------------------------------------------------------

            ClassDatabaseFile classDb =
                manager.LoadClassDatabaseFromPackage(unityVersion);

            ClassDatabaseType monoBehaviourType =
                classDb.FindAssetClassByID(
                    (int)AssetClassID.MonoBehaviour
                ) ?? throw new InvalidOperationException(
                    "MonoBehaviour was not found in TPK."
                );
            var baseField = new AssetTypeTemplateField();

            baseField.FromClassDatabase(
                classDb,
                monoBehaviourType,
                preferEditor: false
            );

            // --------------------------------------------------------
            // 2. Reconstruct the custom MonoBehaviour fields using IL2CPP.
            // --------------------------------------------------------

            var generator = new Cpp2IlTempGenerator(
                globalMetadataPath,
                gameAssemblyPath
            );

            AssetTypeTemplateField generatedTemplate;

            try
            {
                generatedTemplate = generator.GetTemplateField(
                    baseField,
                    assemblyName,
                    namespaceName,
                    className,
                    unityVersion
                );
            }
            finally
            {
                generator.Dispose();
            }

            // --------------------------------------------------------
            // 3. Convert the generated template to TypeTreeType.
            // --------------------------------------------------------

            TypeTreeType typeTree =
                TemplateFieldToTypeTree.Convert(
                    generatedTemplate,
                    (int)AssetClassID.MonoBehaviour,
                    scriptIndex,
                    commonStrings: null
                );

            return new TypeTreeResult(typeTree, Array.Empty<byte>());
        }
    }
}
