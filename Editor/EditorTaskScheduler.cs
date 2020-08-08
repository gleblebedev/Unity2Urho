using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace UnityToCustomEngineExporter.Editor
{
    public class EditorTaskScheduler
    {
        public static readonly EditorTaskScheduler Default = new EditorTaskScheduler();

        private readonly object _gate = new object();

        private readonly Queue<Func<IEnumerable<ProgressBarReport>>> _foregroundTasks =
            new Queue<Func<IEnumerable<ProgressBarReport>>>();

        private readonly Stopwatch _foregroundStepStopwatch = new Stopwatch();
        private readonly EditorApplication.CallbackFunction _processForegroundQueue;
        private List<Task> _tasks;
        private IEnumerator<ProgressBarReport> _enumeration;
        private int _backgroundTaskCount;
        private int _completeForegroundTasksCounter;

        public EditorTaskScheduler()
        {
            _processForegroundQueue = ProcessForegroundQueue;
        }

        public event EventHandler EditorUpdate;

        public event EventHandler ForegroundQueueComplete;

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
                    return _foregroundTasks.Count > 0 || _enumeration != null;
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

        private static IEnumerable<ProgressBarReport> ActionAsEnumerable(Action task, ProgressBarReport report)
        {
            yield return report;
            task();
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

        public bool DisplayProgressBar()
        {
            lock (_gate)
            {
                if (_foregroundTasks.Count == 0 && _enumeration == null)
                {
                    EditorUtility.ClearProgressBar();
                    return false;
                }

                var counter = (float) _completeForegroundTasksCounter;
                EditorUtility.DisplayProgressBar("Hold on...", CurrentReport.Message,
                    counter / (counter + _foregroundTasks.Count + 1));
                return true;
            }
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
                        CurrentReport = exception.ToString();
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
                        if (_foregroundTasks.Count == 0)
                        {
                            CurrentReport = "";
                            _completeForegroundTasksCounter = 0;
                            EditorApplication.update =
                                Delegate.Remove(EditorApplication.update, _processForegroundQueue) as
                                    EditorApplication.CallbackFunction;
                            ForegroundQueueComplete?.Invoke(this, EventArgs.Empty);
                            return;
                        }

                        ++_completeForegroundTasksCounter;

                        task = _foregroundTasks.Dequeue();
                    }

                    try
                    {
                        _enumeration = task().GetEnumerator();
                    }
                    catch (Exception exception)
                    {
                        CurrentReport = exception.ToString();
                        Debug.LogError(exception);
                    }
                }

            EditorUpdate?.Invoke(this, EventArgs.Empty);
        }
    }
}