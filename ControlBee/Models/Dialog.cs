using ControlBee.Constants;
using ControlBee.Interfaces;
using ControlBee.Services;
using log4net;

namespace ControlBee.Models;

public class Dialog(DialogContextFactory dialogContextFactory, IEventManager eventManager)
    : ActorItem,
        IDialog
{
    private static readonly ILog Logger = LogManager.GetLogger("General");

    protected IDialogContext Context = dialogContextFactory.Create();

    public override void Init()
    {
        base.Init();
        Context.ActorName = ActorName;
        Context.ItemPath = ItemPath;
    }

    public virtual Guid Show()
    {
        return Show(null, null);
    }

    public virtual Guid Show(string[] actionButtons)
    {
        return Show(actionButtons, null);
    }

    public virtual Guid Show(string desc)
    {
        return Show(null, desc);
    }

    public override void InjectProperties(ISystemPropertiesDataSource dataSource)
    {
        base.InjectProperties(dataSource);
        Context.Name = Name;
        Context.Desc = Desc;
        if (
            dataSource.GetValue(ActorName, ItemPath, nameof(DialogContext.Code)) is string codeValue
        )
        {
            if (int.TryParse(codeValue, out var code))
                Context.Code = code;
            else
                Logger.Error($"Failed to parse Code ({codeValue})");
        }

        if (
            dataSource.GetValue(ActorName, ItemPath, nameof(DialogContext.Severity))
            is string severityValue
        )
        {
            if (Enum.TryParse<DialogSeverity>(severityValue, out var severity))
                Context.Severity = severity;
            else
                Logger.Error($"Failed to parse DialogSeverity ({severityValue})");
        }
    }

    public virtual Guid Show(string[]? actionButtons, string? desc)
    {
        if (actionButtons != null)
            Context.ActionButtons = actionButtons;
        if (desc != null)
            Context.Desc = desc;
        eventManager.Write(
            Context.ActorName,
            Context.ItemPath,
            Context.Name,
            Context.Severity,
            Context.Code,
            Context.Desc
        );
        return Actor.Ui?.Send(new Message(Actor, "_displayDialog", Context)) ?? Guid.Empty;
    }

    public virtual void Close()
    {
        Actor.Ui?.Send(new Message(Actor, "_closeDialog", Context));
    }
}
