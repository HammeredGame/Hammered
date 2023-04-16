using Microsoft.Xna.Framework;

namespace HammeredGame.Core
{
    /// <summary>
    /// A dependency injection container for various global one-off game services, like Input
    /// handlers, graphic device, or content loaders.
    /// <para/>
    /// Adapted from https://roy-t.nl/2010/08/25/xna-accessing-contentmanager-and-graphicsdevice-anywhere-anytime-the-gameservicecontainer.html
    /// </summary>
    public class GameServices
    {
        /// <summary>
        /// A GameServiceContainer is internally a dictionary from types to instances. Its lookup
        /// cost is higher than a direct reference but negligible as long as there's not so many
        /// things in it. This means calling GetService() is efficient.
        /// </summary>
        private readonly GameServiceContainer container = new();

        /// <summary>
        /// Get a service provider for the service of the specified type. This method approaches an
        /// O(1) operation.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetService<T>()
        {
            return (T)container.GetService(typeof(T));
        }

        /// <summary>
        /// Add a service provider of the specified type. This method will delete any preexisting
        /// service with the same type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="service"></param>
        public void AddService<T>(T service)
        {
            if (GetService<T>() != null)
            {
                RemoveService<T>();
            }
            container.AddService(typeof(T), service);
        }

        /// <summary>
        /// Remove the service provider of the specified type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void RemoveService<T>()
        {
            container.RemoveService(typeof(T));
        }
    }
}
