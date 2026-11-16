using System;
using System.Collections.Generic;

namespace AssetManager.Collections
{
    public sealed class MultiDictionary<K, V>
        where K : notnull
    {
        private readonly Dictionary<K, List<V>> _dictionary;

        private int _valueCount;

        /// <summary>
        /// Total number of values across all keys.
        /// </summary>
        public int Count => _valueCount;

        public int ValueCount => _valueCount;

        /// <summary>
        /// Number of distinct keys.
        /// </summary>
        public int KeyCount => _dictionary.Count;

        public IEqualityComparer<K> Comparer => _dictionary.Comparer;

        public IEnumerable<K> Keys => _dictionary.Keys;

        public MultiDictionary()
            : this(0, default!)
        {
        }

        public MultiDictionary(IEqualityComparer<K> comparer)
            : this(0, comparer)
        {
        }

        public MultiDictionary(int capacity, IEqualityComparer<K> comparer = default!)
        {
            _dictionary = new Dictionary<K, List<V>>(capacity, comparer);
        }

        private void EnsureCanAdd(int count)
        {
            if (count > int.MaxValue - _valueCount)
                throw new OverflowException("Value count exceeds Int32.MaxValue.");
        }

        public void Add(K key, V value)
        {
            EnsureCanAdd(1);

            if (_dictionary.TryGetValue(key, out List<V> list))
            {
                list.Add(value);
            }
            else
            {
                list = new List<V>(1)
                {
                    value
                };

                _dictionary.Add(key, list);
            }

            _valueCount++;
        }

        public void AddRange(K key, IEnumerable<V> values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            // Use ICollection directly when possible.
            // Otherwise, buffer the enumerable to avoid multiple enumeration
            // and modification-during-enumeration problems.

            if (values is not ICollection<V> collection)
            {
                List<V> buffer = new();

                foreach (V value in values)
                {
                    buffer.Add(value);
                }

                collection = buffer;
            }

            int addedCount = collection.Count;

            if (addedCount == 0)
                return;

            EnsureCanAdd(addedCount);

            if (_dictionary.TryGetValue(key, out List<V> list))
            {
                list.AddRange(collection);
            }
            else
            {
                list = new List<V>(addedCount);
                list.AddRange(collection);

                _dictionary.Add(key, list);
            }

            _valueCount += addedCount;
        }

        public bool Remove(K key, V value)
        {
            if (!_dictionary.TryGetValue(key, out List<V> list))
                return false;

            if (!list.Remove(value))
                return false;

            _valueCount--;

            if (list.Count == 0)
                _dictionary.Remove(key);

            return true;
        }

        public bool Remove(K key)
        {
            if (!_dictionary.TryGetValue(key, out List<V> list))
                return false;

            int removedCount = list.Count;

            if (!_dictionary.Remove(key))
                return false;

            _valueCount -= removedCount;

            return true;
        }

        public void Clear()
        {
            _dictionary.Clear();
            _valueCount = 0;
        }

        public bool ContainsKey(K key)
        {
            return _dictionary.ContainsKey(key);
        }

        public bool TryGetValues(
            K key,
            out IReadOnlyList<V> values)
        {
            if (_dictionary.TryGetValue(key, out List<V> list))
            {
                // Return a snapshot, not the internal List.
                values = list.ToArray();
                return true;
            }

            values = default!;
            return false;
        }

        public IReadOnlyList<V> GetValues(K key)
        {
            if (_dictionary.TryGetValue(key, out List<V> list))
                return list.ToArray();

            return Array.Empty<V>();
        }

        /// <summary>
        /// Gets a read-only live view of the existing value list.
        /// Unlike GetValues, this does not copy the list.
        /// </summary>
        public IReadOnlyList<V> GetValuesView(K key)
        {
            if (_dictionary.TryGetValue(key, out List<V> list))
                return list.AsReadOnly();

            return Array.Empty<V>();
        }

        public bool TryGetValuesView(
            K key,
            out IReadOnlyList<V> values)
        {
            if (_dictionary.TryGetValue(key, out List<V> list))
            {
                values = list.AsReadOnly();
                return true;
            }

            values = default!;
            return false;
        }

        /// <summary>
        /// Enumerates all values without creating intermediate collections.
        /// </summary>
        public IEnumerable<V> EnumerateValues()
        {
            foreach (List<V> list in _dictionary.Values)
            {
                foreach (V value in list)
                {
                    yield return value;
                }
            }
        }

        /// <summary>
        /// Enumerates flattened key-value pairs.
        /// </summary>
        public MapPair<K, V>[] EnumeratePairs()
        {
            if (_valueCount == 0)
                return Array.Empty<MapPair<K, V>>();

            MapPair<K, V>[] result = new MapPair<K, V>[_valueCount];

            int index = 0;

            foreach (KeyValuePair<K, List<V>> group in _dictionary)
            {
                foreach (V value in group.Value)
                {
                    result[index++] = new MapPair<K, V>(group.Key, value);
                }
            }

            return result;
        }

        /// <summary>
        /// Enumerates key groups using read-only views.
        /// </summary>
        public IEnumerable<KeyValuePair<K, IReadOnlyList<V>>> EnumerateGroups()
        {
            foreach (KeyValuePair<K, List<V>> group in _dictionary)
            {
                yield return new KeyValuePair<K, IReadOnlyList<V>>(
                    group.Key,
                    group.Value.AsReadOnly());
            }
        }
    }
}
