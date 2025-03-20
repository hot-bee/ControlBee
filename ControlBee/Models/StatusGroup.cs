namespace ControlBee.Models;

public class StatusGroup : IDisposable
{
    private readonly Actor _actor;

    public StatusGroup(Actor actor)
    {
        _actor = actor;
        _actor.PublishStepIn();
    }

    public void Dispose()
    {
        _actor.PublishStepOut();
    }
}
