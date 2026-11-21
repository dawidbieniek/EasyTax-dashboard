using EasyTax.Core.Domain.Alerts;

namespace EasyTax.App.Alerts;

/// <summary>An alert together with whether the user already marked it as handled ("already told them").</summary>
public sealed record AlertItem(ClientAlert Alert, bool IsHandled);
