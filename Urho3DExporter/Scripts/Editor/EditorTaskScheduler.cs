using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace Assets.Scripts.UnityToCustomEngineExporter.Editor
{
    public class EditorTaskScheduler
    {
        public static readonly EditorTaskScheduler Default = new EditorTaskScheduler();

        private readonly object _gate = new object();
        private List<Task> _tasks;

        private readonly Queue<Func<IEnumerable<ProgressBarReport>>> _foregroundTasks =
            new Queue<Func<IEnumerable<ProgressBarReport>>>();

        private readonly Stopwatch _foregroundStepStopwatch = new Stopwatch();
        private readonly EditorApplication.CallbackFunction _processForegroundQueue;
        private IEnumerator<ProgressBarReport> _enumeration;
        private int _backgroundTaskCount;

        public EditorTaskScheduler()
        {
            _processForegroundQueue = ProcessForegroundQueue;
        }

        public event EventHandler EditorUpdate;

        public ProgressBarReport CurrentReport { get; private set; }

        public bool IsRunning
        {
            get
            {
                lock (_gate)
                {
                    return HashForegroundTasks || HashBackgroundTasks;
                }
            }
        }

        public bool HashForegroundTasks
        {
            get
            {
                lock (_gate)
                {
                    return (_foregroundTasks.Count > 0) || (_enumeration != null);
                }
            }
        }

        public bool HashBackgroundTasks
        {
            get
            {
                lock (_gate)
                {
                    return _backgroundTaskCount != 0;
                }
            }
        }

        public void ScheduleForegroundTask(Action task, ProgressBarReport report)
        {
            ScheduleForegroundTask(() => ActionAsEnumerable(task, report));
        }

        public void ScheduleForegroundTask(Func<IEnumerable<ProgressBarReport>> task)
        {
            lock (_gate)
            {
                if (_foregroundTasks.Count == 0)
                    EditorApplication.update =
                        Delegate.Combine(EditorApplication.update, _processForegroundQueue) as
                            EditorApplication.CallbackFunction;
                _foregroundTasks.Enqueue(task);
            }
        }

        public void ScheduleBackgroundTask(Func<Task> task, ProgressBarReport progressBarReport)
        {
            lock (_gate)
            {
                _tasks = _tasks ?? new List<Task>();
                _tasks.Add(Task.Run(() => CallBackgroundTask(task, progressBarReport)));
            }
        }

        public async Task WaitForBackgroundTasks()
        {
            for (;;)
            {
                List<Task> tasks;
                lock (_gate)
                {
                    if (_tasks == null || _tasks.Count == 0)
                        return;
                    tasks = _tasks;
                    _tasks = null;
                }

                await Task.WhenAll(tasks);
            }
        }

        private static IEnumerable<ProgressBarReport> ActionAsEnumerable(Action task, ProgressBarReport report)
        {
            yield return report;
            task();
        }

        private async Task CallBackgroundTask(Func<Task> task, ProgressBarReport report)
        {
            CurrentReport = report;
            Interlocked.Increment(ref _backgroundTaskCount);
            try
            {
                await task();
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }

            Interlocked.Decrement(ref _backgroundTaskCount);
        }

        private void ProcessForegroundQueue()
        {
            EditorUpdate?.Invoke(this, EventArgs.Empty);

            _foregroundStepStopwatch.Restart();
            for (; _foregroundStepStopwatch.Elapsed.TotalMilliseconds < 16;)
                if (_enumeration != null)
                {
                    try
                    {
                        if (!_enumeration.MoveNext())
                        {
                            _enumeration.Dispose();
                            _enumeration = null;
                        }
                        else
                        {
                            CurrentReport = _enumeration.Current;
                        }
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError(exception);
                        _enumeration?.Dispose();
                        _enumeration = null;
                    }
                }
                else
                {
                    Func<IEnumerable<ProgressBarReport>> task;
                    lock (_gate)
                    {
                        if (_foregroundTasks.Count == 0) return;

                        task = _foregroundTasks.Dequeue();
                        if (_foregroundTasks.Count == 0)
                            EditorApplication.update =
                                Delegate.Remove(EditorApplication.update, _processForegroundQueue) as
                                    EditorApplication.CallbackFunction;
                    }

                    try
                    {
                        _enumeration = task().GetEnumerator();
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError(exception);
                    }
                }
        }
    }
}