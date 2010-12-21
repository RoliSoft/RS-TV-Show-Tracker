namespace RoliSoft.TVShowTracker
{
    using System;

    /// <summary>
    /// Generic <c>EventArgs</c> class to further simplify event creation.
    /// </summary>
    /// <typeparam name="T">Type of the data.</typeparam>
    public class EventArgs<T> : EventArgs
    {
        /// <summary>
        /// Gets or sets the data of type T.
        /// </summary>
        /// <value>The data.</value>
        public T Data { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventArgs&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="data">The data.</param>
        public EventArgs(T data)
        {
            Data = data;
        }
    }

    /// <summary>
    /// Generic <c>EventArgs</c> class accepting 2 arguments to further simplify event creation.
    /// </summary>
    /// <typeparam name="T1">The type of the first data.</typeparam>
    /// <typeparam name="T2">The type of the second data.</typeparam>
    public class EventArgs<T1, T2> : EventArgs
    {
        /// <summary>
        /// Gets or sets the first data of type T.
        /// </summary>
        /// <value>The data.</value>
        public T1 First { get; set; }

        /// <summary>
        /// Gets or sets the second data of type T.
        /// </summary>
        /// <value>The data.</value>
        public T2 Second { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventArgs&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="first">The first data.</param>
        /// <param name="second">The second data.</param>
        public EventArgs(T1 first, T2 second)
        {
            First  = first;
            Second = second;
        }
    }

    /// <summary>
    /// Generic <c>EventArgs</c> class accepting 3 arguments to further simplify event creation.
    /// </summary>
    /// <typeparam name="T1">The type of the first data.</typeparam>
    /// <typeparam name="T2">The type of the second data.</typeparam>
    /// <typeparam name="T3">The type of the third data.</typeparam>
    public class EventArgs<T1, T2, T3> : EventArgs
    {
        /// <summary>
        /// Gets or sets the first data of type T.
        /// </summary>
        /// <value>The data.</value>
        public T1 First { get; set; }

        /// <summary>
        /// Gets or sets the second data of type T.
        /// </summary>
        /// <value>The data.</value>
        public T2 Second { get; set; }

        /// <summary>
        /// Gets or sets the third data of type T.
        /// </summary>
        /// <value>The data.</value>
        public T3 Third { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventArgs&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="first">The first data.</param>
        /// <param name="second">The second data.</param>
        /// <param name="third">The third data.</param>
        public EventArgs(T1 first, T2 second, T3 third)
        {
            First  = first;
            Second = second;
            Third  = third;
        }
    }
}
