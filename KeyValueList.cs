namespace RoliSoft.TVShowTracker
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents a collection of keys and values, but the keys don't have to be unique.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    public class KeyValueList<TKey, TValue> : List<KeyValuePair<TKey, TValue>>
    {
        /// <summary>
        /// Adds the specified key and value to the dictionary.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add. The value can be <c>null</c> for reference types.</param>
        public void Add(TKey key, TValue value)
        {
            Add(new KeyValuePair<TKey, TValue>(key, value));
        }
    }
}
