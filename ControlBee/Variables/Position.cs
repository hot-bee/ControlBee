﻿using System.Text.Json.Serialization;
using ControlBee.Interfaces;
using MathNet.Numerics.LinearAlgebra.Double;

namespace ControlBee.Variables;

public abstract class Position : IValueChanged, IActorItemSub
{
    private DenseVector _duplicatedVector = new(0);
    private DenseVector _vector = new(0);
    protected abstract int Rank { get; }

    private IAxis[] Axes => Actor.PositionAxesMap.Get(ItemName);

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
            OnValueChanged(new ValueChangedEventArgs(nameof(Vector), oldValue, value));
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
            OnValueChanged(new ValueChangedEventArgs(i, oldValue, value));
        }
    }

    [JsonIgnore]
    public string ItemName { get; set; } = string.Empty;

    [JsonIgnore]
    public IActor Actor { get; set; } = Models.Actor.Empty;

    public void UpdateSubItem() { }

    public event EventHandler<ValueChangedEventArgs>? ValueChanged;

    public void Move()
    {
        if (Axes == null)
            throw new ApplicationException();

        Move(Axes);
    }

    public void Move(IAxis[] axes)
    {
        if (Axes.Length == 0)
            throw new ApplicationException("No axis information is defined.");

        if (Axes.Length != Vector.Count)
            throw new ApplicationException(
                "Mismatch between the position dimension and the axis dimension. Please ensure they align."
            );

        for (var i = 0; i < Rank; i++)
            if (axes.Contains(Axes[i]))
                Axes[i].Move(Vector.Values[i]);
    }

    protected virtual void OnValueChanged(ValueChangedEventArgs e)
    {
        ValueChanged?.Invoke(this, e);
    }
}
