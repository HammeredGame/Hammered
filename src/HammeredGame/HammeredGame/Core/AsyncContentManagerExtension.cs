using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HammeredGame.Core
{
    /// <summary>
    /// <para>
    /// AsyncContentManagerExtension is a static class to extend the MonoGame ContentManager to
    /// support a <see cref="LoadAsync{T}(ContentManager, string)"/> method for loading content
    /// asynchronously from different threads without being bottlenecked by the main UI thread
    /// (often thread id 1).
    /// </para>
    /// <para>
    /// MonoGame's content readers need access to the OpenGL context on the main thread. Calling
    /// Load from within Update or anywhere on the main thread synchronously is very quick because
    /// it can immediately access this context. But if you begin to have large assets, it becomes
    /// problematic to freeze the main game loop while you load assets, so you'll need to eventually
    /// load assets asynchronously. Unfortunately, if you use the Load function in another thread,
    /// you will notice that it is around 10x slower. This is because the implementation of Load is
    /// such that each asset load (or maybe just textures, but most of them) goes back to the main
    /// thread and blocks the asynchronous thread while it tries to access the OpenGL context, which
    /// is only given once per game loop. This means that only one asset load gets processed per
    /// frame, and Load is bottlenecked by the game loop, which wasn't the case when we did it synchronously.
    /// </para>
    /// <para>
    /// The solution is to use this class, which provides a LoadAsync method to be called from any
    /// asynchronous thread and will offer the similar performance to calling Load synchronously
    /// from the main thread, without blocking the main thread (for the most part).
    /// </para>
    /// <para>
    /// This class is implemented by essentially replicating ContentManager's Load implementation
    /// line by line (and using reflection to access any private fields or methods), but instead of
    /// synchronously initializing assets, we asynchronously load file contents into memory and then
    /// add them to an queue that the UI thread will pop on every Update() to process synchronously,
    /// which we can wait for asynchronously.
    /// </para>
    /// </summary>
    /// <example>
    /// Usage: On every game Update(), call AsyncContentManagerExtension.Update() to process the
    /// queue. Whenever you want to asynchronously load assets, use await ContentManager.LoadAsync().
    /// </example>
    internal static class AsyncContentManagerExtension
    {
        /// <summary>
        /// The queue of yet-to-be-executed functions that contain initialization code (and the
        /// asset bytes in scope) for the asset. These functions should be invoked from the main
        /// thread since it needs access to the OpenGL context.
        /// </summary>
        private static readonly Queue<Action> queuedInitializations = new();

        /// <summary>
        /// Process any assets that have been loaded from disk as part of a <see
        /// cref="LoadAsync{T}(ContentManager, string)"/> but are not yet initialized.
        /// </summary>
        public static void Update()
        {
            while (queuedInitializations.Count > 0)
            {
                queuedInitializations.Dequeue().Invoke();
            }
        }

        /// <summary>
        /// Extension method on <see cref="ContentManager"/> to get a private field. Will not work
        /// with public fields. May return null.
        /// </summary>
        /// <typeparam name="T">The type of the field</typeparam>
        /// <param name="contentManager">The content manager instance</param>
        /// <param name="fieldName">The name of the private field</param>
        /// <returns></returns>
        public static T GetPrivate<T>(this ContentManager contentManager, string fieldName)
        {
            var field = contentManager.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (T)field?.GetValue(contentManager);
        }

        /// <summary>
        /// Extension method on <see cref="ContentManager"/> to call a private method. Will not work
        /// with public methods or generic methods. There is no static type checks for the arguments
        /// you pass, so make sure it matches what you need to call.
        /// </summary>
        /// <typeparam name="T">The return type of the private method</typeparam>
        /// <param name="contentManager">The content manager instance</param>
        /// <param name="methodName">The name of the private method</param>
        /// <param name="args">Any arguments, either as an array or variadic arguments</param>
        /// <returns></returns>
        public static T CallPrivate<T>(this ContentManager contentManager, string methodName, params object[] args)
        {
            return (T)contentManager.GetType().InvokeMember(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.InvokeMethod, null, contentManager, args);
        }

        /// <summary>
        /// Extension method on <see cref="ContentReader"/> to call a private method. Will not work
        /// with public methods. There is no static type checks for the arguments you pass, so make
        /// sure it matches what you need to call.
        /// <para/>
        /// This has a slightly different implementation to <see
        /// cref="CallPrivate{T}(ContentManager, string, object[])"/> because the only use of this
        /// function is to call a generic method (ReadAsset) with two overloads, and this means more complexity.
        /// </summary>
        /// <typeparam name="T">The return type of the private method</typeparam>
        /// <param name="contentReader">The content reader instance</param>
        /// <param name="methodName">The name of the private method</param>
        /// <param name="args">Any arguments, either as an array or variadic arguments</param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException">If no appropriate method is found</exception>
        public static T CallPrivate<T>(this ContentReader contentReader, string methodName, params object[] args)
        {
            // ContentReader has two overloads for ReadAsset. Both are private, both are generic. We
            // filter on either of those based on the argument count and method name.
            var method = contentReader.GetType().GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SingleOrDefault(m =>
                m.Name == methodName &&
                m.GetGenericArguments().Length == 1 &&
                m.GetParameters().Length == args.Length
            );

            if (method == null)
            {
                throw new NullReferenceException($"No private method '{methodName}' with {args.Length} arguments found for ContentReader");
            }

            // Make a concrete version of the generic method using our desired type, then invoke it
            return (T)method.MakeGenericMethod(typeof(T)).Invoke(contentReader, args);
        }

        /// <summary>
        /// Asynchronously load an asset that has been processed by the Content Pipeline.
        /// </summary>
        /// <typeparam name="T">
        /// The type of asset to load. The same types as the synchronous <see
        /// cref="ContentManager.Load{T}(string)"/> are supported.
        /// </typeparam>
        /// <param name="contentManager">The content manager instance</param>
        /// <param name="assetName">
        /// Asset name, relative to loader root directory, and not including the .xnb extension
        /// </param>
        /// <returns>
        /// The loaded asset. Repeated calls to load the same asset will return the same object instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">The assetName argument is null</exception>
        /// <exception cref="ObjectDisposedException">
        /// Load was called after the ContentManager was disposed
        /// </exception>
        public static async Task<T> LoadAsync<T>(this ContentManager contentManager, string assetName)
        {
            // Almost line-by-line equivalence to Load: https://github.com/MonoGame/MonoGame/blob/b21463b419e55b4c898030fc22bee77dabb11210/MonoGame.Framework/Content/ContentManager.cs#L224
            if (string.IsNullOrEmpty(assetName))
            {
                throw new ArgumentNullException("assetName");
            }

            if (contentManager.GetPrivate<bool>("disposed"))
            {
                throw new ObjectDisposedException("ContentManager");
            }

            T result = default(T);
            string key = assetName.Replace('\\', '/');

            // We can't access the private dictionary of loaded assets, use reflection
            object asset = null;
            var loadedAssets = contentManager.GetPrivate<Dictionary<string, object>>("loadedAssets");
            if (loadedAssets.TryGetValue(key, out asset) && asset is T)
            {
                return (T)asset;
            }

            try
            {
                // Here is the difference to the default Load method, we await an asynchronous read
                result = await contentManager.ReadAssetAsync<T>(assetName, null);
                loadedAssets[key] = result;
            }
            catch (Exception e)
            {
                throw new ContentLoadException("Failed to load asset (typo? has it been added to the pipeline?): " + assetName, e);
            }
            return result;
        }

        /// <summary>
        /// Read an asset from the file system and initialize it asynchronously.
        /// </summary>
        /// <typeparam name="T">
        /// The type of asset to load. See <see cref="LoadAsync{T}(ContentManager, string)"/> for
        /// moer info.
        /// </typeparam>
        /// <param name="contentManager">The content manager instance</param>
        /// <param name="assetName">The name of the asset to load</param>
        /// <param name="recordDisposableObject">
        /// No idea what this is for but we're mimicing the ReadAsset API as-is here
        /// </param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">The assetName argument is null</exception>
        /// <exception cref="ObjectDisposedException">
        /// ReadAssetAsync was called after the ContentManager was disposed
        /// </exception>
        /// <exception cref="ContentLoadException">
        /// The ContentReader for the type T didn't successfully return a value
        /// </exception>
        public static async Task<T> ReadAssetAsync<T>(this ContentManager contentManager, string assetName, Action<IDisposable> recordDisposableObject)
        {
            // Similar to https://github.com/MonoGame/MonoGame/blob/b21463b419e55b4c898030fc22bee77dabb11210/MonoGame.Framework/Content/ContentManager.cs#L305
            if (string.IsNullOrEmpty(assetName))
            {
                throw new ArgumentNullException("assetName");
            }

            if (contentManager.GetPrivate<bool>("disposed"))
            {
                throw new ObjectDisposedException("ContentManager");
            }

            // Call the original OpenStream to open the file into a stream. This can be done in
            // parallel from any thread asynchronously.
            object obj = null;
            Stream stream = contentManager.CallPrivate<Stream>("OpenStream", assetName);

            System.Diagnostics.Debug.WriteLine($"Reading {assetName} (disk -> bytes), we can be in any thread. Check: {Thread.CurrentThread.ManagedThreadId}");

            // Read the stream into memory to keep it. This is where the difference begins compared
            // to the synchronous ReadAsset implementation.
            MemoryStream memory = new MemoryStream();
            await stream.CopyToAsync(memory);
            memory.Seek(0, SeekOrigin.Begin);
            stream.Close();
            stream = memory;

            // Now, we want to execute the rest of this function in the main thread since we are
            // going to use ContentReaders and those will block and wait for the UI thread (and is
            // the whole problem we want to avoid) if we execute from any other thread.
            TaskCompletionSource taskCompletionSource = new();

            // Set up an empty await-able task, that gets completed when invoked from the Update
            // method. When SetResult() is called on a TaskCompletionSource, the awaiting code (i.e.
            // the code below in this function) will execute *synchronously* from where SetResult()
            // was called. This means we get to execute the rest of the code in the main thread
            // within an Update().
            queuedInitializations.Enqueue(() => taskCompletionSource.SetResult());
            await taskCompletionSource.Task;

            System.Diagnostics.Debug.WriteLine($"Reading {assetName} (bytes -> init), we should be in main thread. Check: {Thread.CurrentThread.ManagedThreadId}");

            // The following is almost line-by-line equivalent to the original synchronous ReadAsset
            // function in ContentManager, although we use reflection here and there to access
            // private methods. It being synchronous is fine, because the most time-consuming part
            // of an asset load is the file IO, which we have managed to do asynchronously (above).
            // Any thread can call LoadAsync and await for the asset without blocking the UI thread.
            // The initialization after that must be done synchronously from the main thread, but
            // this is a quick procedure and won't affect performance a lot.

            using BinaryReader xnbReader = new BinaryReader(stream);

            using ContentReader contentReader = contentManager.CallPrivate<ContentReader>("GetContentReaderFromXnb", assetName, stream, xnbReader, recordDisposableObject);
            obj = contentReader.CallPrivate<T>("ReadAsset", new object[] { });
            if (obj is GraphicsResource)
            {
                ((GraphicsResource)obj).Name = assetName;
            }

            if (obj == null)
            {
                throw new ContentLoadException("Could not load " + assetName + " asset!");
            }

            return (T)obj;
        }
    }
}
