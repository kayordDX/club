using System.ComponentModel;
using System.Reflection;

namespace Club.Entities;

public enum PaymentProviderType
{
    [Description("PeachPayments")]
    Peach,

    [Description("Payfast")]
    Payfast,
}

public static class PaymentProviderTypeExtensions
{
    public static string GetKey(this PaymentProviderType type)
    {
        var field = typeof(PaymentProviderType).GetField(type.ToString());
        var attr = field?.GetCustomAttribute<DescriptionAttribute>();
        return attr?.Description ?? type.ToString();
    }
}
