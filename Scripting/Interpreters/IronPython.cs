namespace RoliSoft.TVShowTracker.Scripting.Interpreters
{
    using System;
    using System.Collections.Generic;

    using ExternalTypes;

    using global::IronPython.Hosting;

    /// <summary>
    /// Provides support for running Python scripts through IronPython.
    /// </summary>
    public class IronPython : ScriptingPlugin
    {
        /// <summary>
        /// Gets or sets the name of the plugin.
        /// </summary>
        /// <value>
        /// The name of the plugin.
        /// </value>
        public override string Name
        {
            get
            {
                return "IronPython";
            }
        }

        /// <summary>
        /// Gets the URL to the favicon of the site.
        /// </summary>
        /// <value>The icon location.</value>
        public override string Icon
        {
            get
            {
                return "pack://application:,,,/RSTVShowTracker;component/Images/python.png";
            }
        }

        /// <summary>
        /// Gets the name of the plugin's developer.
        /// </summary>
        /// <value>The name of the plugin's developer.</value>
        public override string Developer
        {
            get
            {
                return "RoliSoft";
            }
        }

        /// <summary>
        /// Gets the version number of the plugin.
        /// </summary>
        /// <value>The version number of the plugin.</value>
        public override Version Version
        {
            get
            {
                return Utils.DateTimeToVersion("2011-11-24 7:34 PM");
            }
        }

        /// <summary>
        /// Gets the extension of the files handled by this plugin.
        /// </summary>
        /// <value>
        /// The extension of the files handled by this plugin.
        /// </value>
        public override string Extension
        {
            get
            {
                return ".py";
            }
        }

        /// <summary>
        /// Loads the specified script and extracts the exposed types.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <returns>
        /// A list of types.
        /// </returns>
        public override List<ExternalType> LoadScript(string file)
        {
            var engine = Python.CreateEngine();
            engine.SetSearchPaths(new[] { Signature.FullPath });

            var source = engine.CreateScriptSourceFromFile(file);
            var scope  = engine.CreateScope();

            source.Execute(scope);

            return new List<ExternalType>
                {
                    new DLRType(file, scope, this)
                };
        }

        /// <summary>
        /// Determines whether the exposed type by the specified script derives from the specified type.
        /// </summary>
        /// <param name="externalType">The type exposed by the script.</param>
        /// <param name="internalType">The internal abstract type which the external type derives from.</param>
        /// <returns>
        ///   <c>true</c> if it is a subclass of the specified type; otherwise, <c>false</c>.
        /// </returns>
        public override bool IsCompatible(ExternalType externalType, Type internalType)
        {
            return !externalType.Type.IsAbstract && internalType.IsAssignableFrom(externalType.Type);
        }

        /// <summary>
        /// Creates a new instance from the specified type.
        /// </summary>
        /// <typeparam name="T">The internal abstract type to cast the new instance to.</typeparam>
        /// <param name="type">The type exposed by the script.</param>
        /// <returns>
        /// A new instance from the specified type.
        /// </returns>
        public override T CreateInstance<T>(ExternalType type)
        {
            var scope = ((DLRType)type).Scope;
            var impl  = scope.GetVariable(type.Name);

            return (T)scope.Engine.Operations.CreateInstance(impl);
        }
    }
}
