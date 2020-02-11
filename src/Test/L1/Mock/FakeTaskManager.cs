using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Pipelines = Microsoft.TeamFoundation.DistributedTask.Pipelines;
using System;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.VisualStudio.Services.Agent.Worker;

namespace Microsoft.VisualStudio.Services.Agent.Tests.L1.Worker
{
    public class FakeTaskManager : ITaskManager
    {

        public void Initialize(IHostContext hostContext)
        {
        }

        public Task DownloadAsync(IExecutionContext executionContext, IEnumerable<Pipelines.JobStep> steps)
        {
            return Task.CompletedTask;
        }

        public Definition Load(Pipelines.TaskStep task)
        {
            return new Definition
            {
                Data = new DefinitionData {
                    Execution = new ExecutionData
                    {
                        Process = new ProcessHandlerData
                        {
                            Target = "test"
                        }
                    }
                },
                Directory = "/"
            };
        }

        /// <summary>
        /// Extract a task that has already been downloaded.
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="task">The task to be extracted.</param>
        public void Extract(IExecutionContext executionContext, Pipelines.TaskStep task)
        {

        }

    }
}