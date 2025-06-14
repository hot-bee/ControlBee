using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Variables;
using log4net;
using Dict = System.Collections.Generic.Dictionary<string, object?>;

namespace ControlBee.Services;

public class VariableManager(IDatabase database, IActorRegistry actorRegistry, IUserInfo? userInfo)
    : IVariableManager,
        IDisposable
{
    private static readonly ILog Logger = LogManager.GetLogger("General");

    private readonly Dictionary<Tuple<string, string>, IVariable> _variables = [];
    private string _localName = "Default";

    private IActor? _uiActor;

    public VariableManager(IDatabase database)
        : this(database, EmptyActorRegistry.Instance, null)
    {
    }

    public VariableManager(IDatabase database, IActorRegistry actorRegistry)
        : this(database, actorRegistry, null)
    {
    }

    private IActor? UiActor
    {
        get
        {
            if (_uiActor != null)
                return _uiActor;
            if (actorRegistry == EmptyActorRegistry.Instance)
            {
                Logger.Warn("Skip getting UI Actor.");
                return _uiActor;
            }

            _uiActor = actorRegistry.Get("Ui");
            return _uiActor;
        }
    }

    public int Count => _variables.Count;

    public void Dispose()
    {
        foreach (var variable in _variables.Values)
            variable.ValueChanged -= Variable_ValueChanged;
    }

    public string LocalName
    {
        get => _localName;
        private set
        {
            if (string.IsNullOrEmpty(_localName))
                throw new ApplicationException(
                    "The 'LocalName' property cannot be left blank. Please enter a valid name."
                );
            _localName = value;
        }
    }

    public void Add(IVariable variable)
    {
        if (
            !_variables.TryAdd(
                new Tuple<string, string>(variable.ActorName, variable.ItemPath),
                variable
            )
        )
            throw new ApplicationException(
                "The 'ItemPath' is already being used by another variable."
            );
        variable.ValueChanged += Variable_ValueChanged;
        variable.UserInfo = userInfo;
    }

    public void Save(string? localName = null)
    {
        if (!string.IsNullOrEmpty(localName))
            LocalName = localName;
        foreach (var ((groupName, uid), variable) in _variables)
        {
            if (!variable.Dirty) continue;
            var jsonString = variable.ToJson();
            var dbLocalName = variable.Scope == VariableScope.Local ? LocalName : "";
            database.Write(variable.Scope, dbLocalName, groupName, uid, jsonString);
            variable.Dirty = false;
        }
    }

    public void Load(string? localName = null)
    {
        if (!string.IsNullOrEmpty(localName))
            LocalName = localName;
        foreach (var ((groupName, uid), variable) in _variables)
        {
            var dbLocalName = variable.Scope == VariableScope.Local ? LocalName : "";
            var jsonString = database.Read(dbLocalName, groupName, uid);
            if (jsonString != null)
            {
                variable.FromJson(jsonString);
                variable.Dirty = false;
            }
        }
    }

    private void Variable_ValueChanged(object? sender, ValueChangedArgs e)
    {
        var variable = (IVariable)sender!;
        var payload = new Dict { [nameof(ValueChangedArgs)] = e };
        UiActor?.Send(
            new ActorItemMessage(variable.Actor, variable.ItemPath, "_itemDataChanged", payload)
        );
    }
}