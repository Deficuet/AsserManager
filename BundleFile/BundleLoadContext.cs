namespace AssetManager.BundleFile
{
    /// <summary>
    /// Options for loading bundles through <see cref="UnityAssetManagerExtension"/>.
    /// A context can be supplied when the extension is created and again per load call;
    /// the per-call context wins and is used as a whole (it is not merged field by field).
    /// </summary>
    public sealed class BundleLoadContext
    {
        /// <summary>
        /// The pipeline used to load bundles. When null the call is forwarded to
        /// AssetsTools.NET untouched and the regular bundle loader is used.
        /// </summary>
        public IBundleLoadPipeline Pipeline { get; set; }

        /// <summary>
        /// Unity version used when the bundle itself stores a stripped version
        /// (for example "0.0.0"). The raw value is kept in
        /// <see cref="AssetsTools.NET.AssetBundleHeader.RawEngineVersion"/>.
        /// </summary>
        public string DefaultUnityVersion { get; set; }

        /// <summary>
        /// Optional key forwarded to <see cref="IBundleLoadPipeline.Load"/>.
        /// A key passed directly to the load call takes precedence over this one.
        /// </summary>
        public string Key { get; set; }
    }
}
