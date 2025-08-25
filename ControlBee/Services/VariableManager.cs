using System.ComponentModel;
using System.Data;
using System.Runtime.CompilerServices;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Variables;
using log4net;
using Dict = System.Collections.Generic.Dictionary<string, object?>;

namespace ControlBee.Services;

public class VariableManager(
    IDatabase database,
    IActorRegistry actorRegistry,
    ISystemConfigurations systemConfigurations,
    IUserInfo? userInfo)
    : IVariableManager, IDisposable
{
    private static readonly ILog Logger = LogManager.GetLogger(nameof(VariableManager));

    private readonly Dictionary<Tuple<string, string>, IVariable> _variables = [];
    private bool _loading;
    private string _localName = "Default";

    private IActor? _uiActor;

    public VariableManager(IDatabase database, ISystemConfigurations systemConfigurations)
        : this(database, EmptyActorRegistry.Instance, systemConfigurations, null)
    {
    }

    public VariableManager(IDatabase database, IActorRegistry actorRegistry, ISystemConfigurations systemConfigurations)
        : this(database, actorRegistry, systemConfigurations, null)
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
            SetField(ref _localName, value);
        }
    }

    public string[] LocalNames => database.GetLocalNames();

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
        Logger.Info($"Save. ({localName})");
        var localNameChanged = false;
        if (!string.IsNullOrEmpty(localName))
        {
            localNameChanged = true;
            LocalName = localName;
        }

        foreach (var ((groupName, uid), variable) in _variables)
        {
            if (localNameChanged && variable.Scope != VariableScope.Local) continue;
            if (!(localNameChanged || variable.Dirty)) continue;
            variable.Dirty = false;
            var jsonString = variable.ToJson();
            var dbLocalName = variable.Scope == VariableScope.Local ? LocalName : "";
            var id = database.WriteVariables(variable.Scope, dbLocalName, groupName, uid, jsonString);
            variable.Id = id;
        }

        if (localNameChanged)
        {
            systemConfigurations.RecipeName = LocalName;
            OnPropertyChanged(nameof(LocalNames));
        }
    }

    public void Load(string? localName = null)
    {
        Logger.Info($"Load. ({localName})");
        _loading = true;
        try
        {
            var localNameChanged = false;
            if (!string.IsNullOrEmpty(localName))
            {
                localNameChanged = true;
                LocalName = localName;
            }

            foreach (var ((groupName, uid), variable) in _variables)
            {
                var dbLocalName = variable.Scope == VariableScope.Local ? LocalName : "";
                var row = database.Read(dbLocalName, groupName, uid);
                if (row.HasValue)
                {
                    variable.Id = row.Value.id;
                    variable.FromJson(row.Value.value);
                    variable.Dirty = false;
                }
            }

            if (localNameChanged) systemConfigurations.RecipeName = LocalName;
        }
        finally
        {
            _loading = false;
        }
    }

    public void Delete(string localName)
    {
        database.DeleteLocal(localName);
        OnPropertyChanged(nameof(LocalNames));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public DataTable ReadVariableChanges()
    {
        return database.ReadVariableChanges();
    }

    private void Variable_ValueChanged(object? sender, ValueChangedArgs e)
    {
        var variable = (IVariable)sender!;
        var payload = new Dict { [nameof(ValueChangedArgs)] = e };
        UiActor?.Send(
            new ActorItemMessage(variable.Actor, variable.ItemPath, "_itemDataChanged", payload)
        );

        if (variable.Scope != VariableScope.Temporary && !_loading)
        {
            if (variable.Id == null || variable.Dirty) Save();
            if (variable.Id == null) Logger.Warn("Variable Id is 0 even after saving it.");
            database.WriteVariableChange(variable, e);
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}