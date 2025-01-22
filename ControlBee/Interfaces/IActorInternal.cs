namespace ControlBee.Interfaces;

public interface IActorInternal : IActor
{
    IVariableManager VariableManager { get; }
    IPositionAxesMap PositionAxesMap { get; }
    ITimeManager TimeManager { get; }
    IActor? Ui { get; }
    void Init();
}
