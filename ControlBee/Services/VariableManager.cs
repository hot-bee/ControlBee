using System.ComponentModel;
using System.Data;
using System.Runtime.CompilerServices;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Variables;
using ControlBeeAbstract.Devices;
using log4net;
using Dict = System.Collections.Generic.Dictionary<string, object?>;

namespace ControlBee.Services;

public class VariableManager(
    IDatabase database,
    IActorRegistry actorRegistry,
    ISystemConfigurations systemConfigurations,
    IDeviceManager deviceManager,
    IUserInfo? userInfo)
    : IVariableManager, IDisposable
{
    private static readonly ILog Logger = LogManager.GetLogger(nameof(VariableManager));
    private readonly List<(IVariable variable, ValueChangedArgs args)> _changedArgs = [];

    private readonly Dictionary<Tuple<string, string>, IVariable> _variables = [];
    private bool _loading;
    private string _localName = "Default";
    private bool _modified;
    private IActor? _uiActor;

    public VariableManager(IDatabase database, ISystemConfigurations systemConfigurations, IDeviceManager deviceManager)
        : this(database, EmptyActorRegistry.Instance, systemConfigurations, deviceManager, null)
    {
    }

    public VariableManager(IDatabase database, IActorRegistry actorRegistry,
        ISystemConfigurations systemConfigurations, IDeviceManager deviceManager)
        : this(database, actorRegistry, systemConfigurations, deviceManager, null)
    {
    }

    public bool Modified
    {
        get => _modified;
        private set => SetField(ref _modified, value);
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
        {
            variable.ValueChanging -= VariableOnValueChanging;
            variable.ValueChanged -= VariableOnValueChanged;
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
        variable.ValueChanging += VariableOnValueChanging;
        variable.ValueChanged += VariableOnValueChanged;
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

        foreach (var ((actorName, uid), variable) in _variables)
        {
            if (localNameChanged && variable.Scope != VariableScope.Local) continue;
            if (!(localNameChanged || variable.Dirty)) continue;
            Save(actorName, uid, variable);
        }

        foreach (var (variable, args) in _changedArgs)
            database.WriteVariableChange(variable, args);
        _changedArgs.Clear();
        Modified = false;

        if (localNameChanged)
        {
            systemConfigurations.RecipeName = LocalName;
            systemConfigurations.Save();
            OnPropertyChanged(nameof(LocalNames));
            SaveVisionRecipe(LocalName);
        }
    }

    public void Load(string? localName = null)
    {
        Logger.Info($"Load. ({localName})");
        DiscardChanges();
        _loading = true;
        try
        {
            var localNameChanged = false;
            if (!string.IsNullOrEmpty(localName))
            {
                localNameChanged = true;
                LocalName = localName;
            }

            foreach (var ((actorName, uid), variable) in _variables)
                Load(actorName, uid, variable);

            if (localNameChanged)
            {
                systemConfigurations.RecipeName = LocalName;
                systemConfigurations.Save();
                LoadVisionRecipe(LocalName);
            }
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

    public void SaveTemporaryVariables()
    {
        Logger.Info("SaveTemporary.");
        foreach (var ((actorName, uid), variable) in _variables)
        {
            if (variable.Scope != VariableScope.Temporary || !variable.Dirty) continue;
            Save(actorName, uid, variable);
        }
    }

    public void DiscardChanges()
    {
        Logger.Info("DiscardChanges.");
        foreach (var ((actorName, uid), variable) in _variables)
        {
            if (variable.Scope == VariableScope.Temporary) continue;
            if (!variable.Dirty) continue;
            Load(actorName, uid, variable);
        }
        _changedArgs.Clear();
        Modified = false;
    }

    public void Save(IVariable variableToSave)
    {
        foreach (var ((actorName, uid), variable) in _variables)
            if (variable == variableToSave)
            {
                Save(actorName, uid, variable);
                return;
            }

        Logger.Warn("Couldn't find the variable in _variables in Save().");
    }

    private void Load(IVariable variableToLoad)
    {
        foreach (var ((actorName, uid), variable) in _variables)
            if (variable == variableToLoad)
            {
                Load(actorName, uid, variable);
                return;
            }

        Logger.Warn("Couldn't find the variable in _variables in Load().");
    }

    private void Save(string actorName, string uid, IVariable variable)
    {
        variable.Dirty = false;
        var jsonString = variable.ToJson();
        var dbLocalName = variable.Scope == VariableScope.Local ? LocalName : "";
        var id = database.WriteVariables(variable.Scope, dbLocalName, actorName, uid, jsonString);
        variable.Id = id;
    }

    private void Load(string actorName, string uid, IVariable variable)
    {
        var dbLocalName = variable.Scope == VariableScope.Local ? LocalName : "";
        var row = database.Read(dbLocalName, actorName, uid);
        if (!row.HasValue)
        {
            Save(actorName, uid, variable);
            return;
        }

        variable.Id = row.Value.id;
        variable.FromJson(row.Value.value);
        variable.Dirty = false;
    }

    private void LoadVisionRecipe(string localName)
    {
        foreach (var device in deviceManager.GetDevices())
            if (device is IVisionDevice visionDevice)
                visionDevice.LoadRecipe(localName);
    }

    private void SaveVisionRecipe(string localName)
    {
        foreach (var device in deviceManager.GetDevices())
            if (device is IVisionDevice visionDevice)
                visionDevice.SaveRecipe(localName);
    }

    private void VariableOnValueChanging(object? sender, ValueChangedArgs e)
    {
        // Empty
    }

    private void VariableOnValueChanged(object? sender, ValueChangedArgs e)
    {
        var variable = (IVariable)sender!;
        var payload = new Dict { [nameof(ValueChangedArgs)] = e };
        UiActor?.Send(
            new ActorItemMessage(variable.Actor, variable.ItemPath, "_itemDataChanged", payload)
        );

        if (variable.Scope != VariableScope.Temporary && !_loading)
        {
            if (systemConfigurations.AutoVariableSave)
            {
                Save(variable);
                database.WriteVariableChange(variable, e);
            }
            else
            {
                _changedArgs.Add((variable, e));
                Modified = true;
            }
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