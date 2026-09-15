namespace Club.Common.Enums;

// Values mirror the codes Payfast expects for the `frequency` recurring-billing field so the
// mapping to the gateway is explicit. Providers that support recurring payments translate this
// into their own scheme.
public enum PaymentFrequencyEnum
{
    Monthly = 3,
    Quarterly = 4,
    Biannually = 5,
    Annually = 6,
}
