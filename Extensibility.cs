namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;

    using IronPython.Hosting;

    using Microsoft.Scripting.Hosting;
    using Microsoft.WindowsAPICodePack.Dialogs;

    /// <summary>
    /// Provides support for loading external classes into the application.
    /// </summary>
    public static class Extensibility
    {
        /// <summary>
        /// Gets or sets a list of loaded plugin assemblies.
        /// </summary>
        /// <value>
        /// The list of loaded plugin assemblies
        /// </value>
        public static List<Assembly> Plugins { get; set; }

        /// <summary>
        /// Gets or sets a list of compiled and loaded scripts.
        /// </summary>
        /// <value>
        /// The list of compiled and loaded scripts.
        /// </value>
        public static List<Script> Scripts { get; set; } 

        /// <summary>
        /// Initializes the <see cref="Extensibility"/> class.
        /// </summary>
        static Extensibility()
        {
            Plugins = new List<Assembly>();

            foreach (var file in Directory.EnumerateFiles(Signature.FullPath, "*.Plugin.dll"))
            {
                try
                {
                    Plugins.Add(Assembly.LoadFile(file));
                }
                catch (Exception ex)
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
                            Text                  = "An exception of type {0} was thrown while trying to load plugin {1}.".FormatWith(ex.GetType().ToString().Replace("System.", string.Empty), new FileInfo(file).Name),
                            DetailsExpandedText   = sb.ToString(),
                            DetailsExpandedLabel  = "Hide stacktrace",
                            DetailsCollapsedLabel = "Show stacktrace"
                        }.Show();
                }
            }

            Scripts = new List<Script>();

            foreach (var file in Directory.EnumerateFiles(Signature.FullPath, "*.Plugin.py"))
            {
                try
                {
                    var engine = Python.CreateEngine();
                    engine.SetSearchPaths(new[] { Signature.FullPath });

                    var source = engine.CreateScriptSourceFromFile(file);
                    var scope  = engine.CreateScope();

                    source.Execute(scope);

                    Scripts.Add(new Script(file, scope));
                }
                catch (Exception ex)
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
                            Text                  = "An exception of type {0} was thrown while trying to load plugin {1}.".FormatWith(ex.GetType().ToString().Replace("System.", string.Empty), new FileInfo(file).Name),
                            DetailsExpandedText   = sb.ToString(),
                            DetailsExpandedLabel  = "Hide stacktrace",
                            DetailsCollapsedLabel = "Show stacktrace"
                        }.Show();
                }
            }
        }

        /// <summary>
        /// Gets the derived types from the specified assembly for the specified type.
        /// </summary>
        /// <param name="assembly">The assembly to search.</param>
        /// <param name="baseClass">The class whose derived types are needed.</param>
        /// <param name="inclAbstract">if set to <c>true</c> abstract classes will not be filtered.</param>
        /// <returns>
        /// List of derived classes.
        /// </returns>
        public static IEnumerable<Type> GetDerivedTypesFromAssembly(Assembly assembly, Type baseClass, bool inclAbstract = false)
        {
            return assembly.GetTypes().Where(type => type.IsClass && type.IsSubclassOf(baseClass) && (inclAbstract || !type.IsAbstract));
        }

        /// <summary>
        /// Gets the derived types from both internal and external sources for the specified type.
        /// </summary>
        /// <typeparam name="T">The class whose derived types are needed.</typeparam>
        /// <param name="inclAbstract">if set to <c>true</c> abstract classes will not be filtered.</param>
        /// <param name="inclInternal">if set to <c>true</c> the internal assembly will be included in the search.</param>
        /// <param name="inclExternal">if set to <c>true</c> the external assemblies will be included in the search.</param>
        /// <returns>
        /// List of derived classes.
        /// </returns>
        public static IEnumerable<Type> GetDerivedTypes<T>(bool inclAbstract = false, bool inclInternal = true, bool inclExternal = true)
        {
            if (inclInternal)
            {
                foreach (var type in GetDerivedTypesFromAssembly(Assembly.GetExecutingAssembly(), typeof(T), inclAbstract))
                {
                    yield return type;
                }
            }

            if (inclExternal)
            {
                foreach (var asm in Plugins)
                {
                    foreach (var type in GetDerivedTypesFromAssembly(asm, typeof(T), inclAbstract))
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
        /// <param name="inclAbstract">if set to <c>true</c> abstract classes will not be filtered.</param>
        /// <param name="inclInternal">if set to <c>true</c> the internal assembly will be included in the search.</param>
        /// <param name="inclExternal">if set to <c>true</c> the external assemblies will be included in the search.</param>
        /// <param name="inclScripts">if set to <c>true</c> the compiled scripts will be included in the search.</param>
        /// <returns>
        /// List of derived instantiated classes.
        /// </returns>
        public static IEnumerable<T> GetNewInstances<T>(bool inclAbstract = false, bool inclInternal = true, bool inclExternal = true, bool inclScripts = true)
        {
            foreach (var type in GetDerivedTypes<T>(inclAbstract, inclInternal, inclExternal))
            {
                yield return (T)Activator.CreateInstance(type);
            }

            if (inclScripts)
            {
                foreach (var script in Scripts)
                {
                    if (script.Type.IsSubclassOf(typeof(T)) && (inclAbstract || !script.Type.IsAbstract))
                    {
                        yield return (T)script.Scope.Engine.Operations.CreateInstance(script.Scope.GetVariable(script.Name));
                    }
                }
            }
        }

        /// <summary>
        /// Represents a Python plugin script.
        /// </summary>
        public class Script
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
            /// Gets or sets the CLR type of the class.
            /// </summary>
            /// <value>
            /// The CLR type of the class.
            /// </value>
            public Type Type { get; set; }

            /// <summary>
            /// Gets or sets the script scope.
            /// </summary>
            /// <value>
            /// The script scope.
            /// </value>
            public ScriptScope Scope { get; set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="Script"/> class.
            /// </summary>
            /// <param name="file">The path to the file.</param>
            /// <param name="scope">The script scope.</param>
            public Script(string file, ScriptScope scope)
            {
                File  = file;
                Scope = scope;
                Name  = new FileInfo(file).Name.Replace(".Plugin.py", string.Empty);
                Type  = Scope.GetVariable<Type>(Name);
            }
        }
    }
}