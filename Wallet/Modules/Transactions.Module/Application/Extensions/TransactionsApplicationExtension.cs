using Banks.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Transactions.Application.Interfaces.IFactory; 
using Transactions.Application.Services.BankTransactionParserFactory; 

namespace Transactions.Application.Extensions;

public static class TransactionsApplicationExtension
{
    public static void AddBanksApplicationServices(this IServiceCollection services)
    {
        BankFactoryServices(services);
    }


    private static void BankFactoryServices(IServiceCollection services)
    {
        services.AddKeyedScoped<IBankServicesFactory, AcmeFactory>(nameof(ContractedBanks.Acme));

        services.AddKeyedScoped<IBankServicesFactory, PayTechFactory>(
            nameof(ContractedBanks.PayTech)
        );

        services.AddKeyedScoped<IBankServicesFactory, GothamPaymentsFactory>(
            nameof(ContractedBanks.GothamPayments)
        );

        services.AddKeyedScoped<IBankServicesFactory, BanqueTechFactory>(
            nameof(ContractedBanks.BanqueTech)
        );
    }
}