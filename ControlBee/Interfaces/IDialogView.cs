using ControlBee.Models;

namespace ControlBee.Interfaces;

public interface IDialogView
{
    void Show(IDialogContext context, Message message);
}
