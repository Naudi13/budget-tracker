using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace BudTack.Api.Platform.Web;

/// <summary>
/// MVC only finds <c>public</c> controllers; ours are <c>internal</c>, like the
/// rest of Platform, so the boundary test can keep asserting that the host
/// exposes nothing. This finds both.
/// </summary>
internal sealed class InternalControllers : ControllerFeatureProvider
{
    protected override bool IsController(TypeInfo typeInfo) =>
        typeInfo.IsClass
        && !typeInfo.IsAbstract
        && !typeInfo.ContainsGenericParameters
        && typeof(ControllerBase).IsAssignableFrom(typeInfo)
        && typeInfo.Name.EndsWith("Controller", StringComparison.Ordinal);

    public static IMvcBuilder FindInternalControllers(IMvcBuilder mvc)
    {
        mvc.ConfigureApplicationPartManager(parts =>
        {
            foreach (var provider in parts.FeatureProviders.OfType<ControllerFeatureProvider>().ToList())
            {
                parts.FeatureProviders.Remove(provider);
            }

            parts.FeatureProviders.Add(new InternalControllers());
        });
        return mvc;
    }
}
