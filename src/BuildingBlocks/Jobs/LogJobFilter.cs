using Hangfire.Client;
using Hangfire.Logging;
using Hangfire.Server;
using Hangfire.States;
using Hangfire.Storage;

namespace FSH.Framework.Jobs;

public class LogJobFilter : IClientFilter, IServerFilter, IElectStateFilter, IApplyStateFilter
{
    private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();

    public LogJobFilter()
    {
    }

    public void OnCreating(CreatingContext context)
    {
        var job = context.Job;
        var jobName = GetJobName(job);

        Logger.DebugFormat(
            "Creating job for {0}.", jobName);
    }

    public void OnCreated(CreatedContext context)
    {
        var job = context.Job;
        var jobName = GetJobName(job);
        var jobId = context.BackgroundJob?.Id ?? "<unknown>";
        var recurringJobId = context.Parameters.TryGetValue("RecurringJobId", out var r) ? r : null;

        Logger.DebugFormat(
            "Job created: Id={0}, Name={1}, RecurringJobId={2}",
            jobId,
            jobName,
            recurringJobId ?? "<none>");
    }

    public void OnPerforming(PerformingContext context)
    {
        var backgroundJob = context.BackgroundJob;
        var job = backgroundJob.Job;
        var jobName = GetJobName(job);
        var recurringJobId = context.GetJobParameter<string>("RecurringJobId") ?? "<none>";
        var args = FormatArguments(job.Args);

        Logger.DebugFormat(
            "Starting job: Id={0}, Name={1}, RecurringJobId={2}, Queue={3}, Args={4}",
            backgroundJob.Id,
            jobName,
            recurringJobId,
            backgroundJob.Job.Queue,
            args);
    }

    public void OnPerformed(PerformedContext context)
    {
        var backgroundJob = context.BackgroundJob;
        var job = backgroundJob.Job;
        var jobName = GetJobName(job);

        Logger.DebugFormat(
            "Job completed: Id={0}, Name={1}, Succeeded={2}",
            backgroundJob.Id,
            jobName,
            context.Exception == null);
    }

    public void OnStateElection(ElectStateContext context)
    {
        if (context.CandidateState is FailedState failedState)
        {
            Logger.WarnFormat(
                "Job '{0}' failed. Name={1}, Reason={2}",
                context.BackgroundJob.Id,
                GetJobName(context.BackgroundJob.Job),
                failedState.Exception);
        }
    }

    public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
    {
        Logger.DebugFormat(
            "Job state changed: Id={0}, Name={1}, OldState={2}, NewState={3}",
            context.BackgroundJob.Id,
            GetJobName(context.BackgroundJob.Job),
            context.OldStateName ?? "<none>",
            context.NewState?.Name ?? "<none>");
    }

    public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
    {
        Logger.DebugFormat(
            "Job state unapplied: Id={0}, Name={1}, OldState={2}",
            context.BackgroundJob.Id,
            GetJobName(context.BackgroundJob.Job),
            context.OldStateName ?? "<none>");
    }

    private static string GetJobName(Hangfire.Common.Job job)
    {
        return $"{job.Method.Name}";
    }

    private static string FormatArguments(IReadOnlyList<object?> args)
    {
        if (args == null || args.Count == 0)
        {
            return "[]";
        }

        try
        {
            var rendered = args.Select(a => a?.ToString() ?? "null");
            return "[" + string.Join(", ", rendered) + "]";
        }
        catch
        {
            return "[<unavailable>]";
        }
    }
}
