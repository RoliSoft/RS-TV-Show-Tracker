namespace RoliSoft.TVShowTracker
{
    using System;

    /// <summary>
    /// Manages creating and running a type in a separate AppDomain.
    /// </summary>
    /// <typeparam name="T">The type to be isolated.</typeparam>
    public sealed class Isolated<T> : IDisposable where T : MarshalByRefObject
    {
        private AppDomain _domain;
        private T _instance;

        /// <summary>
        /// Initializes a new instance of the <see cref="Isolated{T}"/> class.
        /// </summary>
        public Isolated()
        {
            var type = typeof(T);

            _domain   = AppDomain.CreateDomain("Isolated:" + type.Name + "/" + Guid.NewGuid(), null, AppDomain.CurrentDomain.SetupInformation);
            _instance = (T)_domain.CreateInstanceAndUnwrap(type.Assembly.FullName, type.FullName);
        }

        /// <summary>
        /// Gets the isolated instance.
        /// </summary>
        /// <value>The instance.</value>
        public T Instance
        {
            get
            {
                return _instance;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (_domain != null)
            {
                AppDomain.Unload(_domain);

                _domain   = null;
                _instance = null;
            }
        }
    }
}
