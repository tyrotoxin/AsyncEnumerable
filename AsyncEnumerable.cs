using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace System.Collections.Async
{
    /// <summary>
    /// Helps to enumerate items in a collection asynchronously
    /// </summary>
    /// <example>
    /// <code>
    /// IAsyncEnumerable&lt;int&gt; ProduceNumbers(int start, int end)
    /// {
    ///   return new AsyncEnumerable&lt;int&gt;(async yield => {
    ///     for (int number = start; number &lt;= end; number++)
    ///       await yield.ReturnAsync(number);
    ///   });
    /// }
    /// 
    /// async Task ConsumeAsync()
    /// {
    ///   var asyncEnumerableCollection = ProduceNumbers(start: 1, end: 10);
    ///   await asyncEnumerableCollection.ForEachAsync(async number => {
    ///     await Console.Out.WriteLineAsync(number)
    ///   });
    /// }
    /// 
    /// // It's backward compatible with synchronous enumeration, but gives no benefits
    /// void ConsumeSync()
    /// {
    ///   var enumerableCollection = ProduceNumbers(start: 1, end: 10);
    ///   foreach (var number in enumerableCollection) {
    ///     Console.Out.WriteLine(number)
    ///   };
    /// }
    /// </code>
    /// </example>
    public sealed class AsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        private Func<AsyncEnumerator<T>.Yield, Task> _enumerationFunction;
        private bool _oneTimeUse;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="enumerationFunction">A function that enumerates items in a collection asynchronously</param>
        public AsyncEnumerable(Func<AsyncEnumerator<T>.Yield, Task> enumerationFunction)
            : this(enumerationFunction, oneTimeUse: false)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="enumerationFunction">A function that enumerates items in a collection asynchronously</param>
        /// <param name="oneTimeUse">When True the enumeration can be performed once only and Reset method is not allowed</param>
        public AsyncEnumerable(Func<AsyncEnumerator<T>.Yield, Task> enumerationFunction, bool oneTimeUse)
        {
            _enumerationFunction = enumerationFunction;
            _oneTimeUse = oneTimeUse;
        }

        /// <summary>
        /// Creates an enumerator that iterates through a collection asynchronously
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel creation of the enumerator in case if it takes a lot of time</param>
        /// <returns>Returns a task with the created enumerator as result on completion</returns>
        public Task<IAsyncEnumerator<T>> GetAsyncEnumeratorAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var enumerator = new AsyncEnumerator<T>(_enumerationFunction, _oneTimeUse);
            return Task.FromResult<IAsyncEnumerator<T>>(enumerator);
        }

        /// <summary>
        /// Creates an enumerator that iterates through a collection asynchronously
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel creation of the enumerator in case if it takes a lot of time</param>
        /// <returns>Returns a task with the created enumerator as result on completion</returns>
        Task<IAsyncEnumerator> IAsyncEnumerable.GetAsyncEnumeratorAsync(CancellationToken cancellationToken) => GetAsyncEnumeratorAsync(cancellationToken).ContinueWith<IAsyncEnumerator>(task => task.Result);

        /// <summary>
        /// Returns an enumerator that iterates through the collection
        /// </summary>
        /// <returns>An instance of enumerator</returns>
        public IEnumerator<T> GetEnumerator() => GetAsyncEnumeratorAsync().ConfigureAwait(false).GetAwaiter().GetResult();

        /// <summary>
        /// Returns an enumerator that iterates through the collection
        /// </summary>
        /// <returns>An instance of enumerator</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetAsyncEnumeratorAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Static convenience methods for AsyncEnumerable.
    /// </summary>
    public static class AsyncEnumerable
    {
        /// <summary>
        /// Convenience extension to AsyncEnumerable to provide an IAsyncEnumerable implementation of a blocking IEnumerable.
        /// Useful for satisifying async interfaces with blocking implementations.
        /// </summary>
        public static IAsyncEnumerable<T> FromBlocking<T>(IEnumerable<T> enumerable)
        {
            return new AsyncEnumerable<T>(async yield =>
            {
                foreach (var item in enumerable)
                    await yield.ReturnAsync(item);
            });
        }

        /// <summary>
        /// Convenience extension to AsyncEnumerable to provide an empty enumerable.
        /// </summary>
        public static IAsyncEnumerable<T> Empty<T>()
        {
            return FromBlocking(Enumerable.Empty<T>());
        }
    }
}
