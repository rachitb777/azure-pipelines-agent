using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Pipelines = Microsoft.TeamFoundation.DistributedTask.Pipelines;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Microsoft.VisualStudio.Services.Agent.Worker.Handlers;

namespace Microsoft.VisualStudio.Services.Agent.Tests.L1.Worker
{
    public class FakeHandlerFactory : IHandlerFactory
    {
        public void Initialize(IHostContext hostContext)
        {
        }

        public IHandler Create(
            IExecutionContext executionContext,
            Pipelines.TaskStepDefinitionReference task,
            IStepHost stepHost,
            List<ServiceEndpoint> endpoints,
            List<SecureFile> secureFiles,
            HandlerData data,
            Dictionary<string, string> inputs,
            Dictionary<string, string> environment,
            Variables runtimeVariables,
            string taskDirectory)
            {
                return new FakeHandler();
            }
    }

    public class FakeHandler : Handler, IHandler
    {
        public Task RunAsync()
        {
            return System.Threading.Tasks.Task.CompletedTask;
        }
    }
}