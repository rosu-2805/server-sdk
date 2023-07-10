using System;
using System.Threading;
using System.Threading.Tasks;

namespace Morph.Server.Sdk.Helper
{
    /// <summary>
    ///   A factory that returns the value once and then returns default(T) on subsequent invocations.
    /// </summary>
    /// <typeparam name="T">The type of the value to return.</typeparam>
    internal class OnceFactory<T>
    {
        private readonly T _value;
        private Func<CancellationToken, Task<T>> _factory;

        public static Func<CancellationToken, Task<T>> Create(T value) => new OnceFactory<T>(value).Dispatch;

        private OnceFactory(T value)
        {
            _value = value;
            _factory = DoReturnValue;
        }

        private async Task<T> Dispatch(CancellationToken token) => await _factory(token).ConfigureAwait(false);

        private Task<T> DoReturnValue(CancellationToken arg)
        {
            _factory = DoReturnNothing;
            return Task.FromResult(_value);
        }

        private static Task<T> DoReturnNothing(CancellationToken arg) => Task.FromResult<T>(default);
    }
}