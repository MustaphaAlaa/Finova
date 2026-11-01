using Banks.Application.Dtos;

namespace Banks.Application.Interfaces.IServices;

public interface ISendMoneyService
{
    Task<string> CreatePaymentRequestXmlAsync(
        Guid applicationUserId,
        SendMoneyRequest request,
        CancellationToken cancellationToken);
}
