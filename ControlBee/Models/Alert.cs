using ControlBee.Interfaces;

namespace ControlBee.Models;

public class Alert : DialogItem
{
    public override void Trigger()
    {
        Actor.Ui.Send(new Message(Actor, "_requestDialog", new DialogContext()));
    }

    public override bool ProcessMessage(ActorItemMessage message)
    {
        throw new NotImplementedException();
    }

    public override void UpdateSubItem()
    {
        // TODO
    }

    public override void InjectProperties(IActorItemInjectionDataSource dataSource)
    {
        // TODO
    }
}
