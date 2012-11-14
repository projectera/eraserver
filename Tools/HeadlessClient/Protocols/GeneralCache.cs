using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace HeadlessClient.Protocols
{
    internal partial class GeneralCache<K, T>
        where T : ICacheable<K>
    {
        /// <summary>
        /// The data cache
        /// </summary>
        private static LinkedHashMap<K, T> _cache;
        /// <summary>
        /// The lock that regulates reading and writing to the cache
        /// </summary>
        private static ReaderWriterLockSlim _cacheLock;

        /// <summary>
        /// Initializes the cache
        /// </summary>
        internal static void InitializeCache()
        {
            Interlocked.CompareExchange(ref _cache, new LinkedHashMap<K, T>(), null);
            Interlocked.CompareExchange(ref _cacheLock, new ReaderWriterLockSlim(), null);
        }

        /// <summary>
        /// Adds a value to the cache
        /// </summary>
        /// <param name="value">The value to add to the cache</param>
        internal static void AddCache(T value)
        {
            _cacheLock.EnterWriteLock();
            _cache.Enqueue(value.Key, value);
            _cacheLock.ExitWriteLock();
        }

        /// <summary>
        /// Removes a key from the cache
        /// </summary>
        /// <param name="key">The key to be removed</param>
        internal static void RemoveCache(K key)
        {
            _cacheLock.EnterWriteLock();
            T value = _cache[key];
            _cache.Remove(value.Key);
            _cacheLock.ExitWriteLock();
        }

        /// <summary>
        /// Tries to retrieve a key from the cache
        /// </summary>
        /// <param name="key">The key to retrieve</param>
        /// <returns>The DataStoreValue if the requested key is in the cache or null if it is absent</returns>
        internal static T QueryCache(K key)
        {
            _cacheLock.EnterReadLock();

            T value;
            if (_cache.TryGetValue(key, out value))
                _cache.Requeue(key);

            _cacheLock.ExitReadLock();

            return value;
        }

        /// <summary>
        /// Updates the cache (or adds if non-existant)
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        internal static T UpdateCache(T value)
        {
            _cacheLock.EnterUpgradeableReadLock();

            T currentvalue;
            if (_cache.TryGetValue(value.Key, out currentvalue))
                RemoveCache(value.Key);

            AddCache(value);

            _cacheLock.ExitUpgradeableReadLock();
            return currentvalue;
        }

        /// <summary>
        /// Clears the cache
        /// </summary>
        internal static void ClearCache()
        {
            _cacheLock.EnterWriteLock();
            _cache = new LinkedHashMap<K, T>();
            _cacheLock.ExitWriteLock();
        }
    }
}
