using ControlBee.Models;

namespace ControlBee.Interfaces;

public interface IDialogView
{
    void ShowDialog(IDialogContext context, Message message);
}
