using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ERA.Utils
{
    public class ThreadsafeDictOfDict<K, I, T>
    {
        Dictionary<K, Dictionary<I, T>> _instances;
        ReaderWriterLockSlim _readerWriterLock;

        /// <summary>
        /// Creates a new threadsafe dictionairy of dictionaries
        /// </summary>
        public ThreadsafeDictOfDict()
        {
            _instances = new Dictionary<K, Dictionary<I, T>>();
            _readerWriterLock = new ReaderWriterLockSlim();
        }

        /// <summary>
        /// Get the outer keys
        /// </summary>
        public List<K> GetKeys()
        {
            List<K> result;
            _readerWriterLock.EnterReadLock();
            result = _instances.Keys.ToList();
            _readerWriterLock.ExitReadLock();

            return result;
        }

        /// <summary>
        /// Get the inner keys of an outer key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public List<I> GetKeysOf(K key)
        {
            List<I> result = null;
            _readerWriterLock.EnterReadLock();
            if (_instances.ContainsKey(key))
                result = _instances[key].Keys.ToList();
            _readerWriterLock.ExitReadLock();

            return result;
        }

        /// <summary>
        /// Get the value of key i in the k dictionairy;
        /// </summary>
        /// <param name="k"></param>
        /// <param name="i"></param>
        /// <returns></returns>
        public T GetValueOf(K k, I i)
        {
            _readerWriterLock.EnterReadLock();
            if (!_instances.ContainsKey(k))
                return default(T);
            if (!_instances[k].ContainsKey(i))
                return default(T);
            var result = _instances[k][i];
            _readerWriterLock.ExitReadLock();
            return result;
        }

        /// <summary>
        /// Adds value in the k dictionairy at key i
        /// </summary>
        /// <param name="k"></param>
        /// <param name="i"></param>
        /// <param name="value"></param>
        public void AddInside(K k, I i, T value)
        {
            _readerWriterLock.EnterWriteLock();
            if (!_instances.ContainsKey(k))
                _instances.Add(k, new Dictionary<I, T>());
            _instances[k].Add(i, value);
            _readerWriterLock.ExitWriteLock();
        }

        /// <summary>
        /// Removes value from the k dictionairy at key i
        /// </summary>
        /// <param name="k"></param>
        /// <param name="i"></param>
        /// <param name="value"></param>
        public Boolean RemoveInside(K k, I i)
        {
            _readerWriterLock.EnterWriteLock();
            if (!_instances.ContainsKey(k))
                return false;
            var result = _instances[k].Remove(i);
            _readerWriterLock.ExitWriteLock();
            return result;
        }
    }
}
