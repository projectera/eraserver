using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ERA.Tools.HeadlessClient.Protocols
{
    public class LinkedHashMap<K, T>
    {
        private Dictionary<K, LinkedHashMapValue> _hashMap;

        private LinkedHashMapValue _first;
        private LinkedHashMapValue _last;

        public Int32 Size { get; protected set; }

        private ReaderWriterLockSlim _rwLock;
        //private ReaderWriterLockSlim _rwLastLock;
        //private ReaderWriterLockSlim _rwFirstLock;

        /// <summary>
        /// Creates a new LinkedHashMap.
        /// </summary>
        public LinkedHashMap()
        {
            _last = new LinkedHashMapValue(default(K), default(T), null, null);
            _first = _last;

            _hashMap = new Dictionary<K, LinkedHashMapValue>();
            _rwLock = new ReaderWriterLockSlim();
            //_rwLastLock = new ReaderWriterLockSlim();
            //_rwFirstLock = new ReaderWriterLockSlim();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="comparer"></param>
        public LinkedHashMap(IEqualityComparer<K> comparer)
        {
            _last = new LinkedHashMapValue(default(K), default(T), null, null);
            _first = _last;

            _hashMap = new Dictionary<K, LinkedHashMapValue>(comparer);
            _rwLock = new ReaderWriterLockSlim();
            //_rwLastLock = new ReaderWriterLockSlim();
            //_rwFirstLock = new ReaderWriterLockSlim();
        }

        /// <summary>
        /// Returns the value stored under a key
        /// </summary>
        /// <param name="key">The key to be looked up</param>
        /// <returns>The value stored</returns>
        public T this[K key]
        {
            get
            {
                try
                {
                    _rwLock.EnterReadLock();
                    return _hashMap[key].Value;
                }
                finally
                {
                    _rwLock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Resets the LinkedHashMap
        /// </summary>
        public void Clear()
        {
            _rwLock.EnterWriteLock();
            //_rwLastLock.EnterWriteLock();
            //_rwFirstLock.EnterWriteLock();
            lock (_first)
            {
                _last = new LinkedHashMapValue(default(K), default(T), null, null);
                _first = _last;
                _hashMap.Clear();
                Size = 0;
            }
            //_rwFirstLock.ExitWriteLock();
            //_rwLastLock.ExitWriteLock();
            _rwLock.ExitWriteLock();
        }

        /// <summary>
        /// Returns the topmost value without removing it
        /// </summary>
        /// <returns>The topmost value</returns>
        public T Peek()
        {
            try
            {
                //_rwLastLock.EnterReadLock();
                if (_last.Prev == null)
                    return default(T);
                return _last.Prev.Value;
            }
            finally
            {
                //_rwLastLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Returns the topmost value and removes it
        /// </summary>
        /// <returns>The topmost value</returns>
        public T Dequeue()
        {
            try
            {
                //_rwLastLock.EnterReadLock();
                if (_last.Prev == null)
                    return default(T);
                T ret = _last.Prev.Value;
                Remove(_last.Prev.Key);
                return ret;
            }
            finally
            {
                //_rwLastLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Adds a value to the LinkedHashMap
        /// </summary>
        /// <param name="key">The key to be added</param>
        /// <param name="value">The value to be added</param>
        public Boolean Enqueue(K key, T value)
        {
            try
            {

                //_rwFirstLock.EnterWriteLock();
                lock (_first)
                {
                    LinkedHashMapValue v = new LinkedHashMapValue(key, value, null, _first);
                    _first.Prev = v;
                    _first = v;

                    //_rwFirstLock.ExitWriteLock();
                    _rwLock.EnterWriteLock();
                    _hashMap.Add(key, v);
                }
                Size++;
            }
            catch (Exception)
            {
                //Logger.Logger.Warning("Duplicate key " + key.ToString());
                return false;
            }
            finally
            {
                if (_rwLock.IsWriteLockHeld)
                    _rwLock.ExitWriteLock();
            }

            return true;
        }

        /// <summary>
        /// Removes a certain key from the LinkedHashMap
        /// </summary>
        /// <param name="key">The key to be removed</param>
        public Boolean Remove(K key)
        {
            try
            {
                _rwLock.EnterWriteLock();
                LinkedHashMapValue val = _hashMap[key];
                _hashMap.Remove(key);
                val.Remove();
                Size--;
            }
            catch (KeyNotFoundException)
            {
                return false;
            }
            finally
            {
                if (_rwLock.IsWriteLockHeld)
                    _rwLock.ExitWriteLock();
            }

            return true;
        }

        /// <summary>
        /// Returns wether a key is in this LinkedHashMap
        /// </summary>
        /// <param name="key">The key to query</param>
        /// <returns>Wether this key exists in this LinkedHashMap</returns>
        public Boolean ContainsKey(K key)
        {
            try
            {
                _rwLock.EnterReadLock();
                return _hashMap.ContainsKey(key);
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public Boolean TryGetValue(K key, out T value)
        {
            value = default(T);

            LinkedHashMapValue innervalue;
            try
            {
                _rwLock.EnterReadLock();
                if (_hashMap.TryGetValue(key, out innervalue))
                {
                    value = innervalue.Value;
                    return true;
                }
            }
            finally
            {
                _rwLock.ExitReadLock();
            }

            return false;
        }

        /// <summary>
        /// Moves a key to the front of the queue
        /// </summary>
        /// <param name="key">The key to be moved to the front</param>
        public void Requeue(K key)
        {
            _rwLock.EnterReadLock();
            LinkedHashMapValue v = _hashMap[key];
            v.Remove();
            v.Next = _first;
            v.Prev = null;
            _rwLock.ExitReadLock();

            //_rwFirstLock.EnterWriteLock();
            lock (_first)
                _first = v;
            //_rwFirstLock.ExitWriteLock();
        }

        /// <summary>
        /// 
        /// </summary>
        internal class LinkedHashMapValue
        {
            public LinkedHashMapValue Prev { get; set; }
            public LinkedHashMapValue Next { get; set; }
            public K Key { get; set; }
            public T Value { get; set; }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="key"></param>
            /// <param name="value"></param>
            /// <param name="prev"></param>
            /// <param name="next"></param>
            public LinkedHashMapValue(K key, T value, LinkedHashMapValue prev, LinkedHashMapValue next)
            {
                this.Key = key;
                this.Value = value;
                this.Prev = prev;
                this.Next = next;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            internal T Remove()
            {
                if (this.Prev != null)
                    this.Prev.Next = Next;
                if (this.Next != null)
                    this.Next.Prev = Prev;
                this.Next = null;
                this.Prev = null;
                return Value;
            }
        }
    }
}
