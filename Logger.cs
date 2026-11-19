namespace AssetManager
{
    /// <summary>
    /// Logger hooks for the bundle loading layer.
    /// </summary>
    /// <remarks>
    /// The library never decides which levels are interesting: all five methods
    /// always forward to their delegate, and a null delegate means "silent".
    /// Filtering is the responsibility of whoever installs the delegates
    /// (for example a host that maps them to its own console logger).
    /// </remarks>
    public static class Logger
    {
        public delegate void LogEntry(string logHead, object message);

        /// <summary>Very detailed tracing (per block, per field).</summary>
        public static LogEntry Verbose;

        /// <summary>Diagnostic details useful while developing.</summary>
        public static LogEntry Debug;

        /// <summary>Normal progress information.</summary>
        public static LogEntry Info;

        /// <summary>Something unexpected that is still recoverable.</summary>
        public static LogEntry Warning;

        /// <summary>A failure.</summary>
        public static LogEntry Error;

        public static void LogV(string tag, object message)
        {
            Verbose?.Invoke(tag, message);
        }

        public static void LogD(string tag, object message)
        {
            Debug?.Invoke(tag, message);
        }

        public static void LogI(string tag, object message)
        {
            Info?.Invoke(tag, message);
        }

        public static void LogW(string tag, object message)
        {
            Warning?.Invoke(tag, message);
        }

        public static void LogE(string tag, object message)
        {
            Error?.Invoke(tag, message);
        }
    }
}
