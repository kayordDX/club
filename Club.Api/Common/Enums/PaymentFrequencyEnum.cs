namespace Club.Common.Enums;

// Provider-agnostic recurring frequency. The values are deliberately aligned 1:1 with the codes
// Payfast expects for its `frequency` field so that provider's mapping is a direct cast and stays
// explicit/auditable. Other providers translate these members into their own scheme.
//
// Adding a new frequency (e.g. Weekly): pick a member name and a stable integer wire value, then
// make every provider that supports recurring map it (or reject it). Prefer keeping any values that
// overlap Payfast's scheme aligned with Payfast's codes to preserve the direct cast; values with no
// Payfast equivalent can use any unused number. The wire value is part of the API contract once
// shipped, so do not renumber existing members.
public enum PaymentFrequencyEnum
{
    Monthly = 3,
    Quarterly = 4,
    Biannually = 5,
    Annually = 6,
}
