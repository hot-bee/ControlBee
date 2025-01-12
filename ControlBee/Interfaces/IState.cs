using ControlBee.Models;

namespace ControlBee.Interfaces;

public interface IState
{
    IState ProcessMessage(Message message);
}
