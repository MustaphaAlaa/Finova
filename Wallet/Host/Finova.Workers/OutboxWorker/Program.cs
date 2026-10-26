using Banks.Infrastructure.Extensions;
using MassTransit;
using OutboxWorker.Workers;
using Transactions.Application.Extensions;
using Transactions.Infrastructure.Extensions;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("finovaWalletDb");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new Exception("Connection string is required");
}

builder.Services.AddBanksInfrastructure(connectionString);
builder.Services.AddTransactionsInfrastructure(connectionString);

builder.Services.AddTransactionsApplicationServices();


builder.Services.AddHostedService<MessagePublisher>();

builder.Services.AddHostedService<BankTransactionStageFlusherWorker>();


builder.Services.AddScoped<IMessageProcessingRecordWriter, MessageProcessingRecordWriter>();


builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<InboundWebhookMessagesConsumer>();
    x.SetKebabCaseEndpointNameFormatter();

    x.UsingRabbitMq((context, cfg) =>

    {
        cfg.Host("rabbitmq", "/", h =>
        {
            h.Username("finova");
            h.Password(
                builder.Configuration["RabbitMq:Password"]!
            );
        });

        cfg.ConfigureEndpoints(context);
    });
});


var host = builder.Build();

await host.Services.MigrateBanksInfrastructureAsync();

host.Run();