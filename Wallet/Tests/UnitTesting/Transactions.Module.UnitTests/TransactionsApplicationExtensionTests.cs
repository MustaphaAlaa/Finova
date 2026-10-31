using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Banks.Contracts.Enums;
using Transaction.Contracts.Interfaces;
using Transactions.Application.Extensions;
using Transactions.Application.Services.BankTransactionParserFactory;

namespace Transactions.Module.UnitTests;

public class TransactionsApplicationExtensionTests
{
    [Theory]
    [InlineData(ContractedBanks.Acme, typeof(AcmeParser))]
    [InlineData(ContractedBanks.PayTech, typeof(PayTechParser))]
    [InlineData(ContractedBanks.GothamPayments, typeof(GothamPaymentsParser))]
    [InlineData(ContractedBanks.BanqueTech, typeof(BanqueTechParser))]
    public void AddTransactionsApplicationServices_ResolvesParserForEachConfiguredBank(
        ContractedBanks bank,
        Type expectedParserType)
    {
        var services = new ServiceCollection();
        services.AddTransactionsApplicationServices();
        using var provider = services.BuildServiceProvider();

        var resolver = provider.GetRequiredService<BankTransactionParserResolver>();
        var factory = resolver.ResolveFactory(bank.ToString());

        factory.Should().NotBeNull();
        factory!.CreateTransactionParser().GetType().Should().Be(expectedParserType);
        // Catches missing or mis-keyed parser factory registrations for a supported bank.
    }
}
