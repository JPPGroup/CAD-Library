using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JPPServer
{
    public class TaskManagerLogic : IHostedService
    {
        private Task _executingTask;
        private readonly CancellationTokenSource _stoppingCts = new CancellationTokenSource();

        private List<AutocadInstance> autocadInstances;

        private readonly TaskManager _taskManager;

        public TaskManagerLogic(TaskManager taskManager)
        {
            _taskManager = taskManager;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Store the task we're executing
            _executingTask = Task.Run(() => ExecuteAsync(_stoppingCts.Token));

            // If the task is completed then return it, 
            // this will bubble cancellation and failure to the caller
            if (_executingTask.IsCompleted)
            {
                return _executingTask;
            }

            // Otherwise it's running
            return Task.CompletedTask;


        }

        protected async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            autocadInstances = new List<AutocadInstance>();
            List<Task> runningAutocadTasks = new List<Task>();

            while (!stoppingToken.IsCancellationRequested)
            {
                //_taskManager.autocadEmpty.WaitOne();

                AssignAutocadWork();

                runningAutocadTasks.Add(Task.Delay(5000));
                int index = Task.WaitAny(runningAutocadTasks.ToArray());                
                runningAutocadTasks.RemoveAt(index);

                CompleteAutocadWork();
            }
        }

        private void AssignAutocadWork()
        {
            if(_taskManager.autocadQueue.Count < 1)
            {
                _taskManager.autocadEmpty.Reset();
            } else
            {
                //Check there are workers
                if (autocadInstances.Count < 1)
                {
                    AutocadInstance inst = new AutocadInstance();
                    autocadInstances.Add(inst);
                }

                //Find an idle worker
                foreach(AutocadInstance ai in autocadInstances)
                {
                    if(ai.Idle)
                    {
                        ai.StartAsync(new CancellationToken(), _taskManager.autocadQueue.Dequeue());
                        break;
                    }
                }
            }
        }

        private void CompleteAutocadWork()
        {
            foreach (AutocadInstance ai in autocadInstances)
            {
                if (ai.Idle)
                {
                    if (ai.task != null)
                    {
                        if (ai.task.Status == TaskStatus.Verifying)
                        {
                            _taskManager.CompletedTasks.Add(ai.task);
                            ai.task.Status = TaskStatus.Complete;
                            ai.task = null;
                        }
                    }
                }
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            // Stop called without start
            if (_executingTask == null)
            {
                return;
            }

            try
            {
                // Signal cancellation to the executing method
                _stoppingCts.Cancel();
            }
            finally
            {
                // Wait until the task completes or the stop token triggers
                await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));
            }
        }                
    }
}
