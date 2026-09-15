namespace Oishipan.Services;

public interface IShippingService
{
    Task<decimal> CalculateDistanceAsync(string address);
    (decimal ShippingFee, decimal SurchargeFee) CalculateFees(decimal distanceInKm);
}
