namespace BudTack.Identity.Api;

/// <summary>
/// The only entry point into the identity module. SignUp (HU-01), SignIn
/// (HU-02) and Authenticate arrive with their stories; until then the facade
/// exists so the host and the boundary tests can depend on it.
/// </summary>
public sealed class IdentityFacade
{
    internal IdentityFacade()
    {
    }
}
