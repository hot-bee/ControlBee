using System.Reflection;
using ControlBee.Exceptions;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Variables;
using log4net;

namespace ControlBee.Services;

public class VariableManager(IDatabase database, IActorRegistry actorRegistry)
    : IVariableManager,
        IDisposable
{
    private static readonly ILog Logger = LogManager.GetLogger("General");

    private readonly Dictionary<Tuple<string, string>, IVariable> _variables = [];
    private string _localName = "Default";

    private IActor? _uiActor;

    public VariableManager(IDatabase database)
        : this(database, EmptyActorRegistry.Instance) { }

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
            _uiActor = actorRegistry.Get("ui");
            return _uiActor;
        }
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

    public int Count => _variables.Count;

    public void Dispose()
    {
        foreach (var variable in _variables.Values)
            variable.ValueChanged -= Variable_ValueChanged;
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
    }

    public void Save(string? localName = null)
    {
        if (!string.IsNullOrEmpty(localName))
            LocalName = localName;
        foreach (var ((groupName, uid), variable) in _variables)
        {
            var jsonString = variable.ToJson();
            var dbLocalName = variable.Scope == VariableScope.Local ? LocalName : "";
            database.Write(variable.Scope, dbLocalName, groupName, uid, jsonString);
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
                variable.FromJson(jsonString);
        }
    }

    private void Variable_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        var variable = (IVariable)sender!;
        var payload = new Dictionary<string, object?>
        {
            ["Location"] = e.Location,
            ["OldValue"] = e.OldValue,
            ["NewValue"] = e.NewValue,
        };
        UiActor?.Send(
            new ActorItemMessage(variable.Actor, variable.ItemPath, "_itemDataChanged", payload)
        );
    }
}
