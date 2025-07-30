using ControlBee.Interfaces;

namespace ControlBee.Models;

public class DialogDisplay
{
    private readonly DialogViewFactory _dialogViewFactory;

    private readonly HashSet<DialogContext> _onContexts = [];

    public DialogDisplay(IActorRegistry actorRegistry, DialogViewFactory dialogViewFactory)
    {
        _dialogViewFactory = dialogViewFactory;
        var ui = (IUiActor)actorRegistry.Get("Ui")!;
        ui.MessageArrived += Ui_MessageArrived;
    }

    private void Ui_MessageArrived(object? sender, Message e)
    {
        if (e.Name != "_displayDialog")
            return;
        var context = (DialogContext)e.Payload!;
        if (_onContexts.Contains(context)) return;
        var dialog = _dialogViewFactory.Create();
        dialog.Show(context, e);
        _onContexts.Add(context);
        dialog.DialogClosed += (o, args) => { _onContexts.Remove(context); };
    }
}