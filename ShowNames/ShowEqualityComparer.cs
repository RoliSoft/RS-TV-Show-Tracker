namespace RoliSoft.TVShowTracker.ShowNames
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Provides support for comparing shows for Linq functions.
    /// </summary>
    public class ShowEqualityComparer : IEqualityComparer<string>
    {
        /// <summary>
        /// Determines whether the specified objects are equal.
        /// </summary>
        /// <returns>
        /// true if the specified objects are equal; otherwise, false.
        /// </returns>
        public bool Equals(string x, string y)
        {
            return Tools.GetRoot(x).SequenceEqual(Tools.GetRoot(y));
        }

        /// <summary>
        /// Returns a hash code for the specified object.
        /// </summary>
        /// <returns>
        /// A hash code for the specified object.
        /// </returns>
        /// <param name="obj">The <see cref="T:System.Object"/> for which a hash code is to be returned.</param>
        /// <exception cref="T:System.ArgumentNullException">The type of <paramref name="obj"/> is a reference type and <paramref name="obj"/> is null.</exception>
        public int GetHashCode(string obj)
        {
            return Tools.GetRoot(obj).Sum(word => word.GetHashCode());
        }
    }
}
