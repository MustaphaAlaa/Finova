using Banks.Contracts.Entities;
using Banks.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Transaction.Contracts.Entities;

namespace Banks.Infrastructure.EntitiesConfigurations;

public class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
{
    public void Configure(EntityTypeBuilder<Currency> builder)
    {
        builder.HasKey(cr => cr.Id);
        builder.Property(cr => cr.Name).HasConversion<string>().IsRequired();
        builder.Property(cr => cr.Code).HasConversion<string>().IsRequired();


        builder.HasData(_currencies);
    }

    List<Currency> _currencies = new List<Currency>
{
    new Currency
    {
        Id = Guid.Parse("00596123-1c69-4603-ac90-4bbe433621e9"),
        Code = Currencies.EGP,
        Name = "Egyptian Pound"
    },
    new Currency
    {
        Id = Guid.Parse("cce9fc73-d6f7-4604-bb0b-6358a65e2053"),
        Code = Currencies.USD,
        Name = "US Dollar"
    },
    new Currency
    {
        Id = Guid.Parse("66bbf057-896c-4223-a8cb-915599fa3003"),
        Code = Currencies.GBP,
        Name = "British Pound Sterling"
    },
    new Currency
    {
        Id = Guid.Parse("66deb98a-534f-4a76-9cc3-17c60b54fc94"),
        Code = Currencies.SAR,
        Name = "Saudi Riyal"
    },
    new Currency
    {
        Id = Guid.Parse("704ed0f7-ab61-444f-96ee-d249eab626e7"),
        Code = Currencies.AED,
        Name = "UAE Dirham"
    },
    new Currency
    {
        Id = Guid.Parse("ba922e41-9a9c-422d-8810-e634372e7075"),
        Code = Currencies.EURO,
        Name = "Euro"
    },
    new Currency
    {
        Id = Guid.Parse("d85f635f-2b96-44b8-b624-ba409c82eb34"),
        Code = Currencies.KWD,
        Name = "Kuwaiti Dinar"
    }
};

}