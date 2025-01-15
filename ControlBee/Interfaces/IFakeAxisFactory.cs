using ControlBee.Models;

namespace ControlBee.Interfaces;

public interface IFakeAxisFactory
{
    FakeAxis Create(bool emulationMode);
}
