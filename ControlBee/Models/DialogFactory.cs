using ControlBee.Interfaces;
using ControlBee.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ControlBee.Models;

public class DialogFactory(
    DialogContextFactory dialogContextFactory,
    IServiceProvider? serviceProvider
) : IDialogFactory
{
    public IDialog Create()
    {
        return serviceProvider != null
            ? serviceProvider.GetRequiredService<IDialog>()
            : new Dialog(dialogContextFactory);
    }
}
