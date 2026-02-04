using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Utils;
using JetBrains.Annotations;
using Moq;
using Xunit;

namespace ControlBee.Tests.Models;

[TestSubject(typeof(SystemPropertiesDataSource))]
public class SystemPropertiesDataSourceTest
{
    [Fact]
    public void ReadTest()
    {
        var systemConfigurations = new SystemConfigurations();
        var localizationManager = Mock.Of<ILocalizationManager>();
        var dataSource = new SystemPropertiesDataSource(systemConfigurations, localizationManager);
        dataSource.ReadFromString(
            @"
Picker0:
  PickupPosX:
    Name: Pickup Position X
    Unit: mm
    Desc: Please set the X-axis position for pickup.
  VacuumDet:
    OnTimeoutError:
      Name: Timeout while waiting the sensor On

    Name: Vacuum Detect
Picker1:


"
        );
        Assert.Equal(
            "Pickup Position X",
            dataSource.GetValue("Picker0", "PickupPosX", "Name") as string
        );
        Assert.Equal("mm", dataSource.GetValue("Picker0", "PickupPosX", "Unit") as string);
        Assert.Equal("mm", dataSource.GetValue("Picker0", "/PickupPosX", "Unit") as string);
        Assert.Equal(
            "Vacuum Detect",
            dataSource.GetValue("Picker0", "/VacuumDet", "Name") as string
        );
        Assert.Equal(
            "Timeout while waiting the sensor On",
            dataSource.GetValue("Picker0", "/VacuumDet/OnTimeoutError", "Name") as string
        );
        Assert.Null(dataSource.GetValue("Picker1", "/VacuumDet/OnTimeoutError", "Name") as string);
    }
}
