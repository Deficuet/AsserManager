using System.IO;
using AssetsTools.NET.Extra;

namespace AssetManager.BundleFile
{
    /// <summary>
    /// Loads a bundle that the built-in <see cref="AssetsManager"/> cannot read
    /// (for example an encrypted bundle) and returns a <see cref="BundleFileInstance"/>
    /// that behaves exactly like one produced by <see cref="AssetsManager"/>.
    /// </summary>
    /// <remarks>
    /// Implementations live outside this assembly. Everything after the bundle is
    /// loaded (assets files, type tree blobs, dependency resolution) keeps using the
    /// regular AssetsTools.NET code path, so the returned instance must be complete:
    /// a valid <c>Header</c>, <c>BlockAndDirInfo</c>, <c>Reader</c> and an already
    /// decompressed <c>DataReader</c> with <c>DataIsCompressed = false</c>.
    /// </remarks>
    public interface IBundleLoadPipeline
    {
        /// <summary>
        /// Loads a bundle from a raw stream.
        /// </summary>
        /// <param name="source">
        /// Raw bundle stream, positioned anywhere. The returned instance takes
        /// ownership of it, exactly like <see cref="AssetsManager.LoadBundleFile(Stream, string, bool)"/>.
        /// </param>
        /// <param name="path">Path used by the manager for lookups.</param>
        /// <param name="context">The context that selected this pipeline. May be null.</param>
        /// <param name="key">
        /// Overrides the key configured on the pipeline when not null.
        /// </param>
        /// <returns>A fully loaded bundle instance.</returns>
        BundleFileInstance Load(Stream source, string path, BundleLoadContext context, string key = null);
    }
}
