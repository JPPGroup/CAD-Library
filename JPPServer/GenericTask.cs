using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JPPServer
{
    public class GenericTask
    {
        public string Name { get; set; }
        public TaskStatus Status { get; set; } = TaskStatus.Waiting;
    }

    public enum TaskStatus
    {
        Waiting,
        Running,
        Verifying,
        Complete,
        Error
    }
}
