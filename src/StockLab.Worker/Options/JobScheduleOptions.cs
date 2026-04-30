namespace StockLab.Worker.Options;

public class JobScheduleOptions
{
    public string CronExpression { get; set; } = string.Empty;

    public string TimeZoneId { get; set; } = "Taipei Standard Time";
}
