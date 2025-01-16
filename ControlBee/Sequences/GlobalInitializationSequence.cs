using System.Reflection;
using ControlBee.Constants;
using ControlBee.Exceptions;
using ControlBee.Interfaces;
using ControlBee.Models;
using log4net;

namespace ControlBee.Sequences;

public class GlobalInitializationSequence(
    IActor actor,
    Action<GlobalInitializationSequence> runAction
)
{
    private static readonly ILog Logger = LogManager.GetLogger(
        MethodBase.GetCurrentMethod()!.DeclaringType!
    );

    private readonly Dictionary<IActor, InitializationStatus> _initializationState = new();

    public bool IsComplete =>
        _initializationState.All(x =>
            x.Value
                is InitializationStatus.Initialized
                    or InitializationStatus.Skipped
                    or InitializationStatus.Error
        );

    public bool IsInitializingActors =>
        _initializationState.Any(x => x.Value is InitializationStatus.Initializing);

    public bool IsError => _initializationState.Any(x => x.Value is InitializationStatus.Error);

    public void SetInitializationState(IActor initActor, InitializationStatus status)
    {
        _initializationState[initActor] = status;
    }

    private void Initialize(IActor initActor)
    {
        Logger.Info($"Initializing {initActor.ActorName}...");
        initActor.Send(new Message(actor, "_unReady"));
        initActor.Send(new Message(actor, "_initialize"));
        _initializationState[initActor] = InitializationStatus.Initializing;
    }

    public void InitializeIfPossible(IActor initActor)
    {
        if (_initializationState[initActor] == InitializationStatus.Uninitialized)
            Initialize(initActor);
    }

    public void Run()
    {
        if (IsInitializingActors)
            throw new PlatformException(
                "This operation cannot be performed while any actor is in the initializing state."
            );

        runAction(this);
    }
}
