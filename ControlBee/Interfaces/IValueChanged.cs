﻿using ControlBee.Variables;

namespace ControlBee.Interfaces;

public interface IValueChanged
{
    public event EventHandler<ValueChangedArgs> ValueChanged;
}
