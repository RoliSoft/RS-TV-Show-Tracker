namespace RoliSoft.TVShowTracker.Scripting.ExternalTypes
{
    using System;
    using System.IO;

    using Microsoft.Scripting.Hosting;

    /// <summary>
    /// Represents a type exposed through the Microsoft® Dynamic Language Runtime™.
    /// </summary>
    public class DLRType : ExternalType
    {
        /// <summary>
        /// Gets or sets the script scope.
        /// </summary>
        /// <value>
        /// The script scope.
        /// </value>
        public ScriptScope Scope { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DLRType"/> class.
        /// </summary>
        /// <param name="file">The path to the file.</param>
        /// <param name="scope">The script scope.</param>
        /// <param name="handler">The handler plugin.</param>
        public DLRType(string file, ScriptScope scope, ScriptingPlugin handler)
        {
            File    = file;
            Scope   = scope;
            Handler = handler;
            Name    = Path.GetFileName(file).Replace(".Plugin.py", string.Empty);
            Type    = Scope.GetVariable<Type>(Name);
        }
    }
}
