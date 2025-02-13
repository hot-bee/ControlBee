using ControlBee.Interfaces;

namespace ControlBee.Models;

public class DialogDisplay
{
    private readonly DialogViewFactory _dialogViewFactory;

    public DialogDisplay(IActorRegistry actorRegistry, DialogViewFactory dialogViewFactory)
    {
        _dialogViewFactory = dialogViewFactory;
        var ui = (IUiActor)actorRegistry.Get("ui")!;
        ui.MessageArrived += Ui_MessageArrived;
    }

    private void Ui_MessageArrived(object? sender, Message e)
    {
        if (e.Name != "_displayDialog")
            return;
        var context = (DialogContext)e.Payload!;
        var dialog = _dialogViewFactory.Create();
        dialog.ShowDialog(context, e);
    }
}
