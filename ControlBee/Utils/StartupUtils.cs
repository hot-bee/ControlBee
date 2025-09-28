using System.Diagnostics;
using ControlBee.Interfaces;
using ControlBeeAbstract.Devices;

namespace ControlBee.Utils;

public class StartupUtils
{
    public static void LaunchInspectionBeeAndConnect(string visionDeviceName, IDeviceManager deviceManager, int retryCount = 3, int delayBetweenRetries = 3000)
    {
        if (deviceManager.Get(visionDeviceName) is not IVisionDevice visionDevice) return;
        if (visionDevice.IsConnected()) return;
        var executablePath = visionDevice.GetInitArgument("ExecutablePath") as string;
        if (string.IsNullOrEmpty(executablePath)) return;

        Process.Start(new ProcessStartInfo
        {
            FileName = executablePath,
            WorkingDirectory = Path.GetDirectoryName(executablePath)
        });
        for (var i = 0; i < retryCount; i++)
        {
            visionDevice.Connect();
            if (visionDevice.IsConnected()) break;
            Thread.Sleep(delayBetweenRetries);
        }
    }
}