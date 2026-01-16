using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;

namespace Postica.Common
{
    /// <summary>
    /// This class allows to queue callbacks on Unity's main thread.
    /// </summary>
    public static class UnityThread
    {
        private static readonly object _lock = new object();

        private static readonly Queue<ActionWrapper>[] _queues = new Queue<ActionWrapper>[] { new Queue<ActionWrapper>(), new Queue<ActionWrapper>() };
        private static readonly bool[] _readyArray = new bool[1024];
        private static int _queueIndex;
        private static int _actionsCount;
        private static int _nextReadyIndex;
        private static UnityThreadMono _monoThread;
        private static int _waitDelayInMs;

        private static SynchronizationContext _context;

        /// <summary>
        /// Gets the synchronization context of the Unity's main thread.
        /// </summary>
        public static SynchronizationContext Context => _context;

        private struct ActionWrapper
        {
            public Action action;
            public int readyIndex;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        internal static void Initialize()
        {
            _context = SynchronizationContext.Current;
            _waitDelayInMs = (int)(Time.fixedDeltaTime * 1000);
        }

        internal static void SpinOnce()
        {
            if(_actionsCount == 0) { return; }

            Queue<ActionWrapper> queue = null;
            lock (_lock)
            {
                queue = _queues[_queueIndex];
                _queueIndex = (_queueIndex + 1) % 2;
                _actionsCount = 0;
            }

            foreach (var action in queue)
            {
                try
                {
                    action.action();
                    if (action.readyIndex >= 0)
                    {
                        _readyArray[action.readyIndex] = true;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        /// <summary>
        /// Run the specified <paramref name="action"/> on the Unity's main thread asynchronously.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <exception cref="ArgumentNullException">If the <paramref name="action"/> is null.</exception>
        public static void Run(Action action)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (SynchronizationContext.Current == _context)
            {
                action();
            }
            else
            {
                _context?.Post(v => action(), null);
            }
        }

        /// <summary>
        /// Runs the specified <paramref name="action"/> on the Unity's main thread and blocks the current thread until the <paramref name="action"/> is executed.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <exception cref="ArgumentNullException">If the <paramref name="action"/> is null.</exception>
        internal static void RunAndWait(Action action)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (SynchronizationContext.Current == _context)
            {
                action();
            }
            else
            {
                RuntimeValidation();

                var readyIndex = -1;
                lock (_lock)
                {
                    readyIndex = _nextReadyIndex;
                    _nextReadyIndex = (_nextReadyIndex + 1) % _readyArray.Length;
                    _queues[_queueIndex].Enqueue(new ActionWrapper() { action = action, readyIndex = readyIndex });
                    _actionsCount++;
                    _readyArray[readyIndex] = false;
                }

                if(readyIndex < 0)
                {
                    return;
                }

                while (!_readyArray[readyIndex])
                {
                    Thread.Sleep(_waitDelayInMs);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void RuntimeValidation()
        {
            if (!Application.isEditor && !_monoThread)
            {
                _monoThread = new GameObject("__UNITY_THREAD_CONTROLLER__").AddComponent<UnityThreadMono>();
                _monoThread.hideFlags = HideFlags.HideAndDontSave;
                UnityEngine.Object.DontDestroyOnLoad(_monoThread);
            }
        }
    }
}