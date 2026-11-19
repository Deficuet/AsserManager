using System;
using System.IO;
using AssetsTools.NET.Extra;
using AssetManager.BundleFile;

namespace AssetManager
{
    /// <summary>
    /// Thin wrapper over <see cref="AssetsManager"/> that allows a bundle to be loaded
    /// by a custom <see cref="IBundleLoadPipeline"/> instead of the built-in loader.
    /// </summary>
    /// <remarks>
    /// When no pipeline is configured the call is forwarded to <see cref="AssetsManager"/>
    /// untouched. When a pipeline is configured it produces the <see cref="BundleFileInstance"/>
    /// and the instance is registered with the manager, so every downstream feature
    /// (assets files, type tree blobs, dependency resolution, unloading) keeps working.
    /// </remarks>
    public class UnityAssetManagerExtension
    {
        /// <summary>
        /// The wrapped AssetsTools.NET manager.
        /// </summary>
        public AssetsManager Manager { get; }

        /// <summary>
        /// Context used when a load call does not provide its own.
        /// </summary>
        public BundleLoadContext Context { get; }

        public UnityAssetManagerExtension()
            : this(null, null)
        {
        }

        public UnityAssetManagerExtension(BundleLoadContext context)
            : this(null, context)
        {
        }

        public UnityAssetManagerExtension(AssetsManager manager, BundleLoadContext context = null)
        {
            Manager = manager ?? new AssetsManager();
            Context = context;
        }

        /// <summary>
        /// Loads a bundle from a file.
        /// </summary>
        /// <param name="path">Path of the bundle.</param>
        /// <param name="unpackIfPacked">
        /// Forwarded to <see cref="AssetsManager"/> for the plain path. A pipeline
        /// always returns already decompressed data, so the value is ignored there.
        /// </param>
        /// <param name="context">
        /// Per-call context. Replaces <see cref="Context"/> entirely when not null.
        /// Pass <c>new BundleLoadContext()</c> to force the plain path for one call.
        /// </param>
        /// <param name="key">Overrides the key stored on the pipeline when not null.</param>
        public BundleFileInstance LoadBundleFile(string path, bool unpackIfPacked = true,
            BundleLoadContext context = null, string key = null)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            BundleLoadContext effectiveContext = context ?? Context;
            if (effectiveContext == null || effectiveContext.Pipeline == null)
                return Manager.LoadBundleFile(path, unpackIfPacked);

            string lookupKey = AssetsManager.GetBundleLookupKey(path);
            if (Manager.BundleLookup.TryGetValue(lookupKey, out BundleFileInstance existing))
                return existing;

            Stream stream = File.OpenRead(path);
            return LoadWithPipeline(stream, path, effectiveContext, key);
        }

        /// <summary>
        /// Loads a bundle from a stream.
        /// </summary>
        /// <param name="stream">
        /// Raw bundle stream. When the pipeline path is taken the returned instance owns
        /// the stream; when the plain path is taken AssetsTools.NET owns it.
        /// </param>
        /// <param name="path">Path used by the manager for lookups.</param>
        /// <param name="unpackIfPacked">
        /// Forwarded to <see cref="AssetsManager"/> for the plain path.
        /// </param>
        /// <param name="context">
        /// Per-call context. Replaces <see cref="Context"/> entirely when not null.
        /// </param>
        /// <param name="key">Overrides the key stored on the pipeline when not null.</param>
        public BundleFileInstance LoadBundleFile(Stream stream, string path, bool unpackIfPacked = true,
            BundleLoadContext context = null, string key = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            BundleLoadContext effectiveContext = context ?? Context;
            if (effectiveContext == null || effectiveContext.Pipeline == null)
                return Manager.LoadBundleFile(stream, path, unpackIfPacked);

            string lookupKey = AssetsManager.GetBundleLookupKey(path);
            if (Manager.BundleLookup.TryGetValue(lookupKey, out BundleFileInstance existing))
                return existing;

            return LoadWithPipeline(stream, path, effectiveContext, key);
        }

        private BundleFileInstance LoadWithPipeline(Stream stream, string path,
            BundleLoadContext context, string key)
        {
            BundleFileInstance bunInst;
            try
            {
                string effectiveKey = key ?? context.Key;
                bunInst = context.Pipeline.Load(stream, path, context, effectiveKey);
            }
            catch
            {
                stream.Dispose();
                throw;
            }

            if (bunInst == null)
            {
                stream.Dispose();
                throw new InvalidOperationException(
                    "The bundle pipeline returned no instance for '" + path + "'.");
            }

            BundleFileInstance registered = Manager.RegisterBundleFile(bunInst, path);
            if (!ReferenceEquals(registered, bunInst))
            {
                // Another instance for this path won the race; release the one we built.
                bunInst.file.Close();
            }

            return registered;
        }
    }
}
