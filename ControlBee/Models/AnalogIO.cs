using ControlBee.Interfaces;

namespace ControlBee.Models;

// ReSharper disable once InconsistentNaming
public abstract class AnalogIO(IDeviceManager deviceManager) : DeviceChannel(deviceManager) { }
