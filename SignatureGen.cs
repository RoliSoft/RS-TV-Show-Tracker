using System;
using System.Reflection;

[assembly: AssemblyVersion("2.2.0.568")]

namespace RoliSoft.TVShowTracker
{
    /// <summary>
    /// Contains informations about the assembly.
    /// </summary>
    public static partial class Signature
    {
        /// <summary>
        /// Gets the date and time when the executing assembly was compiled.
        /// </summary>
        /// <value>The compile time.</value>
        public static DateTime CompileTime
        {
            get { return DateTime.FromBinary(-8588428805649793754); }
        }

        /// <summary>
        /// Gets the git commit hash.
        /// </summary>
        /// <value>The git commit hash.</value>
        public static string GitRevision
        {
            get { return "52d63eb3b42f9314b15b55f7eca7ba83874c4043"; }
        }

        /// <summary>
        /// Gets the directory in which the project was built.
        /// </summary>
        /// <value>The directory in which the project was built.</value>
        public static string BuildDirectory
        {
            get { return @"C:\Users\RoliSoft\Documents\Visual Studio 2012\Projects\RS TV Show Tracker\RS TV Show Tracker"; }
        }
    }
}
