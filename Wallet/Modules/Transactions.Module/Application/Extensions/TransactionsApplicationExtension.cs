using Banks.Contracts.Enums;
using Microsoft.Extensions.DependencyInjection;
using Transaction.Contracts.Interfaces;
using Transactions.Application.Interfaces.IFactory;
using Transactions.Application.Interfaces.IValidations;
using Transactions.Application.Services;
using Transactions.Application.Services.BankTransactionParserFactory;

namespace Transactions.Application.Extensions;

public static class TransactionsApplicationExtension
{
    public static void AddTransactionsApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<BankTransactionParserResolver>();

        services.AddScoped<ITransactionServies, TransactionServices>();

        services.AddScoped<ITransactionServicesValidator, TransactionServicesValidator>();

        ParserFactoryServices(services);
    }

    private static void ParserFactoryServices(IServiceCollection services)
    {
        services.AddKeyedScoped<IBankTransactionFactory, AcmeFactory>(nameof(ContractedBanks.Acme));

        services.AddKeyedScoped<IBankTransactionFactory, PayTechFactory>(
            nameof(ContractedBanks.PayTech)
        );

        services.AddKeyedScoped<IBankTransactionFactory, GothamPaymentsFactory>(
            nameof(ContractedBanks.GothamPayments)
        );

        services.AddKeyedScoped<IBankTransactionFactory, BanqueTechFactory>(
            nameof(ContractedBanks.BanqueTech)
        );
    }
}
