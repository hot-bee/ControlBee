using ControlBee.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ControlBee.Models;

public class DialogViewFactory(IServiceProvider serviceProvider)
{
    public IDialogView Create()
    {
        return serviceProvider.GetRequiredService<IDialogView>();
    }
}
