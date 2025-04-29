using ControlBee.Constants;
using ControlBee.Interfaces;
using ControlBee.Services;
using log4net;

namespace ControlBee.Models;

public class Dialog(DialogContextFactory dialogContextFactory, IEventManager eventManager) : ActorItem, IDialog
{
    private static readonly ILog Logger = LogManager.GetLogger("General");

    protected IDialogContext Context = dialogContextFactory.Create();

    public override void Init()
    {
        base.Init();
        Context.ActorName = ActorName;
    }

    public Guid Show()
    {
        eventManager.Write(
            Context.ActorName,
            Context.Code?.ToString() ?? string.Empty,
            Context.Name,
            Context.Desc,
            Context.Severity.ToString()
            );

        return Show([]);
    }

    public Guid Show(string[] actionButtons)
    {
        Context.ActionButtons = actionButtons;
        return Actor.Ui?.Send(new Message(Actor, "_displayDialog", Context)) ?? Guid.Empty;
    }

    public Guid Show(string desc)
    {
        Context.Desc = desc;
        return Actor.Ui?.Send(new Message(Actor, "_displayDialog", Context)) ?? Guid.Empty;
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
}