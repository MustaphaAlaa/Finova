using Banks.Infrastructure.Extensions;
using OutboxWorker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

var connectionString = builder.Configuration.GetConnectionString("finovaWalletDb");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new Exception("Connection string is required");
}

builder.Services.AddBanksInfrastructure(connectionString);

var host = builder.Build();

await host.Services.MigrateBanksInfrastructureAsync();

host.Run();