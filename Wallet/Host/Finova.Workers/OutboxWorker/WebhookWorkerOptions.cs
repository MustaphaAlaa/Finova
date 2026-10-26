namespace OutboxWorker.Workers;

public sealed class WebhookWorkerOptions
{
    public int ClaimSize { get; set; } = 16; // rows claimed per poll
    public int MaxConcurrency { get; set; } = 4; // keep < Npgsql MaxPoolSize
    public int MaxAttempts { get; set; } = 8;
    public TimeSpan IdleDelay { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan BaseRetryDelay { get; set; } = TimeSpan.FromSeconds(10);
}
