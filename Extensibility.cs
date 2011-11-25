namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;

    using Scripting;

    using Microsoft.Scripting;
    using Microsoft.WindowsAPICodePack.Dialogs;

    /// <summary>
    /// Provides support for loading external classes into the application.
    /// </summary>
    public static class Extensibility
    {
        /// <summary>
        /// Gets or sets a list of internal plugin types.
        /// </summary>
        /// <value>
        /// The list of internal plugin types.
        /// </value>
        public static List<Type> InternalPlugins { get; set; }

        /// <summary>
        /// Gets or sets a list of external plugin types.
        /// </summary>
        /// <value>
        /// The list of external plugin types.
        /// </value>
        public static List<Type> ExternalPlugins { get; set; }

        /// <summary>
        /// Gets or sets a list of compiled and loaded scripts.
        /// </summary>
        /// <value>
        /// The list of compiled and loaded scripts.
        /// </value>
        public static List<ExternalType> Scripts { get; set; }

        /// <summary>
        /// Gets or sets a dictionary where file extensions are mapped to their handler plugins.
        /// </summary>
        /// <value>
        /// The dictionary where file extensions are mapped to their handler plugins.
        /// </value>
        public static Dictionary<string, ScriptingPlugin> Handlers { get; set; }

        /// <summary>
        /// Initializes the <see cref="Extensibility"/> class.
        /// </summary>
        static Extensibility()
        {
            // load internal classes

            InternalPlugins = new List<Type>(GetDerivedTypesFromAssembly<IPlugin>(Assembly.GetExecutingAssembly()));

            // load external plugins

            ExternalPlugins = new List<Type>();
            Scripts         = new List<ExternalType>();

            var plugins = Directory.GetFiles(Signature.FullPath, "*.Plugin.*");

            foreach (var file in plugins.Where(f => f.EndsWith(".dll")))
            {
                try
                {
                    var asm = Assembly.LoadFile(file);
                    var lst = GetDerivedTypesFromAssembly<IPlugin>(asm);

                    ExternalPlugins.AddRange(lst);
                }
                catch (Exception ex)
                {
                    HandleLoadException(file, ex);
                }
            }

            // load script handlers

            Handlers = new Dictionary<string, ScriptingPlugin>();

            foreach (var splugin in GetNewInstances<ScriptingPlugin>(inclScripts: false))
            {
                Handlers.Add(splugin.Extension, splugin);
            }

            // load external scripts

            foreach (var file in plugins.Where(f => !f.EndsWith(".dll")))
            {
                ScriptingPlugin handler;

                if (!Handlers.TryGetValue(Path.GetExtension(file), out handler))
                {
                    continue;
                }

                List<ExternalType> types = null;

                try
                {
                    types = handler.LoadScript(file);
                }
                catch (Exception ex)
                {
                    HandleLoadException(file, ex);
                }

                if (types != null && types.Count != 0)
                {
                    Scripts.AddRange(types);
                }
            }

            // resolve conflicts

            var iremove = new List<Type>();
            var eremove = new List<Type>();
            var sremove = new List<ExternalType>();

            foreach (var iplugin in InternalPlugins)
            {
                foreach (var eplugin in ExternalPlugins)
                {
                    if (iplugin.Name == eplugin.Name)
                    {
                        var iinst = (IPlugin)Activator.CreateInstance(iplugin);
                        var einst = (IPlugin)Activator.CreateInstance(eplugin);

                        if (iinst.Version >= einst.Version)
                        {
                            eremove.Add(eplugin);
                        }
                        else
                        {
                            iremove.Add(iplugin);
                        }
                    }
                }

                foreach (var splugin in Scripts)
                {
                    if (iplugin.Name == splugin.Name)
                    {
                        var iinst = (IPlugin)Activator.CreateInstance(iplugin);
                        var sinst = splugin.Handler.CreateInstance<IPlugin>(splugin);

                        if (iinst.Version >= sinst.Version)
                        {
                            sremove.Add(splugin);
                        }
                        else
                        {
                            iremove.Add(iplugin);
                        }
                    }
                }
            }

            foreach (var splugin in Scripts)
            {
                foreach (var eplugin in ExternalPlugins)
                {
                    if (splugin.Name == eplugin.Name)
                    {
                        var einst = (IPlugin)Activator.CreateInstance(eplugin);
                        var sinst = splugin.Handler.CreateInstance<IPlugin>(splugin);

                        if (einst.Version >= sinst.Version)
                        {
                            sremove.Add(splugin);
                        }
                        else
                        {
                            eremove.Add(eplugin);
                        }
                    }
                }
            }

            // remove conflicts

            foreach (var iplugin in iremove)
            {
                InternalPlugins.Remove(iplugin);
            }

            foreach (var eplugin in eremove)
            {
                ExternalPlugins.Remove(eplugin);
            }

            foreach (var splugin in sremove)
            {
                Scripts.Remove(splugin);
            }
        }

        /// <summary>
        /// Gets the derived types from the specified assembly for the specified type.
        /// </summary>
        /// <typeparam name="T">The class whose derived types are needed.</typeparam>
        /// <param name="assembly">The assembly to search.</param>
        /// <returns>
        /// List of derived classes.
        /// </returns>
        public static IEnumerable<Type> GetDerivedTypesFromAssembly<T>(Assembly assembly)
        {
            var parent = typeof(T);

            foreach (var type in assembly.GetTypes())
            {
                if (type.IsClass && !type.IsAbstract && parent.IsAssignableFrom(type))
                {
                    yield return type;
                }
            }
        }

        /// <summary>
        /// Gets the derived types from both internal and external sources for the specified type.
        /// </summary>
        /// <typeparam name="T">The class whose derived types are needed.</typeparam>
        /// <param name="inclInternal">if set to <c>true</c> the internal assembly will be included in the search.</param>
        /// <param name="inclExternal">if set to <c>true</c> the external assemblies will be included in the search.</param>
        /// <returns>
        /// List of derived classes.
        /// </returns>
        public static IEnumerable<Type> GetDerivedTypes<T>(bool inclInternal = true, bool inclExternal = true)
        {
            var parent = typeof(T);

            if (inclInternal)
            {
                foreach (var type in InternalPlugins)
                {
                    if (parent.IsAssignableFrom(type))
                    {
                        yield return type;
                    }
                }
            }

            if (inclExternal)
            {
                foreach (var type in ExternalPlugins)
                {
                    if (parent.IsAssignableFrom(type))
                    {
                        yield return type;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the derived types from both internal and external sources for the specified type and create a new instance for each.
        /// </summary>
        /// <typeparam name="T">The class whose derived types are needed.</typeparam>
        /// <param name="inclInternal">if set to <c>true</c> the internal assembly will be included in the search.</param>
        /// <param name="inclExternal">if set to <c>true</c> the external assemblies will be included in the search.</param>
        /// <param name="inclScripts">if set to <c>true</c> the compiled scripts will be included in the search.</param>
        /// <returns>
        /// List of derived instantiated classes.
        /// </returns>
        public static IEnumerable<T> GetNewInstances<T>(bool inclInternal = true, bool inclExternal = true, bool inclScripts = true)
        {
            foreach (var type in GetDerivedTypes<T>(inclInternal, inclExternal))
            {
                yield return (T)Activator.CreateInstance(type);
            }

            if (inclScripts)
            {
                var parent = typeof(T);

                foreach (var script in Scripts)
                {
                    if (script.Handler.IsCompatible(script, parent))
                    {
                        yield return script.Handler.CreateInstance<T>(script);
                    }
                }
            }
        }

        /// <summary>
        /// Handles the exception which occurred while loading a plugin.
        /// </summary>
        /// <param name="file">The plugin file.</param>
        /// <param name="ex">The thrown exception.</param>
        private static void HandleLoadException(string file, Exception ex)
        {
            if (ex is SyntaxErrorException)
            {
                new TaskDialog
                    {
                        Icon            = TaskDialogStandardIcon.Error,
                        Caption         = "Failed to parse plugin",
                        InstructionText = "Failed to parse plugin",
                        Text            = "Syntax error in {0} line {1} column {2}:\r\n\r\n{3}".FormatWith(Path.GetFileName(file), ((SyntaxErrorException)ex).Line, ((SyntaxErrorException)ex).Column, ex.Message.ToUppercaseFirst()),
                    }.Show();
            }
            else
            {
                var sb = new StringBuilder();

            parseException:
                sb.AppendLine(ex.GetType() + ": " + ex.Message);
                sb.AppendLine(ex.StackTrace);

                if (ex.InnerException != null)
                {
                    ex = ex.InnerException;
                    goto parseException;
                }

                new TaskDialog
                    {
                        Icon                  = TaskDialogStandardIcon.Error,
                        Caption               = "Failed to load plugin",
                        InstructionText       = "Failed to load plugin",
                        Text                  = "An exception of type {0} was thrown while trying to load plugin {1}.".FormatWith(ex.GetType().ToString().Replace("System.", string.Empty), Path.GetFileName(file)),
                        DetailsExpandedText   = sb.ToString(),
                        DetailsExpandedLabel  = "Hide stacktrace",
                        DetailsCollapsedLabel = "Show stacktrace"
                    }.Show();
            }
        }
    }
}