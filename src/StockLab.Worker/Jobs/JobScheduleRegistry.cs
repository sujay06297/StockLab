using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using StockLab.Worker.Options;

namespace StockLab.Worker.Jobs;

public static class JobScheduleRegistry
{
    private const string JobSchedulesSectionName = "JobSchedules";

    public static void AddScheduledJobs(IConfiguration configuration, IServiceCollection services)
    {
        services.AddQuartz(quartz =>
        {
            AddScheduledJob<StockDayAllSyncJob>(
                quartz,
                configuration,
                StockDayAllSyncJob.JobName,
                StockDayAllSyncJob.DefaultCronExpression,
                StockDayAllSyncJob.DefaultTimeZoneId,
                "同步每日股票行情");

            AddScheduledJob<StockMomentumSelectionJob>(
                quartz,
                configuration,
                StockMomentumSelectionJob.JobName,
                StockMomentumSelectionJob.DefaultCronExpression,
                StockMomentumSelectionJob.DefaultTimeZoneId,
                "挑選動能候選股票");
        });

        services.AddQuartzHostedService(options =>
        {
            options.WaitForJobsToComplete = true;
        });
    }

    private static void AddScheduledJob<TJob>(
        IServiceCollectionQuartzConfigurator quartz,
        IConfiguration configuration,
        string jobName,
        string defaultCronExpression,
        string defaultTimeZoneId,
        string description)
        where TJob : IJob
    {
        var schedule = configuration
            .GetSection($"{JobSchedulesSectionName}:{jobName}")
            .Get<JobScheduleOptions>() ?? new JobScheduleOptions();

        var cronExpression = string.IsNullOrWhiteSpace(schedule.CronExpression)
            ? defaultCronExpression
            : schedule.CronExpression;
        var timeZoneId = string.IsNullOrWhiteSpace(schedule.TimeZoneId)
            ? defaultTimeZoneId
            : schedule.TimeZoneId;

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        var jobKey = new JobKey(jobName);

        quartz.AddJob<TJob>(job => job
            .WithIdentity(jobKey)
            .WithDescription(description));

        quartz.AddTrigger(trigger => trigger
            .ForJob(jobKey)
            .WithIdentity($"{jobName}.CronTrigger")
            .WithCronSchedule(cronExpression, scheduleBuilder => scheduleBuilder
                .InTimeZone(timeZone)
                .WithMisfireHandlingInstructionFireAndProceed())
            .WithDescription($"{description}：{cronExpression} ({timeZone.Id})"));
    }
}
