using Banks.Infrastructure.Extensions;
using MassTransit;
using OutboxWorker.Workers;
using Shared.Exceptions;
using Transaction.Contracts.Interfaces;
using Transaction.Infrastructure;
using Transactions.Application.Extensions;

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

builder.Services.AddSingleton<InboundWebhookMessageChannel>();

// Register before MassTransit so its producers stop before this channel is completed and drained.
builder.Services.AddHostedService(services =>
    services.GetRequiredService<InboundWebhookMessageChannel>()
);

builder.Services.AddScoped<IMessageProcessingRecordWriter, MessageProcessingRecordWriter>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<InboundWebhookMessagesConsumer>(consumer => consumer.ConcurrentMessageLimit = 8);
    x.SetKebabCaseEndpointNameFormatter();

    x.UsingRabbitMq(
        (context, cfg) =>
        {
            cfg.Host(
                "rabbitmq",
                "/",
                h =>
                {
                  
                    h.Username(
                        builder.Configuration["RabbitMq:USER"]
                            ?? throw new InternalConfigurationException(
                                "RabbitMq User is not found"
                            )
                    );
                   
                    h.Password(
                        builder.Configuration["RabbitMq:PASS"]
                            ?? throw new InternalConfigurationException(
                                "RabbitMq PASS is not found"
                            )
                    );
                }
            );

            cfg.ConfigureEndpoints(context);
        }
    );
});

var host = builder.Build();

await host.Services.MigrateBanksInfrastructureAsync();

host.Run();
