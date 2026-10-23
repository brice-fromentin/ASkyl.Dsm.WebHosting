using Askyl.Dsm.WebHosting.Constants.UI;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Askyl.Dsm.WebHosting.Ui.Client.Extensions;

/// <summary>
/// Registers FluentUI with the defaults this application's layouts were designed for.
/// </summary>
public static class FluentUIServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds the FluentUI services, restoring the FluentStack gap FluentUI 4 applied by default.
        /// The rendering tests call this too, so they render what the application renders.
        /// </summary>
        public IServiceCollection AddFluentUIComponentsWithDefaults()
            => services.AddFluentUIComponents(configuration =>
            {
                configuration.DefaultValues.For<FluentStack>().Set(stack => stack.HorizontalGap, LayoutConstants.StackGap);
                configuration.DefaultValues.For<FluentStack>().Set(stack => stack.VerticalGap, LayoutConstants.StackGap);
            });
    }
}
