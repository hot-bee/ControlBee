using System.Text.Json.Serialization;
using ControlBee.Constants;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBeeAbstract.Exceptions;
using log4net;
using MathNet.Numerics.LinearAlgebra.Double;

namespace ControlBee.Variables;

public abstract class Position
    : INotifyValueChanged,
        IActorItemSub,
        IWriteData,
        IIndex1D,
        ICloneable
{
    private static readonly ILog Logger = LogManager.GetLogger("Position");
    private DenseVector _duplicatedVector = new(0);
    private DenseVector _vector = new(0);

    protected Position() { }

    protected Position(DenseVector vector)
        : this()
    {
        // ReSharper disable once VirtualMemberCallInConstructor
        if (vector.Count != Rank)
            throw new PlatformException();
        InternalVector = DenseVector.OfVector(vector);
    }

    protected abstract int Rank { get; }

    [JsonIgnore]
    public IAxis[] Axes => Actor.PositionAxesMap.Get(ItemPath);

    protected DenseVector InternalVector
    {
        get
        {
            if (!_vector.SequenceEqual(_duplicatedVector))
                throw new ApplicationException(
                    "Please modify the vector value using the Position class index, not directly through the Vector property."
                );
            return _vector;
        }
        set
        {
            _vector = DenseVector.OfVector(value);
            _duplicatedVector = DenseVector.OfVector(value);
        }
    }

    [JsonIgnore]
    public DenseVector Vector
    {
        get => InternalVector;
        set
        {
            var oldValue = Vector;
            var valueChangedArgs = new ValueChangedArgs([nameof(Vector)], oldValue, value);
            OnValueChanging(valueChangedArgs);
            InternalVector = value;
            _duplicatedVector = value;
            OnValueChanged(valueChangedArgs);
        }
    }

    public double[] Values
    {
        get => Vector.Values;
        set => Vector = DenseVector.OfArray(value);
    }

    public double this[int i]
    {
        get => InternalVector[i];
        set
        {
            var newVector = DenseVector.OfVector(InternalVector);
            var oldValue = newVector[i];
            var valueChangedArgs = new ValueChangedArgs([i], oldValue, value);
            OnValueChanging(valueChangedArgs);
            newVector[i] = value;
            InternalVector = newVector;
            OnValueChanged(valueChangedArgs);
        }
    }

    [JsonIgnore]
    public string ItemPath { get; set; } = string.Empty;

    [JsonIgnore]
    public IActorInternal Actor { get; set; } = EmptyActor.Instance;

    public void UpdateSubItem() { }

    public virtual void OnDeserialized() { }

    public bool ProcessMessage(ActorItemMessage message)
    {
        switch (message.Name)
        {
            case "MoveToSavedPos":
                MoveToSavedPos(message);
                return true;
            case "MoveToHomePos":
                MoveToHomePos();
                return true;
            case "SetPos":
                SetPos();
                return true;
        }

        return false;
    }

    public event EventHandler<ValueChangedArgs>? ValueChanging;
    public event EventHandler<ValueChangedArgs>? ValueChanged;

    public void WriteData(ItemDataWriteArgs args)
    {
        args.EnsureNewValueInRange();
        var index = (int)args.Location[0];
        this[index] = (double)args.NewValue;
        if (args.Location.Length > 1)
            Logger.Warn("Location arguments too many.");
    }

    public void Move()
    {
        Move(Axes, false);
    }

    public void Move(bool @override)
    {
        if (Axes == null)
            throw new ApplicationException();
        Move(Axes, @override);
    }

    public void Stop()
    {
        foreach (var axis in Axes)
            axis.Stop();
    }

    public void Wait()
    {
        if (Axes == null)
            throw new ApplicationException();
        Wait(Axes);
    }

    public void MoveAndWait()
    {
        Move();
        Wait();
    }

    public void WaitForPosition(PositionComparisonType type)
    {
        foreach (var (axis, position) in Axes.Zip(_vector.Values))
            axis.WaitForPosition(type, position);
    }

    public bool IsNear(double range)
    {
        foreach (var (axis, position) in Axes.Zip(_vector.Values))
            if (!axis.IsNear(position, range))
                return false;
        return true;
    }

    public void Move(IAxis[] axes)
    {
        Move(axes, false);
    }

    public void Move(IAxis[] axes, bool @override)
    {
        if (Axes.Length == 0)
            throw new ApplicationException("No axis information is defined.");

        if (Axes.Length != Vector.Count)
            throw new ApplicationException(
                "Mismatch between the position dimension and the axis dimension. Please ensure they align."
            );

        for (var i = 0; i < Rank; i++)
            if (axes.Contains(Axes[i]))
                Axes[i].Move(Vector.Values[i], @override);
    }

    public void Wait(IAxis[] axes)
    {
        for (var i = 0; i < Rank; i++)
            if (axes.Contains(Axes[i]))
                Axes[i].Wait();
    }

    public void MoveAndWait(IAxis[] axes)
    {
        Move(axes);
        Wait(axes);
    }

    protected virtual void OnValueChanged(ValueChangedArgs e)
    {
        ValueChanged?.Invoke(this, e);
    }

    [JsonIgnore]
    public int Size => InternalVector.Count;

    public object? GetValue(int index)
    {
        return this[index];
    }

    public void SetValue(int index, object value)
    {
        this[index] = (double)value;
    }

    public void MoveToHomePos()
    {
        for (var i = Axes.Length - 1; i >= 0; i--)
        {
            Axes[i].SetSpeed(Axes[i].GetJogSpeed(JogSpeedLevel.Fast));
            Axes[i].GetInitPos().MoveAndWait();
        }
    }

    public void MoveToSavedPos(ActorItemMessage? message = null)
    {
        double[]? speedProfiles = null;
        if (message?.DictPayload?.TryGetValue("Speed", out var speedValue) == true)
        {
            speedProfiles = speedValue as double[];
        }

        for (var i = 0; i < Axes.Length; i++)
        {
            if (speedProfiles != null)
            {
                var speedProfile = (SpeedProfile)Axes[i].GetNormalSpeed().Clone();
                speedProfile.Velocity = speedProfiles[i];
                Axes[i].SetSpeed(speedProfile);
            }
            else
            {
                Axes[i].SetSpeed(Axes[i].GetJogSpeed(JogSpeedLevel.Fast));
            }
            Axes[i].MoveAndWait(this[i]);
        }
    }

    public void SetPos()
    {
        for (var i = 0; i < Axes.Length; i++)
        {
            var pos = Axes[i].GetPosition();
            this[i] = pos;
        }
    }

    protected virtual void OnValueChanging(ValueChangedArgs e)
    {
        ValueChanging?.Invoke(this, e);
    }

    public abstract object Clone();
}
