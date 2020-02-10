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
    public class FakeJobServer : IJobServer
    {
        public List<JobEvent> RecordedEvents { get; }

        public Dictionary<Guid, Timeline> Timelines { get; }

        public FakeJobServer()
        {
            RecordedEvents = new List<JobEvent>();
        }

        public void Initialize(IHostContext hostContext)
        {
        }
        public Task ConnectAsync(VssConnection jobConnection)
        {
            return Task.CompletedTask;
        }

        public Task<TaskLog> AppendLogContentAsync(Guid scopeIdentifier, string hubName, Guid planId, int logId, Stream uploadStream, CancellationToken cancellationToken)
        {
            return null;
        }
        public Task AppendTimelineRecordFeedAsync(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId, Guid timelineRecordId, Guid stepId, IList<string> lines, long startLine, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
        public Task<TaskAttachment> CreateAttachmentAsync(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId, Guid timelineRecordId, String type, String name, Stream uploadStream, CancellationToken cancellationToken)
        {
            return null;
        }
        public Task<TaskLog> CreateLogAsync(Guid scopeIdentifier, string hubName, Guid planId, TaskLog log, CancellationToken cancellationToken)
        {
            log.Id = 1;
            return Task.FromResult(log);
        }
        public Task<Timeline> CreateTimelineAsync(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId, CancellationToken cancellationToken)
        {
            var timeline = new Timeline {
                Id = timelineId
            };
            Timelines.Add(timelineId, timeline);
            return Task.FromResult(timeline);
        }
        public Task<List<TimelineRecord>> UpdateTimelineRecordsAsync(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId, IEnumerable<TimelineRecord> records, CancellationToken cancellationToken)
        {
            var recordDictionary = records.ToDictionary(x => x.Id);
            Timeline timeline = Timelines[timelineId];
            timeline.Records.RemoveAll(x => recordDictionary.Keys.Contains(x.Id));
            timeline.Records.AddRange(records);
            return Task.FromResult(records.ToList());
        }
        public Task RaisePlanEventAsync<T>(Guid scopeIdentifier, string hubName, Guid planId, T eventData, CancellationToken cancellationToken) where T : JobEvent
        {
            RecordedEvents.Add(eventData);
            return Task.CompletedTask;
        }
        public Task<Timeline> GetTimelineAsync(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Timelines[timelineId]);
        }
    }
}