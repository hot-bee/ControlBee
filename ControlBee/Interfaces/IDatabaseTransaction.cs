namespace ControlBee.Interfaces;

public interface IDatabaseTransaction : IDisposable
{
    void Commit();
}
