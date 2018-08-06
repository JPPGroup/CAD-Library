using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JPPServer
{
    public class TaskManager
    {
        public List<GenericTask> CompletedTasks = new List<GenericTask>();
        public Queue<AutocadTask> autocadQueue = new Queue<AutocadTask>();
        public List<AutocadInstance> instances = new List<AutocadInstance>();

        public ManualResetEvent autocadEmpty = new ManualResetEvent(false);                  

        public void StartTask(AutocadTask task)
        {
            //TODO: Is this thread safe??
            autocadQueue.Enqueue(task);

            autocadEmpty.Set();
        }

        public List<GenericTask> GetTasks()
        {
            List<GenericTask> tasks = new List<GenericTask>();

            foreach(GenericTask gt in CompletedTasks)
            {
                tasks.Add(gt);
            }

            foreach(AutocadTask at in autocadQueue.ToArray())
            {
                tasks.Add(at);
            }

            return tasks;
        }
    }
}
