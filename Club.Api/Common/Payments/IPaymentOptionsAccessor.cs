namespace Club.Common.Payments;

public interface IPaymentOptionsAccessor<T> where T : class
{
    Task<T?> GetAsync(CancellationToken ct);
}
