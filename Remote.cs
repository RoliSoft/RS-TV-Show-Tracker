namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Diagnostics;
    using System.Dynamic;
    using System.Linq;

    using Newtonsoft.Json;

    /// <summary>
    /// Provides a fun way to communicate with lab.rolisoft.net/api by harnessing the power of dynamic.
    /// </summary>
    public class REST : DynamicObject
    {
        private static readonly REST _instance = new REST();

        private REST() { }

        /// <summary>
        /// Gets the method proxy to lab.rolisoft.net/api
        /// </summary>
        /// <value>The instance.</value>
        public static dynamic Instance
        {
            get
            {
                return _instance;
            }
        }

        /// <summary>
        /// Provides the implementation for operations that invoke a member. Classes derived from the <see cref="T:System.Dynamic.DynamicObject"/> class can override this method to specify dynamic behavior for operations such as calling a method.
        /// </summary>
        /// <param name="binder">Provides information about the dynamic operation. The binder.Name property provides the name of the member on which the dynamic operation is performed. For example, for the statement sampleObject.SampleMethod(100), where sampleObject is an instance of the class derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, binder.Name returns "SampleMethod". The binder.IgnoreCase property specifies whether the member name is case-sensitive.</param>
        /// <param name="args">The arguments that are passed to the object member during the invoke operation. For example, for the statement sampleObject.SampleMethod(100), where sampleObject is derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, <paramref name="args"/>[0] is equal to 100.</param>
        /// <param name="result">The result of the member invocation.</param>
        /// <returns>
        /// true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of the language determines the behavior. (In most cases, a language-specific run-time exception is thrown.)
        /// </returns>
        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            dynamic obj;
            var sw = Stopwatch.StartNew();

            try
            {
                var r = Utils.GetURL("http://lab.rolisoft.net/api/",
                    "json"
                  + "&software=" + Uri.EscapeUriString("RS TV Show Tracker")
                  + "&version=" + Uri.EscapeUriString(Signature.Version)
                  + "&uuid=" + Uri.EscapeUriString(Utils.GetUUID())
                  + "&func=" + Uri.EscapeUriString(binder.Name)
                  + (args.Count() != 0
                     ? "&args[]=" + string.Join("&args[]=", args.Select(arg => Uri.EscapeUriString(arg.ToString())))
                     : string.Empty)
                );

                obj = JsonConvert.DeserializeObject(r);
                obj.Success = obj.Error == null;
            }
            catch (Exception ex)
            {
                obj = new ExpandoObject();
                obj.Success = false;
                obj.Error = ex.Message;
            }

            sw.Stop();
            obj.Time = sw.Elapsed.TotalSeconds;
            result = obj;
            return true;
        }
    }
}
