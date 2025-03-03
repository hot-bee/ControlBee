using System.Reflection;
using ControlBee.Interfaces;
using ControlBee.Utils;
using ControlBeeAbstract.Devices;
using log4net;
using Dict = System.Collections.Generic.Dictionary<string, object?>;

namespace ControlBee.Models;

public class DeviceLoader : IDeviceLoader
{
    private static readonly ILog Logger = LogManager.GetLogger("General");

    public DeviceLoader(
        ISystemPropertiesDataSource systemPropertiesDataSource,
        IDeviceManager deviceManager
    )
    {
        if (systemPropertiesDataSource.GetValue("Devices") is not Dict devices)
        {
            Logger.Warn("There's no device definitions.");
            return;
        }

        foreach (var (deviceName, deviceInfo) in devices)
        {
            var dllPath = DictPath.Start(deviceInfo)["DllPath"].Value as string;
            if (string.IsNullOrEmpty(dllPath))
            {
                Logger.Error($"There's no dll path for the device. ({deviceName})");
                continue;
            }

            var driverDll = Assembly.LoadFrom(dllPath!);
            var initArgs = DictPath.Start(deviceInfo)["InitArgs"].Value as Dict ?? [];
            var type = driverDll.ExportedTypes.Where(x => x.BaseType?.Name == "Device").ToList()[0];
            var device = (Device)Activator.CreateInstance(type)!;
            device.Init(initArgs);
            deviceManager.Add(deviceName, device);
        }
    }
}
