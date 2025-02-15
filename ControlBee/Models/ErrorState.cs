using ControlBee.Exceptions;
using ControlBee.Interfaces;

namespace ControlBee.Models;

public class ErrorState(SequenceError error) : IState
{
    public SequenceError Error { get; } = error;

    public virtual bool ProcessMessage(Message message)
    {
        return false;
    }

    public virtual void Dispose()
    {
        // Empty
    }
}
