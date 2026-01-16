using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Postica.BindingSystem
{

    class BindTask : IDisposable
    {
        private readonly object _lock = new object();

        private readonly Queue<(Action action, CancellationToken token)> _executionQueue = new Queue<(Action action, CancellationToken token)>();
        private readonly CancellationToken _token;
        private readonly CancellationTokenSource _tokenSource;
        private readonly int _progressId;

        private int _actionsExecuted;
        private int _totalActions;
        private bool _running;
        private bool _newActionsAvailable;
        private bool _completeRequested;

        public BindTask(string name, string description, CancellationToken cancellationToken = default)
            : this(name, description, -1, cancellationToken)
        {
            
        }

        public BindTask(string name, string description, int groupId, CancellationToken cancellationToken = default)
        {
            _tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _token = cancellationToken;
            _progressId = Progress.Start(name, description, Progress.Options.Indefinite, groupId);
            Progress.RegisterCancelCallback(_progressId, () => _token.IsCancellationRequested);
            _completeRequested = false;
        }

        public BindTask Append(Action action, CancellationToken cancellationToken = default)
        {
            if (!_running)
            {
                _newActionsAvailable = true;
                _executionQueue.Enqueue((action, cancellationToken));
                Task.Run(TaskRun);
                return this;
            }
            bool upgradeToDeterministic = false;
            lock(_lock)
            {
                _newActionsAvailable = true;
                _executionQueue.Enqueue((action, cancellationToken));
                upgradeToDeterministic = _executionQueue.Count >= 2;
                _totalActions++;
            }
            if (upgradeToDeterministic)
            {
                Progress.Report(_progressId, _actionsExecuted, _totalActions);
            }
            return this;
        }

        public void Dispose()
        {
            Stop();
        }

        public void Stop()
        {
            _tokenSource?.Cancel();
            _tokenSource?.Dispose();
            _running = false;
            Progress.Remove(_progressId);
        }

        public async Task<BindTask> Await()
        {
            while (_running)
            {
                _running = false;
                await Task.Yield();
            }
            _executionQueue.Clear();
            return this;
        }

        public BindTask DisposeOnCompletion()
        {
            _completeRequested = true;
            return this;
        }

        private void TaskRun()
        {
            bool shouldReportProgress = false;
            _running = true;
            while (_running && !_token.IsCancellationRequested)
            {
                if (_newActionsAvailable)
                {
                    Action action = null;
                    CancellationToken token = default;
                    lock (_lock)
                    {
                        shouldReportProgress |= _executionQueue.Count >= 2;
                        (action, token) = _executionQueue.Dequeue();
                        _newActionsAvailable = _executionQueue.Count > 0;
                        _actionsExecuted++;
                    }

                    if (token.IsCancellationRequested)
                    {
                        continue;
                    }

                    if (shouldReportProgress)
                    {
                        Progress.Report(_progressId, _actionsExecuted, _totalActions);
                    }

                    try
                    {
                        action?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }
                else if (_completeRequested)
                {
                    // Nothing else to do
                    Progress.Remove(_progressId);
                    break;
                }
            }
        }
    }
}