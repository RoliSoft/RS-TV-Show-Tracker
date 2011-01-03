// JsonObject.cs
// Source: https://github.com/NikhilK/dynamicrest/tree/9fae9d32c6ffb13081744afda019cce625311f1e/DynamicRest
// Developer: http://www.nikhilk.net/CSharp-Dynamic-Programming-JSON.aspx
//
// Comments for public methods were added by me, but there are no other modifications to the code.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;

namespace RoliSoft.TVShowTracker.DynamicJson {

    /// <summary>
    /// Represents a JSON object.
    /// </summary>
    public sealed class JsonObject : DynamicObject, IDictionary<string, object>, IDictionary {

        private Dictionary<string, object> _members;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonObject"/> class.
        /// </summary>
        public JsonObject() {
            _members = new Dictionary<string, object>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonObject"/> class.
        /// </summary>
        /// <param name="nameValuePairs">The name value pairs.</param>
        public JsonObject(params object[] nameValuePairs)
            : this() {
            if (nameValuePairs != null) {
                if (nameValuePairs.Length % 2 != 0) {
                    throw new ArgumentException("Mismatch in name/value pairs.");
                }

                for (int i = 0; i < nameValuePairs.Length; i += 2) {
                    if (!(nameValuePairs[i] is string)) {
                        throw new ArgumentException("Name parameters must be strings.");
                    }

                    _members[(string)nameValuePairs[i]] = nameValuePairs[i + 1];
                }
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="System.Object"/> with the specified key.
        /// </summary>
        /// <value>Array item.</value>
        public object this[string key] {
            get {
                return ((IDictionary<string, object>)this)[key];
            }
            set {
                ((IDictionary<string, object>)this)[key] = value;
            }
        }

        /// <summary>
        /// Determines whether the array contains the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>
        /// 	<c>true</c> if the array contains the specified key; otherwise, <c>false</c>.
        /// </returns>
        public bool ContainsKey(string key) {
            return ((IDictionary<string, object>)this).ContainsKey(key);
        }

        /// <summary>
        /// Provides implementation for type conversion operations. Classes derived from the <see cref="T:System.Dynamic.DynamicObject"/> class can override this method to specify dynamic behavior for operations that convert an object from one type to another.
        /// </summary>
        /// <param name="binder">Provides information about the conversion operation. The binder.Type property provides the type to which the object must be converted. For example, for the statement (String)sampleObject in C# (CType(sampleObject, Type) in Visual Basic), where sampleObject is an instance of the class derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, binder.Type returns the <see cref="T:System.String"/> type. The binder.Explicit property provides information about the kind of conversion that occurs. It returns true for explicit conversion and false for implicit conversion.</param>
        /// <param name="result">The result of the type conversion operation.</param>
        /// <returns>
        /// true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of the language determines the behavior. (In most cases, a language-specific run-time exception is thrown.)
        /// </returns>
        public override bool TryConvert(ConvertBinder binder, out object result) {
            Type targetType = binder.Type;

            if ((targetType == typeof(IEnumerable)) ||
                (targetType == typeof(IEnumerable<KeyValuePair<string, object>>)) ||
                (targetType == typeof(IDictionary<string, object>)) ||
                (targetType == typeof(IDictionary))) {
                result = this;
                return true;
            }

            return base.TryConvert(binder, out result);
        }

        /// <summary>
        /// Provides the implementation for operations that delete an object member. This method is not intended for use in C# or Visual Basic.
        /// </summary>
        /// <param name="binder">Provides information about the deletion.</param>
        /// <returns>
        /// true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of the language determines the behavior. (In most cases, a language-specific run-time exception is thrown.)
        /// </returns>
        public override bool TryDeleteMember(DeleteMemberBinder binder) {
            return _members.Remove(binder.Name);
        }

        /// <summary>
        /// Provides the implementation for operations that get a value by index. Classes derived from the <see cref="T:System.Dynamic.DynamicObject"/> class can override this method to specify dynamic behavior for indexing operations.
        /// </summary>
        /// <param name="binder">Provides information about the operation.</param>
        /// <param name="indexes">The indexes that are used in the operation. For example, for the sampleObject[3] operation in C# (sampleObject(3) in Visual Basic), where sampleObject is derived from the DynamicObject class, <paramref name="indexes[0]"/> is equal to 3.</param>
        /// <param name="result">The result of the index operation.</param>
        /// <returns>
        /// true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of the language determines the behavior. (In most cases, a run-time exception is thrown.)
        /// </returns>
        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result) {
            if (indexes.Length == 1) {
                result = ((IDictionary<string, object>)this)[(string)indexes[0]];
                return true;
            }

            return base.TryGetIndex(binder, indexes, out result);
        }

        /// <summary>
        /// Provides the implementation for operations that get member values. Classes derived from the <see cref="T:System.Dynamic.DynamicObject"/> class can override this method to specify dynamic behavior for operations such as getting a value for a property.
        /// </summary>
        /// <param name="binder">Provides information about the object that called the dynamic operation. The binder.Name property provides the name of the member on which the dynamic operation is performed. For example, for the Console.WriteLine(sampleObject.SampleProperty) statement, where sampleObject is an instance of the class derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, binder.Name returns "SampleProperty". The binder.IgnoreCase property specifies whether the member name is case-sensitive.</param>
        /// <param name="result">The result of the get operation. For example, if the method is called for a property, you can assign the property value to <paramref name="result"/>.</param>
        /// <returns>
        /// true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of the language determines the behavior. (In most cases, a run-time exception is thrown.)
        /// </returns>
        public override bool TryGetMember(GetMemberBinder binder, out object result) {
            object value;
            if (_members.TryGetValue(binder.Name, out value)) {
                result = value;
                return true;
            }
            return base.TryGetMember(binder, out result);
        }

        /// <summary>
        /// Provides the implementation for operations that set a value by index. Classes derived from the <see cref="T:System.Dynamic.DynamicObject"/> class can override this method to specify dynamic behavior for operations that access objects by a specified index.
        /// </summary>
        /// <param name="binder">Provides information about the operation.</param>
        /// <param name="indexes">The indexes that are used in the operation. For example, for the sampleObject[3] = 10 operation in C# (sampleObject(3) = 10 in Visual Basic), where sampleObject is derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, <paramref name="indexes[0]"/> is equal to 3.</param>
        /// <param name="value">The value to set to the object that has the specified index. For example, for the sampleObject[3] = 10 operation in C# (sampleObject(3) = 10 in Visual Basic), where sampleObject is derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, <paramref name="value"/> is equal to 10.</param>
        /// <returns>
        /// true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of the language determines the behavior. (In most cases, a language-specific run-time exception is thrown.
        /// </returns>
        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value) {
            if (indexes.Length == 1) {
                ((IDictionary<string, object>)this)[(string)indexes[0]] = value;
                return true;
            }

            return base.TrySetIndex(binder, indexes, value);
        }

        /// <summary>
        /// Provides the implementation for operations that set member values. Classes derived from the <see cref="T:System.Dynamic.DynamicObject"/> class can override this method to specify dynamic behavior for operations such as setting a value for a property.
        /// </summary>
        /// <param name="binder">Provides information about the object that called the dynamic operation. The binder.Name property provides the name of the member to which the value is being assigned. For example, for the statement sampleObject.SampleProperty = "Test", where sampleObject is an instance of the class derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, binder.Name returns "SampleProperty". The binder.IgnoreCase property specifies whether the member name is case-sensitive.</param>
        /// <param name="value">The value to set to the member. For example, for sampleObject.SampleProperty = "Test", where sampleObject is an instance of the class derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, the <paramref name="value"/> is "Test".</param>
        /// <returns>
        /// true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of the language determines the behavior. (In most cases, a language-specific run-time exception is thrown.)
        /// </returns>
        public override bool TrySetMember(SetMemberBinder binder, object value) {
            _members[binder.Name] = value;
            return true;
        }

        #region Implementation of IEnumerable
        IEnumerator IEnumerable.GetEnumerator() {
            return new DictionaryEnumerator(_members.GetEnumerator());
        }
        #endregion

        #region Implementation of IEnumerable<KeyValuePair<string, object>>
        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator() {
            return _members.GetEnumerator();
        }
        #endregion

        #region Implementation of ICollection
        int ICollection.Count {
            get {
                return _members.Count;
            }
        }

        bool ICollection.IsSynchronized {
            get {
                return false;
            }
        }

        object ICollection.SyncRoot {
            get {
                return this;
            }
        }

        void ICollection.CopyTo(Array array, int index) {
            throw new NotImplementedException();
        }
        #endregion

        #region Implementation of ICollection<KeyValuePair<string, object>>
        int ICollection<KeyValuePair<string, object>>.Count {
            get {
                return _members.Count;
            }
        }

        bool ICollection<KeyValuePair<string, object>>.IsReadOnly {
            get {
                return false;
            }
        }

        void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> item) {
            ((IDictionary<string, object>)_members).Add(item);
        }

        void ICollection<KeyValuePair<string, object>>.Clear() {
            _members.Clear();
        }

        bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item) {
            return ((IDictionary<string, object>)_members).Contains(item);
        }

        void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) {
            ((IDictionary<string, object>)_members).CopyTo(array, arrayIndex);
        }

        bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item) {
            return ((IDictionary<string, object>)_members).Remove(item);
        }
        #endregion

        #region Implementation of IDictionary
        bool IDictionary.IsFixedSize {
            get {
                return false;
            }
        }

        bool IDictionary.IsReadOnly {
            get {
                return false;
            }
        }

        ICollection IDictionary.Keys {
            get {
                return _members.Keys;
            }
        }

        object IDictionary.this[object key] {
            get {
                return ((IDictionary<string, object>)this)[(string)key];
            }
            set {
                ((IDictionary<string, object>)this)[(string)key] = value;
            }
        }

        ICollection IDictionary.Values {
            get {
                return _members.Values;
            }
        }

        void IDictionary.Add(object key, object value) {
            _members.Add((string)key, value);
        }

        void IDictionary.Clear() {
            _members.Clear();
        }

        bool IDictionary.Contains(object key) {
            return _members.ContainsKey((string)key);
        }

        IDictionaryEnumerator IDictionary.GetEnumerator() {
            return (IDictionaryEnumerator)((IEnumerable)this).GetEnumerator();
        }

        void IDictionary.Remove(object key) {
            _members.Remove((string)key);
        }
        #endregion

        #region Implementation of IDictionary<string, object>
        ICollection<string> IDictionary<string, object>.Keys {
            get {
                return _members.Keys;
            }
        }

        object IDictionary<string, object>.this[string key] {
            get {
                object value = null;
                if (_members.TryGetValue(key, out value)) {
                    return value;
                }
                return null;
            }
            set {
                _members[key] = value;
            }
        }

        ICollection<object> IDictionary<string, object>.Values {
            get {
                return _members.Values;
            }
        }

        void IDictionary<string, object>.Add(string key, object value) {
            _members.Add(key, value);
        }

        bool IDictionary<string, object>.ContainsKey(string key) {
            return _members.ContainsKey(key);
        }

        bool IDictionary<string, object>.Remove(string key) {
            return _members.Remove(key);
        }

        bool IDictionary<string, object>.TryGetValue(string key, out object value) {
            return _members.TryGetValue(key, out value);
        }
        #endregion


        /// <summary>
        /// Represents a dictionary enumerator.
        /// </summary>
        private sealed class DictionaryEnumerator : IDictionaryEnumerator {

            private IEnumerator<KeyValuePair<string, object>> _enumerator;

            /// <summary>
            /// Initializes a new instance of the <see cref="DictionaryEnumerator"/> class.
            /// </summary>
            /// <param name="enumerator">The enumerator.</param>
            public DictionaryEnumerator(IEnumerator<KeyValuePair<string, object>> enumerator) {
                _enumerator = enumerator;
            }

            /// <summary>
            /// Gets the current element in the collection.
            /// </summary>
            /// <value></value>
            /// <returns>The current element in the collection.</returns>
            /// <exception cref="T:System.InvalidOperationException">The enumerator is positioned before the first element of the collection or after the last element.</exception>
            public object Current {
                get {
                    return Entry;
                }
            }

            /// <summary>
            /// Gets both the key and the value of the current dictionary entry.
            /// </summary>
            /// <value></value>
            /// <returns>A <see cref="T:System.Collections.DictionaryEntry"/> containing both the key and the value of the current dictionary entry.</returns>
            /// <exception cref="T:System.InvalidOperationException">The <see cref="T:System.Collections.IDictionaryEnumerator"/> is positioned before the first entry of the dictionary or after the last entry. </exception>
            public DictionaryEntry Entry {
                get {
                    return new DictionaryEntry(_enumerator.Current.Key, _enumerator.Current.Value);
                }
            }

            /// <summary>
            /// Gets the key of the current dictionary entry.
            /// </summary>
            /// <value></value>
            /// <returns>The key of the current element of the enumeration.</returns>
            /// <exception cref="T:System.InvalidOperationException">The <see cref="T:System.Collections.IDictionaryEnumerator"/> is positioned before the first entry of the dictionary or after the last entry. </exception>
            public object Key {
                get {
                    return _enumerator.Current.Key;
                }
            }

            /// <summary>
            /// Gets the value of the current dictionary entry.
            /// </summary>
            /// <value></value>
            /// <returns>The value of the current element of the enumeration.</returns>
            /// <exception cref="T:System.InvalidOperationException">The <see cref="T:System.Collections.IDictionaryEnumerator"/> is positioned before the first entry of the dictionary or after the last entry. </exception>
            public object Value {
                get {
                    return _enumerator.Current.Value;
                }
            }

            /// <summary>
            /// Advances the enumerator to the next element of the collection.
            /// </summary>
            /// <returns>
            /// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
            /// </returns>
            /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception>
            public bool MoveNext() {
                return _enumerator.MoveNext();
            }

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element in the collection.
            /// </summary>
            /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception>
            public void Reset() {
                _enumerator.Reset();
            }
        }
    }
}
