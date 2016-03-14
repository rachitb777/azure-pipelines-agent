using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    public class ExecutionContextType
    {
        public static string Job = "Job";
        public static string Task = "Task";
    }

    [ServiceLocator(Default = typeof(ExecutionContext))]
    public interface IExecutionContext : IAgentService
    {
        TaskResult? Result { get; set; }
        CancellationToken CancellationToken { get; }
        List<ServiceEndpoint> Endpoints { get; }
        Variables Variables { get; }

        // Initialize
        void InitializeJob(JobRequestMessage message);
        IExecutionContext CreateChild(Guid recordId, string name);

        // logging
        bool WriteDebug { get; set; }
        void Write(string tag, string message);

        // timeline record update methods
        void Start(string currentOperation = null);
        void Complete(string currentOperation = null);
        void AddIssue(Issue issue);
        void Progress(int percentage, string currentOperation = null);
        void UpdateDetailTimelineRecord(TimelineRecord record);
    }

    public sealed class ExecutionContext : AgentService, IExecutionContext
    {
        private const int _maxIssueCount = 10;

        private readonly TimelineRecord _record = new TimelineRecord();
        private readonly Dictionary<Guid, TimelineRecord> _detailRecords = new Dictionary<Guid, TimelineRecord>();

        private IPagingLogger _logger;
        private IJobServerQueue _jobServerQueue;

        private Guid _mainTimelineId;
        private Guid _detailTimelineId;
        private int _childExecutionContextCount = 0;

        public CancellationToken CancellationToken { get; private set; }
        public List<ServiceEndpoint> Endpoints { get; private set; }
        public Variables Variables { get; private set; }
        public bool WriteDebug { get; set; }

        public TaskResult? Result
        {
            get
            {
                return _record.Result;
            }
            set
            {
                _record.Result = value;
            }
        }

        private string ContextType
        {
            get
            {
                return _record.RecordType;
            }
        }

        // might remove this.
        // TODO: figure out how do we actually use the result code.
        public string ResultCode
        {
            get
            {
                return _record.ResultCode;
            }
            set
            {
                _record.ResultCode = value;
            }
        }

        public IExecutionContext CreateChild(Guid recordId, string name)
        {
            Trace.Entering();

            var child = new ExecutionContext();
            child.Initialize(HostContext);
            child.Variables = Variables;
            child.Endpoints = Endpoints;
            child.CancellationToken = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken).Token;

            // the job timeline record is at order 1.
            child.InitializeTimelineRecord(_mainTimelineId, recordId, _record.Id, ExecutionContextType.Task, name, _childExecutionContextCount + 2);

            _childExecutionContextCount++;
            return child;
        }

        public void Start(string currentOperation = null)
        {
            _logger = HostContext.CreateService<IPagingLogger>();
            _logger.Setup(_mainTimelineId, _record.Id);

            _record.CurrentOperation = currentOperation ?? _record.CurrentOperation;
            _record.StartTime = DateTime.UtcNow;
            _record.State = TimelineRecordState.InProgress;

            _jobServerQueue.QueueTimelineRecordUpdate(_mainTimelineId, _record);

            //Section
            Write(WellKnownTags.Section, _record.Name);
        }

        public void Complete(string currentOperation = null)
        {
            try
            {


                _record.CurrentOperation = currentOperation ?? _record.CurrentOperation;
                _record.FinishTime = DateTime.UtcNow;
                _record.PercentComplete = 100;
                _record.Result = _record.Result ?? TaskResult.Succeeded;
                _record.State = TimelineRecordState.Completed;

                _jobServerQueue.QueueTimelineRecordUpdate(_mainTimelineId, _record);

                // complete all detail timeline records.
                if (_detailTimelineId != Guid.Empty && _detailRecords.Count > 0)
                {
                    foreach (var record in _detailRecords)
                    {
                        record.Value.FinishTime = record.Value.FinishTime ?? DateTime.UtcNow;
                        record.Value.PercentComplete = record.Value.PercentComplete ?? 100;
                        record.Value.Result = record.Value.Result ?? TaskResult.Succeeded;
                        record.Value.State = record.Value.State ?? TimelineRecordState.Completed;

                        _jobServerQueue.QueueTimelineRecordUpdate(_detailTimelineId, record.Value);
                    }
                }
            }
            finally
            {
                if (_logger != null)
                {
                    _logger.End();
                }
            }            
        }

        public void Progress(int percentage, string currentOperation = null)
        {
            if (percentage > 100 || percentage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(percentage));
            }

            _record.CurrentOperation = currentOperation ?? _record.CurrentOperation;
            _record.PercentComplete = Math.Max(percentage, _record.PercentComplete.Value);

            _jobServerQueue.QueueTimelineRecordUpdate(_mainTimelineId, _record);
        }

        // This is not thread safe, the caller need to take lock before calling issue()
        public void AddIssue(Issue issue)
        {
            if (issue.Type == IssueType.Error)
            {
                if (_record.ErrorCount <= _maxIssueCount)
                {
                    _record.Issues.Add(issue);
                }

                _record.ErrorCount++;
            }
            else if (issue.Type == IssueType.Warning)
            {
                if (_record.WarningCount <= _maxIssueCount)
                {
                    _record.Issues.Add(issue);
                }

                _record.WarningCount++;
            }

            _jobServerQueue.QueueTimelineRecordUpdate(_mainTimelineId, _record);
        }

        public void UpdateDetailTimelineRecord(TimelineRecord record)
        {
            ArgUtil.NotNull(record, nameof(record));

            if (record.RecordType == ExecutionContextType.Job)
            {
                throw new ArgumentOutOfRangeException(nameof(record));
            }

            if (_detailTimelineId == Guid.Empty)
            {
                // create detail timeline
                _detailTimelineId = Guid.NewGuid();
                _record.Details = new Timeline(_detailTimelineId);

                _jobServerQueue.QueueTimelineRecordUpdate(_mainTimelineId, _record);
            }

            TimelineRecord existRecord;
            if (_detailRecords.TryGetValue(record.Id, out existRecord))
            {
                existRecord.Name = record.Name ?? existRecord.Name;
                existRecord.RecordType = record.RecordType ?? existRecord.RecordType;
                existRecord.Order = record.Order ?? existRecord.Order;
                existRecord.ParentId = record.ParentId ?? existRecord.ParentId;
                existRecord.StartTime = record.StartTime ?? existRecord.StartTime;
                existRecord.FinishTime = record.FinishTime ?? existRecord.FinishTime;
                existRecord.PercentComplete = record.PercentComplete ?? existRecord.PercentComplete;
                existRecord.CurrentOperation = record.CurrentOperation ?? existRecord.CurrentOperation;
                existRecord.Result = record.Result ?? existRecord.Result;
                existRecord.ResultCode = record.ResultCode ?? existRecord.ResultCode;
                existRecord.State = record.State ?? existRecord.State;

                _jobServerQueue.QueueTimelineRecordUpdate(_detailTimelineId, existRecord);
            }
            else
            {
                _detailRecords[record.Id] = record;
                _jobServerQueue.QueueTimelineRecordUpdate(_detailTimelineId, record);
            }
        }

        public void InitializeJob(JobRequestMessage message)
        {
            // Validate/store parameters.
            Trace.Entering();
            ArgUtil.NotNull(message, nameof(message));
            ArgUtil.NotNull(message.Environment, nameof(message.Environment));
            ArgUtil.NotNull(message.Environment.Endpoints, nameof(message.Environment.Endpoints));
            ArgUtil.NotNull(message.Environment.Variables, nameof(message.Environment.Variables));

            // Initialize the environment.
            Endpoints = message.Environment.Endpoints;
            Variables = new Variables(HostContext, message.Environment.Variables);

            // Initialize the job timeline record.
            // the job timeline record is at order 1.
            InitializeTimelineRecord(message.Timeline.Id, message.JobId, null, ExecutionContextType.Job, message.JobName, 1);
        }

        // Do not add a format string overload. In general, execution context messages are user facing and
        // therefore should be localized. Use the Loc methods from the StringUtil class. The exception to
        // the rule is command messages - which should be crafted using strongly typed wrapper methods.
        public void Write(string tag, string message)
        {
            string msg = $"{tag}{message}";
            _logger.Write(msg);

            _jobServerQueue.QueueWebConsoleLine(msg);
        }

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);

            _jobServerQueue = HostContext.GetService<IJobServerQueue>();
        }

        private void InitializeTimelineRecord(Guid timelineId, Guid timelineRecordId, Guid? parentTimelineRecordId, string recordType, string name, int order)
        {
            _mainTimelineId = timelineId;
            _record.Id = timelineRecordId;
            _record.RecordType = recordType;
            _record.Name = name;
            _record.Order = order;
            _record.PercentComplete = 0;
            _record.State = TimelineRecordState.Pending;
            _record.ErrorCount = 0;
            _record.WarningCount = 0;

            if (parentTimelineRecordId != null && parentTimelineRecordId.Value != Guid.Empty)
            {
                _record.ParentId = parentTimelineRecordId;
            }

            var configuration = HostContext.GetService<IConfigurationStore>();
            _record.WorkerName = configuration.GetSettings().AgentName;

            _jobServerQueue.QueueTimelineRecordUpdate(_mainTimelineId, _record);
        }
    }

    public static class ExecutionContextExtension
    {
        public static void Error(this IExecutionContext context, Exception ex)
        {
            context.Error(ex.Message);
            context.Debug(ex.ToString());
        }

        // Do not add a format string overload. See comment on ExecutionContext.Write().
        public static void Error(this IExecutionContext context, string message)
        {
            context.Write(WellKnownTags.Error, message);
        }

        // Do not add a format string overload. See comment on ExecutionContext.Write().
        public static void Warning(this IExecutionContext context, string message)
        {
            context.Write(WellKnownTags.Warning, message);
        }

        // Do not add a format string overload. See comment on ExecutionContext.Write().
        public static void Output(this IExecutionContext context, string message)
        {
            context.Write(null, message);
        }

        // Do not add a format string overload. See comment on ExecutionContext.Write().
        public static void Command(this IExecutionContext context, string message)
        {
            context.Write(WellKnownTags.Command, message);
        }

        // Do not add a format string overload. See comment on ExecutionContext.Write().
        public static void Section(this IExecutionContext context, string message)
        {
            context.Write(WellKnownTags.Section, message);
        }

        //
        // Verbose output is enabled by setting System.Debug
        // It's meant to help the end user debug their definitions.
        // Why are my inputs not working?  It's not meant for dev debugging which is diag
        //
        // Do not add a format string overload. See comment on ExecutionContext.Write().
        public static void Debug(this IExecutionContext context, string message)
        {
            if (context.WriteDebug)
            {
                context.Write(WellKnownTags.Debug, message);
            }
        }
    }

    public static class WellKnownTags
    {
        public static readonly string Section = "##[section]";
        public static readonly string Command = "##[command]";
        public static readonly string Error = "##[error]";
        public static readonly string Warning = "##[warning]";
        public static readonly string Debug = "##[debug]";
    }
}