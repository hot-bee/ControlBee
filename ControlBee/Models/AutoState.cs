using ControlBee.Interfaces;
using log4net;

namespace ControlBee.Models;

public class AutoState<T>(T actor, IActor parent) : State<T>(actor)
    where T : Actor
{
    private static readonly ILog Logger = LogManager.GetLogger("Sequence");

    public override void Dispose()
    {
        base.Dispose();
        Actor.SetStatus("_auto", false);
        Logger.Info("Finished auto state.");
    }

    public override bool ProcessMessage(Message message)
    {
        switch (message.Name)
        {
            case StateEntryMessage.MessageName:
                Logger.Info("Start auto state.");
                Actor.SetStatus("_auto", true);
                Scan();
                return true;
            case "_status":
                return Scan();
        }

        return false;
    }

    public virtual bool Scan()
    {
        if (Actor.GetPeerStatus(parent, "_auto") is not true)
        {
            Logger.Debug("Parent actor is not in auto mode.");
            Actor.State = Actor.CreateIdleState();
            return true;
        }

        return false;
    }
}
