# Control Bee

The world's first open-source platform for machine control in manufacturing fields such as semiconductors, secondary batteries, and solar power.

## What is ControlBee?

ControlBee is a .NET framework for building deterministic, sequential control software for industrial equipment (pick-and-place machines, assembly lines, semiconductor/battery/solar fab tools, etc.). It provides a unified, type-safe API over the kind of hardware these machines are typically built from — motion axes, digital/analog I/O, sensors — so control logic can be written the same way regardless of which vendor's hardware sits underneath.

### Core concepts

- **Actor** (`IActor`) — the central unit of control logic, modeled after the actor pattern popularized by [Akka](https://akka.io/). Each actor runs on its own thread, processes messages from a mailbox one at a time, and manages its state as a stack of `IState` instances (supporting nested states). Typically, one actor orchestrates one physical machine or subsystem.
- **Devices** (`IDevice` and friends, in `DeviceBase`) — a hardware abstraction layer with minimal dependencies, meant to be implemented by hardware vendors or integrators. Covers motion controllers (`IMotionDevice`), digital I/O (`IDigitalIoDevice`), and analog I/O (`IAnalogIoDevice`).
- **Control primitives** — higher-level, actor-owned items built on top of devices:
  - `IAxis` — motion control with software limits, homing sequences (home/limit/Z-phase sensors), and both absolute/relative and velocity moves.
  - `IDigitalInput` / `IDigitalOutput` — binary signals with state queries and wait operations.
  - `IAnalogInput` / `IAnalogOutput` — analog values.
  - `IBinaryActuator` — a higher-level device combining input detection with output control.
- **Variables** (`IVariable<T>`) — strongly-typed, scoped (global/local) state that can persist across sessions via a built-in SQLite store. Includes `Position1D`–`Position4D` for coordinating multiple axes and `SpeedProfile` for motion parameters (velocity, acceleration, jerk).
- **Dialogs** (`IDialog`) — a UI-framework-agnostic way for control logic to pause and request operator input (e.g., on a sensor timeout or alarm), leaving the actual UI implementation up to the integrator.

### Typical usage

1. Subclass `Actor` and declare its owned items — axes, I/O, variables — as properties.
2. Override the actor's message handler to implement the machine's control sequence (e.g., home an axis, wait for a sensor, move to a position, toggle an output).
3. Start the actor; it consumes messages from its mailbox and drives the state machine, blocking on motion (`MoveAndWait`) where the sequence is inherently procedural.
4. Implement `DeviceBase` interfaces against the real hardware SDK (or use a fake/simulated device for testing), and wire them into the actor's device pool.

See the [Examples](#examples) repository for complete, runnable machine definitions.

## Authors

[Leo Younghyo Kim](mailto:leo@hotbee.co.kr)

## License

This project is licensed under the MIT License - see the LICENSE.md file for details

## Documentation

https://controlbee.hotbee.ai/

## Examples

https://github.com/hot-bee/ControlBeeExamples

## Acknowledgments

Inspiration, code snippets, etc.
* [Akka](https://akka.io/)

Hello world from Buzz
