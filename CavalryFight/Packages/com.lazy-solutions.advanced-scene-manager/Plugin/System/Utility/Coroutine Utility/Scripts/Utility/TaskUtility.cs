using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;

namespace AdvancedSceneManager.Utility
{

    /// <summary>Provides utility methods for working with tasks.</summary>
    public static class TaskUtility
    {

        /// <summary>Runs a coroutine as a <see cref="Task"/>.</summary>
        /// <param name="coroutine">The coroutine to run.</param>
        /// <returns>A task that completes when the coroutine finishes.</returns>
        public static Task StartCoroutineAsTask(this IEnumerator coroutine)
        {
            TaskCompletionSource<bool> taskCompletionSource = new();
            RunCoroutine(coroutine, taskCompletionSource).StartCoroutine();
            return taskCompletionSource.Task;
        }

        private static IEnumerator RunCoroutine(IEnumerator coroutine, TaskCompletionSource<bool> tcs)
        {
            yield return coroutine;
            tcs.SetResult(true);
        }

        /// <summary>Runs a coroutine as an <see cref="Awaitable{TResult}"/>.</summary>
        /// <param name="coroutine">The coroutine to run.</param>
        /// <returns>An awaitable that completes when the coroutine finishes.</returns>
        public static Awaitable<bool> StartCoroutineAsAwaitable(this IEnumerator coroutine)
        {
            AwaitableCompletionSource<bool> awaitableCompletionSource = new();
            RunCoroutine(coroutine, awaitableCompletionSource).StartCoroutine();
            return awaitableCompletionSource.Awaitable;
        }

        private static IEnumerator RunCoroutine(IEnumerator coroutine, AwaitableCompletionSource<bool> acs)
        {
            yield return coroutine;
            acs.SetResult(true);
        }

        /// <summary>Gets an awaiter that allows awaiting the coroutine.</summary>
        /// <param name="coroutine">The coroutine to await.</param>
        /// <returns>A <see cref="CoroutineAwaiter"/> for the coroutine.</returns>
        public static CoroutineAwaiter GetAwaiter(this IEnumerator coroutine)
        {
            return new CoroutineAwaiter(coroutine);
        }

    }

    /// <summary>Provides an awaiter for coroutines, allowing them to be awaited like tasks.</summary>
    /// <remarks>See also <see cref="TaskUtility.GetAwaiter(IEnumerator)"/>.</remarks>
    public class CoroutineAwaiter : INotifyCompletion
    {

        private bool isCompleted;
        private Action continuation;

        /// <summary>Initializes a new instance of the <see cref="CoroutineAwaiter"/> class.</summary>
        /// <param name="coroutine">The coroutine to await.</param>
        public CoroutineAwaiter(IEnumerator coroutine)
        {
            RunCoroutine(coroutine);
        }

        private async void RunCoroutine(IEnumerator coroutine)
        {
            await coroutine.StartCoroutineAsTask();
            isCompleted = true;
            continuation?.Invoke();
        }

        /// <summary>Gets whether the coroutine has completed.</summary>
        public bool IsCompleted => isCompleted;

        /// <summary>Retrieves the result of the coroutine.</summary>
        public void GetResult() { }

        /// <summary>Registers a continuation to be invoked when the coroutine completes.</summary>
        /// <param name="continuation">The continuation to invoke.</param>
        public void OnCompleted(Action continuation)
        {
            this.continuation = continuation;
        }
    }

}
