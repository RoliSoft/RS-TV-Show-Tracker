// JsonArray.cs
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
    /// Represents a JSON array.
    /// </summary>
    public sealed class JsonArray : DynamicObject, ICollection<object>, ICollection {

        private List<object> _members;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonArray"/> class.
        /// </summary>
        public JsonArray() {
            _members = new List<object>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonArray"/> class.
        /// </summary>
        /// <param name="o">The object.</param>
        public JsonArray(object o)
            : this() {
            _members.Add(o);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonArray"/> class.
        /// </summary>
        /// <param name="o1">The first object.</param>
        /// <param name="o2">The second object.</param>
        public JsonArray(object o1, object o2)
            : this() {
            _members.Add(o1);
            _members.Add(o2);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonArray"/> class.
        /// </summary>
        /// <param name="objects">The objects.</param>
        public JsonArray(params object[] objects)
            : this() {
            _members.AddRange(objects);
        }

        /// <summary>
        /// Gets the count.
        /// </summary>
        /// <value>The count.</value>
        public int Count {
            get {
                return _members.Count;
            }
        }

        /// <summary>
        /// Gets the <see cref="System.Object"/> at the specified index.
        /// </summary>
        /// <value>Array item.</value>
        public object this[int index] {
            get {
                return _members[index];
            }
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
                (targetType == typeof(IEnumerable<object>)) ||
                (targetType == typeof(ICollection<object>)) ||
                (targetType == typeof(ICollection))) {
                result = this;
                return true;
            }

            return base.TryConvert(binder, out result);
        }

        /// <summary>
        /// Provides the implementation for operations that invoke a member. Classes derived from the <see cref="T:System.Dynamic.DynamicObject"/> class can override this method to specify dynamic behavior for operations such as calling a method.
        /// </summary>
        /// <param name="binder">Provides information about the dynamic operation. The binder.Name property provides the name of the member on which the dynamic operation is performed. For example, for the statement sampleObject.SampleMethod(100), where sampleObject is an instance of the class derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, binder.Name returns "SampleMethod". The binder.IgnoreCase property specifies whether the member name is case-sensitive.</param>
        /// <param name="args">The arguments that are passed to the object member during the invoke operation. For example, for the statement sampleObject.SampleMethod(100), where sampleObject is derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, <paramref name="args[0]"/> is equal to 100.</param>
        /// <param name="result">The result of the member invocation.</param>
        /// <returns>
        /// true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of the language determines the behavior. (In most cases, a language-specific run-time exception is thrown.)
        /// </returns>
        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result) {
            if (String.Compare(binder.Name, "Add", StringComparison.Ordinal) == 0) {
                if (args.Length == 1) {
                    _members.Add(args[0]);
                    result = null;
                    return true;
                }
                result = null;
                return false;
            }
            else if (String.Compare(binder.Name, "Insert", StringComparison.Ordinal) == 0) {
                if (args.Length == 2) {
                    _members.Insert(Convert.ToInt32(args[0]), args[1]);
                    result = null;
                    return true;
                }
                result = null;
                return false;
            }
            else if (String.Compare(binder.Name, "IndexOf", StringComparison.Ordinal) == 0) {
                if (args.Length == 1) {
                    result = _members.IndexOf(args[0]);
                    return true;
                }
                result = null;
                return false;
            }
            else if (String.Compare(binder.Name, "Clear", StringComparison.Ordinal) == 0) {
                if (args.Length == 0) {
                    _members.Clear();
                    result = null;
                    return true;
                }
                result = null;
                return false;
            }
            else if (String.Compare(binder.Name, "Remove", StringComparison.Ordinal) == 0) {
                if (args.Length == 1) {
                    result = _members.Remove(args[0]);
                    return true;
                }
                result = null;
                return false;
            }
            else if (String.Compare(binder.Name, "RemoveAt", StringComparison.Ordinal) == 0) {
                if (args.Length == 1) {
                    _members.RemoveAt(Convert.ToInt32(args[0]));
                    result = null;
                    return true;
                }
                result = null;
                return false;
            }

            return base.TryInvokeMember(binder, args, out result);
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
                result = _members[Convert.ToInt32(indexes[0])];
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
            if (String.Compare("Length", binder.Name, StringComparison.Ordinal) == 0) {
                result = _members.Count;
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
                _members[Convert.ToInt32(indexes[0])] = value;
                return true;
            }

            return base.TrySetIndex(binder, indexes, value);
        }

        #region Implementation of IEnumerable
        IEnumerator IEnumerable.GetEnumerator() {
            return _members.GetEnumerator();
        }
        #endregion

        #region Implementation of IEnumerable<object>
        IEnumerator<object> IEnumerable<object>.GetEnumerator() {
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

        #region Implementation of ICollection<object>
        int ICollection<object>.Count {
            get {
                return _members.Count;
            }
        }

        bool ICollection<object>.IsReadOnly {
            get {
                return false;
            }
        }

        void ICollection<object>.Add(object item) {
            ((ICollection<object>)_members).Add(item);
        }

        void ICollection<object>.Clear() {
            ((ICollection<object>)_members).Clear();
        }

        bool ICollection<object>.Contains(object item) {
            return ((ICollection<object>)_members).Contains(item);
        }

        void ICollection<object>.CopyTo(object[] array, int arrayIndex) {
            ((ICollection<object>)_members).CopyTo(array, arrayIndex);
        }

        bool ICollection<object>.Remove(object item) {
            return ((ICollection<object>)_members).Remove(item);
        }
        #endregion
    }
}
