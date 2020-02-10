using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using System.Collections.Generic;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using Microsoft.VisualStudio.Services.WebApi;
using System.Linq;
using System.Reflection;
using Pipelines = Microsoft.TeamFoundation.DistributedTask.Pipelines;
using Microsoft.VisualStudio.Services.Agent.Worker;

namespace Microsoft.VisualStudio.Services.Agent.Tests.L1.Worker
{
    public class FakeTaskServer : ITaskServer
    {

        public void Initialize(IHostContext hostContext)
        {
        }

        public Task ConnectAsync(VssConnection jobConnection)
        {
            return Task.CompletedTask;
        }

        public Task<Stream> GetTaskContentZipAsync(Guid taskId, TaskVersion taskVersion, CancellationToken token)
        {
            return null;
        }

        public Task<bool> TaskDefinitionEndpointExist()
        {
            return Task.FromResult(true);
        }
    }
}