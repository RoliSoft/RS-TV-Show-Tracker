namespace RoliSoft.TVShowTracker.Scripting
{
    using System;

    /// <summary>
    /// Represents a derived type in an external script.
    /// </summary>
    public abstract class ExternalType
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the full path to the file.
        /// </summary>
        /// <value>
        /// The full path to the file.
        /// </value>
        public string File { get; set; }

        /// <summary>
        /// Gets or sets the handler plugin.
        /// </summary>
        /// <value>
        /// The handler plugin.
        /// </value>
        public ScriptingPlugin Handler { get; set; }

        /// <summary>
        /// Gets or sets the CLR type of the class.
        /// </summary>
        /// <value>
        /// The CLR type of the class.
        /// </value>
        /// <remarks>
        /// The handler is not required to provide this, but in most of the cases
        /// it is available and used internally.
        /// </remarks>
        public Type Type { get; set; }

    }
}
