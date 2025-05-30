﻿using ControlBee.Interfaces;

namespace ControlBee.Models;

public class EmptyState : IState
{
    public virtual bool ProcessMessage(Message message)
    {
        return false;
    }

    public virtual void Dispose()
    {
        // Empty
    }
}
