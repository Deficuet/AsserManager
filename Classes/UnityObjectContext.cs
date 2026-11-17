using AssetsTools.NET;

namespace AssetManager.Classes
{
    public class UnityObjectContext<T> where T : IUnityObject
    {
        public AssetsFile SerializedFile { get; private set; }
        public AssetFileInfo ObjectInfo { get; private set; }
        public T Data { get; private set; }

        public UnityObjectContext(AssetsFile serializedFile, AssetFileInfo objectInfo, T data)
        {
            SerializedFile = serializedFile;
            ObjectInfo = objectInfo;
            Data = data;
        }
    }
}
