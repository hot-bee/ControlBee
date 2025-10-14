using ControlBee.Interfaces;

namespace ControlBee.Models;

public class DialogDisplay
{
    private readonly DialogViewFactory _dialogViewFactory;

    private readonly HashSet<IDialogContext> _onContexts = [];

    public DialogDisplay(IActorRegistry actorRegistry, DialogViewFactory dialogViewFactory)
    {
        _dialogViewFactory = dialogViewFactory;
        var ui = (IUiActor)actorRegistry.Get("Ui")!;
        ui.MessageArrived += Ui_MessageArrived;
    }

    private void Ui_MessageArrived(object? sender, Message e)
    {
        switch (e.Name)
        {
            case "_displayDialog":
            {
                var context = (IDialogContext)e.Payload!;
                if (_onContexts.Contains(context)) return;
                var dialog = _dialogViewFactory.Create();
                dialog.Show(context, e);
                _onContexts.Add(context);
                dialog.DialogClosed += (o, args) => { _onContexts.Remove(context); };
                break;
            }
            case "_closeDialog":
            {
                var context = (IDialogContext)e.Payload!;
                if (!_onContexts.Contains(context)) return;
                context.Close();
                break;
            }
        }
    }
}