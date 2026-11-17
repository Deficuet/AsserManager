using System;

namespace AssetManager.Deserialize
{
    public class DeserializeException : Exception
    {
        public DeserializeException(string message)
            : base(message)
        {
        }

        public DeserializeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
