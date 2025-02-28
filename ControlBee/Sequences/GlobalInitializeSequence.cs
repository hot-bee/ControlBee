using System.Reflection;
using ControlBee.Constants;
using ControlBee.Exceptions;
using ControlBee.Interfaces;
using ControlBee.Models;
using log4net;

namespace ControlBee.Sequences;

public class GlobalInitializeSequence
{
    private static readonly ILog Logger = LogManager.GetLogger("General");

    private readonly IActor _actor;

    private readonly Dictionary<IActor, InitializationStatus> _initializationState = new();
    private readonly Action<GlobalInitializeSequence> _runAction;

    public GlobalInitializeSequence(
        IActor actor,
        Action<GlobalInitializeSequence> runAction,
        IEnumerable<IActor> initializingActors
    )
    {
        _actor = actor;
        _runAction = runAction;
        foreach (var initializingActor in initializingActors)
            SetInitializationState(initializingActor, InitializationStatus.Uninitialized);
    }

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
    public event EventHandler<(string actorName, InitializationStatus status)>? StateChanged;

    public void SetInitializationState(IActor initActor, InitializationStatus status)
    {
        _initializationState[initActor] = status;
        OnStateChanged((initActor.Name, status));
    }

    private void Initialize(IActor initActor)
    {
        Logger.Info($"Initializing {initActor.Name}...");
        initActor.Send(new Message(_actor, "_resetState"));
        initActor.Send(new Message(_actor, "_initialize"));
        SetInitializationState(initActor, InitializationStatus.Initializing);
    }

    public void InitializeIfPossible(IActor initActor)
    {
        if (!_initializationState.TryGetValue(initActor, out var value))
            return;
        if (value == InitializationStatus.Uninitialized)
            Initialize(initActor);
    }

    public void Run()
    {
        if (IsInitializingActors)
            throw new PlatformException(
                "This operation cannot be performed while any actor is in the initializing state."
            );

        _runAction(this);
    }

    protected virtual void OnStateChanged((string actorName, InitializationStatus status) e)
    {
        StateChanged?.Invoke(this, e);
    }
}
