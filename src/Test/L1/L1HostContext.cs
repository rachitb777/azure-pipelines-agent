// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.WebApi;
using Pipelines = Microsoft.TeamFoundation.DistributedTask.Pipelines;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Microsoft.VisualStudio.Services.Agent.Worker.Build;
using Microsoft.VisualStudio.Services.Agent.Worker.Release;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.L1.Worker
{
    public class L1HostContext : HostContext
    {
        public L1HostContext(string hostType, string logFile = null)
            : base(hostType, logFile)
        {
        }

        public T SetupService<T>(Type target) where T : class, IAgentService
        {
            if (!typeof(T).IsAssignableFrom(target))
            {
                throw new ArgumentException("The target type must implement the specified interface");
            }
            _serviceTypes.TryAdd(typeof(T), target);
            return CreateService<T>();
        }
    }
}