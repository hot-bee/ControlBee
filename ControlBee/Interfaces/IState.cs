using ControlBee.Models;

namespace ControlBee.Interfaces;

public interface IState
{
    bool ProcessMessage(Message message);
}
