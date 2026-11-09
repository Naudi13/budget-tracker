namespace BudTack.Identity.Api.Contracts;

/// <summary>Who a valid bearer token stands for, and with which role.</summary>
public sealed record AuthenticatedUser(Guid Id, string Role);
