﻿using ControlBee.Interfaces;
using ControlBee.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ControlBee.Models;

public class DialogFactory(
    DialogContextFactory dialogContextFactory,
    IEventWriter eventWriter,
    IServiceProvider? serviceProvider
) : IDialogFactory
{
    public IDialog Create()
    {
        return serviceProvider != null
            ? serviceProvider.GetRequiredService<IDialog>()
            : new Dialog(dialogContextFactory, eventWriter);
    }
}
