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
    internal static class ContentManagerExtensions
    {
        public static T GetPrivate<T>(this ContentManager contentManager, string fieldName)
        {
            var field = contentManager.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (T)field?.GetValue(contentManager);
        }

        public static T CallPrivate<T>(this ContentManager contentManager, string  methodName, params object[] args)
        {
            return (T)contentManager.GetType().InvokeMember(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.InvokeMethod, null, contentManager, args);
        }

        public static T CallPrivate<T>(this ContentReader contentReader, string methodName, params object[] args)
        {
            var method = contentReader.GetType().GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SingleOrDefault(m =>
                m.Name == methodName &&
                m.GetGenericArguments().Length == 1 &&
                m.GetParameters().Length == args.Length
            );

            if (method == null)
            {
                throw new NullReferenceException($"No private method '{methodName}' with {args.Length} arguments found for ContentReader");
            }

            return (T)method.MakeGenericMethod(typeof(T)).Invoke(contentReader, args);
        }

        public static async Task<T> LoadAsync<T>(this ContentManager contentManager, ScriptUtils utils, string assetName)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                throw new ArgumentNullException("assetName");
            }

            if (contentManager.GetPrivate<bool>("disposed"))
            {
                throw new ObjectDisposedException("ContentManager");
            }

            T val = default(T);
            string key = assetName.Replace('\\', '/');
            object value = null;

            var loadedAssets = contentManager.GetPrivate<Dictionary<string, object>>("loadedAssets");
            if (loadedAssets.TryGetValue(key, out value) && value is T)
            {
                return (T)value;
            }

            val = await contentManager.ReadAssetAsync<T>(utils, assetName, null);
            loadedAssets[key] = val;
            return val;
        }

        public static async Task<T> ReadAssetAsync<T>(this ContentManager contentManager, ScriptUtils utils, string assetName, Action<IDisposable> recordDisposableObject)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                throw new ArgumentNullException("assetName");
            }

            if (contentManager.GetPrivate<bool>("disposed"))
            {
                throw new ObjectDisposedException("ContentManager");
            }

            object obj = null;
            Stream stream = contentManager.CallPrivate<Stream>("OpenStream", assetName);

            System.Diagnostics.Debug.WriteLine(assetName + Thread.CurrentThread.ManagedThreadId);
            MemoryStream memory = new MemoryStream();
            await stream.CopyToAsync(memory);
            memory.Seek(0, SeekOrigin.Begin);
            stream.Close();
            stream = memory;

            using (BinaryReader xnbReader = new BinaryReader(stream))
            {
                await utils.WaitNextUpdate();

                System.Diagnostics.Debug.WriteLine(assetName + Thread.CurrentThread.ManagedThreadId);
                using ContentReader contentReader = contentManager.CallPrivate<ContentReader>("GetContentReaderFromXnb", assetName, stream, xnbReader, recordDisposableObject);
                obj = contentReader.CallPrivate<T>("ReadAsset", new object[] { });
                if (obj is GraphicsResource)
                {
                    ((GraphicsResource)obj).Name = assetName;
                }
            }

            if (obj == null)
            {
                throw new ContentLoadException("Could not load " + assetName + " asset!");
            }

            return (T)obj;
        }
    }
}
