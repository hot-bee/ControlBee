using ControlBee.Models;
using ControlBee.Utils;
using JetBrains.Annotations;
using Xunit;

namespace ControlBee.Tests.Models;

[TestSubject(typeof(ActorItemInjectionDataSource))]
public class ActorItemInjectionDataSourceTest
{
    [Fact]
    public void ReadTest()
    {
        var dataSource = new ActorItemInjectionDataSource();
        dataSource.ReadFromString(
            @"
Picker0:
  PickupPosX:
    Name: Pickup Position X
    Unit: mm
    Desc: Please set the X-axis position for pickup.
  VacuumDet:
    IsOnTimeout:
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
            dataSource.GetValue("Picker0", "/VacuumDet/IsOnTimeout", "Name") as string
        );
        Assert.Null(dataSource.GetValue("Picker1", "/VacuumDet/IsOnTimeout", "Name") as string);
    }
}
