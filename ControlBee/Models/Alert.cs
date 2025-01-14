using System.Dynamic;
using ControlBee.Interfaces;

namespace ControlBee.Models;

public class Alert : DialogItem
{
    public override void Trigger()
    {
        dynamic payload = new ExpandoObject();
        payload.name = "requestDialog";
        payload.context = new DialogContext();
        Actor.Ui.Send(new Message(Actor, payload));
    }

    public override void ProcessMessage(Message message)
    {
        throw new NotImplementedException();
    }

    public override void UpdateSubItem()
    {
        // TODO
    }
}
