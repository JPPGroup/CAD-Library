using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JPPServer
{
    public class AutocadInstance : IDisposable
    {
        private Task _executingTask;
        private readonly CancellationTokenSource _stoppingCts = new CancellationTokenSource();

        List<string> standardOutLog;
        StreamWriter stdin;
        StreamReader stdout, stderror;
        Process p;

        public AutocadTask task;
        public bool Idle;

        public AutocadInstance()
        {
            var psi = new ProcessStartInfo("C:\\Program Files\\Autodesk\\AutoCAD 2018\\accoreconsole.exe")
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            standardOutLog = new List<string>();

            p = new Process { StartInfo = psi };                      

            p.Start();

            stdin = p.StandardInput;
            stdout = p.StandardOutput;
            stderror = p.StandardError;

            Idle = true;
        }

        public Task StartAsync(CancellationToken cancellationToken, AutocadTask at)
        {           
            // Store the task we're executing
            _executingTask = ExecuteAsync(_stoppingCts.Token);
            task = at;

            task.Status = TaskStatus.Running;

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
            //Send

            /*while (!stoppingToken.IsCancellationRequested)
            {
                await Update();
                await Task.Delay(1000);
            }*/

            await Task.Delay(5000);

            task.Status = TaskStatus.Verifying;
            Idle = true;
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

        public async Task Update()
        {
            if (stdout.Peek() != -1)
            {
                string line = await stdout.ReadLineAsync();
                standardOutLog.Add(line);
            }

            if (stderror.Peek() != -1)
            {
                string line2 = await stderror.ReadLineAsync();
                standardOutLog.Add(line2);
            }            
        }

        public void Dispose()
        {
            // write a line to the subprocess
            p.StandardInput.WriteLine("exit");
        }
    }
}
