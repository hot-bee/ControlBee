using ControlBee.Interfaces;
using ControlBee.Models;
using Microsoft.Extensions.DependencyInjection;

namespace ControlBee.Services;

public class DialogContextFactory
{
    private readonly IServiceProvider? _serviceProvider;

    public DialogContextFactory() { }

    public DialogContextFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IDialogContext Create()
    {
        return _serviceProvider != null
            ? _serviceProvider.GetRequiredService<IDialogContext>()
            : new DialogContext();
    }
}
