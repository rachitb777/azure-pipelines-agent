// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.L1.Worker
{

    public class WorkerL1Tests : L1TestBase
    {
        [Fact]
        [Trait("Level", "L1")]
        [Trait("Category", "Worker")]
        public async Task Test_Template_Message()
        {
            // Arrange
            var message = LoadTemplateMessage();

            // Act
            var results = await RunWorker(message);

            // Assert
            AssertJobCompleted();
            Assert.Equal(TaskResult.Succeeded, results.Result);

            var steps = GetSteps();
            Assert.Equal(4, steps.Count());
            Assert.Equal(1, steps.Where(x => x.Name == "CmdLine").Count());
        }
    }
}
