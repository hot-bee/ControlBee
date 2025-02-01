using ControlBee.Models;

namespace ControlBee.Interfaces;

public interface IState : IDisposable
{
    bool ProcessMessage(Message message);
}
