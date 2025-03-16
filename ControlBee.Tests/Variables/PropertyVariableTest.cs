using System;
using System.Linq;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Utils;
using ControlBee.Variables;
using ControlBeeTest.Utils;
using JetBrains.Annotations;
using Moq;
using Xunit;

namespace ControlBee.Tests.Variables;

[TestSubject(typeof(Variable<>))]
public class PropertyVariableTest : ActorFactoryBase
{
    [Fact]
    public void DataReadTest()
    {
        var sendMock = new SendMock();
        var uiActor = Mock.Of<IUiActor>();
        Mock.Get(uiActor).Setup(m => m.Name).Returns("Ui");
        ActorRegistry.Add(uiActor);
        var actor = ActorFactory.Create<TestActor>("MyActor");

        sendMock.SetupActionOnMessage(
            actor,
            uiActor,
            "_itemDataChanged",
            message =>
            {
                var valueChangedArgs =
                    message.DictPayload![nameof(ValueChangedArgs)] as ValueChangedArgs;
                var newValue = (Product)valueChangedArgs!.NewValue!;
                Assert.False(newValue.Exists);
                actor.Send(new TerminateMessage());
            }
        );
        actor.Send(new ActorItemMessage(uiActor, "/Product", "_itemDataRead"));

        actor.Start();
        actor.Join();
    }

    [Fact]
    public void DataWriteTest()
    {
        var sendMock = new SendMock();
        var uiActor = Mock.Of<IUiActor>();
        Mock.Get(uiActor).Setup(m => m.Name).Returns("Ui");
        ActorRegistry.Add(uiActor);
        var actor = ActorFactory.Create<TestActor>("MyActor");

        sendMock.SetupActionOnMessage(
            actor,
            uiActor,
            "_itemDataChanged",
            message =>
            {
                var valueChangedArgs =
                    message.DictPayload![nameof(ValueChangedArgs)] as ValueChangedArgs;
                var newValue = (Product)valueChangedArgs!.NewValue!;
                Assert.True(newValue.Exists);
                actor.Send(new TerminateMessage());
            }
        );
        actor.Send(
            new ActorItemMessage(
                uiActor,
                "/Product",
                "_itemDataWrite",
                new ItemDataWriteArgs([], new Product { Exists = true })
            )
        );

        actor.Start();
        actor.Join();
    }

    [Fact]
    public void DataChangedTest()
    {
        var sendMock = new SendMock();
        var uiActor = Mock.Of<IUiActor>();
        Mock.Get(uiActor).Setup(m => m.Name).Returns("Ui");
        ActorRegistry.Add(uiActor);
        var actor = ActorFactory.Create<TestActor>("MyActor");

        sendMock.SetupActionOnMessage(
            actor,
            uiActor,
            "_itemDataChanged",
            message =>
            {
                var valueChangedArgs =
                    message.DictPayload![nameof(ValueChangedArgs)] as ValueChangedArgs;
                Assert.True(valueChangedArgs?.Location.SequenceEqual(["Exists"]));
                Assert.True(valueChangedArgs?.NewValue is true);
                actor.Send(new TerminateMessage());
            }
        );
        actor.Send(new Message(uiActor, "ChangeData"));

        actor.Start();
        actor.Join();
    }

    [Fact]
    public void ItemDataWriteTest()
    {
        var sendMock = new SendMock();
        var uiActor = Mock.Of<IUiActor>();
        Mock.Get(uiActor).Setup(m => m.Name).Returns("Ui");
        ActorRegistry.Add(uiActor);
        var actor = ActorFactory.Create<TestActor>("MyActor");

        sendMock.SetupActionOnMessage(
            actor,
            uiActor,
            "_itemDataChanged",
            message =>
            {
                var valueChangedArgs =
                    message.DictPayload![nameof(ValueChangedArgs)] as ValueChangedArgs;
                var location = valueChangedArgs!.Location;
                var newValue = (bool)valueChangedArgs.NewValue!;
                Assert.True(location.SequenceEqual(["Exists"]));
                Assert.True(newValue);
                actor.Send(new TerminateMessage());
            }
        );
        actor.Send(
            new ActorItemMessage(
                uiActor,
                "/Product",
                "_itemDataWrite",
                new ItemDataWriteArgs(["Exists"], true)
            )
        );

        actor.Start();
        actor.Join();
    }

    [Fact]
    public void DeepItemDataWriteTest()
    {
        var sendMock = new SendMock();
        var uiActor = Mock.Of<IUiActor>();
        Mock.Get(uiActor).Setup(m => m.Name).Returns("Ui");
        ActorRegistry.Add(uiActor);
        var actor = ActorFactory.Create<TestActor>("MyActor");

        sendMock.SetupActionOnMessage(
            actor,
            uiActor,
            "_itemDataChanged",
            message =>
            {
                var valueChangedArgs =
                    message.DictPayload![nameof(ValueChangedArgs)] as ValueChangedArgs;
                var location = valueChangedArgs!.Location;
                var newValue = (int)valueChangedArgs.NewValue!;
                Assert.True(location.SequenceEqual(["Numbers", 0]));
                Assert.Equal(10, newValue);
                actor.Send(new TerminateMessage());
            }
        );
        actor.Send(
            new ActorItemMessage(
                uiActor,
                "/Product",
                "_itemDataWrite",
                new ItemDataWriteArgs(["Numbers", 0], 10)
            )
        );

        actor.Start();
        actor.Join();
    }

    public class TestActor : Actor
    {
        public Variable<Product> Product = new(VariableScope.Temporary);

        public TestActor(ActorConfig config)
            : base(config) { }

        protected override bool ProcessMessage(Message message)
        {
            switch (message.Name)
            {
                case "ChangeData":
                    Product.Value.Exists = true;
                    return true;
            }

            return base.ProcessMessage(message);
        }
    }

    public class Product : PropertyVariable, IDisposable
    {
        private bool _exists;
        private bool _good;

        public Product()
        {
            Numbers.ValueChanged += NumbersOnValueChanged;
        }

        public Array1D<int> Numbers { get; set; } = new(4);

        public bool Exists
        {
            get => _exists;
            set => ValueChangedUtils.SetField(ref _exists, value, OnValueChanged);
        }

        public bool Good
        {
            get => _good;
            set => ValueChangedUtils.SetField(ref _good, value, OnValueChanged);
        }

        public void Dispose()
        {
            Numbers.ValueChanged -= NumbersOnValueChanged;
        }

        private void NumbersOnValueChanged(object? sender, ValueChangedArgs e)
        {
            OnValueChanged(
                new ValueChangedArgs(
                    ((object[])[nameof(Numbers)]).Concat(e.Location).ToArray(),
                    e.OldValue,
                    e.NewValue
                )
            );
        }

        public override void OnDeserialized()
        {
            Numbers.ValueChanged += NumbersOnValueChanged;
            Numbers.OnDeserialized();
        }
    }
}
