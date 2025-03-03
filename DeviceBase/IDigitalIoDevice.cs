namespace DeviceBase;

public interface IDigitalIoDevice : IDevice
{
    bool GetDigitalInputBit(int channel);
    void SetDigitalOutputBit(int channel, bool value);
    bool GetDigitalOutputBit(int channel);
}
