using System.Text.Json.Serialization;
using ControlBee.Constants;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBeeAbstract.Exceptions;
using log4net;
using MathNet.Numerics.LinearAlgebra.Double;

namespace ControlBee.Variables;

public abstract class Position : IValueChanged, IActorItemSub, IWriteData, IIndex1D
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

    private IAxis[] Axes => Actor.PositionAxesMap.Get(ItemPath);

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
            InternalVector = value;
            _duplicatedVector = value;
            OnValueChanged(new ValueChangedArgs([nameof(Vector)], oldValue, value));
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
            newVector[i] = value;
            InternalVector = newVector;
            OnValueChanged(new ValueChangedArgs([i], oldValue, value));
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
                MoveToSavedPos();
                return true;
            case "MoveToHomePos":
                MoveToHomePos();
                return true;
            case "SavePos":
                SavePos();
                return true;
        }

        return false;
    }

    public event EventHandler<ValueChangedArgs>? ValueChanged;

    public void WriteData(ItemDataWriteArgs args)
    {
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

    public object? GetValue(int index)
    {
        return this[index];
    }

    public void MoveToHomePos()
    {
        foreach (var axis in Axes)
        {
            axis.SetSpeed(axis.GetJogSpeed(JogSpeed.Slow));
            axis.GetHomePos().Move();
        }
        foreach (var axis in Axes)
        {
            axis.GetHomePos().Wait();
        }
    }

    public void MoveToSavedPos()
    {
        foreach (var axis in Axes)
            axis.SetSpeed(axis.GetJogSpeed(JogSpeed.Slow));
        MoveAndWait();
    }

    public void SavePos()
    {
        for (var i = 0; i < Axes.Length; i++)
        {
            var pos = Axes[i].GetPosition();
            this[i] = pos;
        }
    }
}
