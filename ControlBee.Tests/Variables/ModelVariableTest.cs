using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Tests.TestUtils;
using ControlBee.Utils;
using ControlBee.Variables;
using JetBrains.Annotations;
using Moq;
using Xunit;
using Dict = System.Collections.Generic.Dictionary<string, object?>;

namespace ControlBee.Tests.Variables;

[TestSubject(typeof(Variable<>))]
public class ModelVariableTest : ActorFactoryBase
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
                var newValue = (Product)DictPath.Start(message.Payload)["NewValue"].Value!;
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
                var newValue = (Product)DictPath.Start(message.Payload)["NewValue"].Value!;
                Assert.True(newValue.Exists);
                actor.Send(new TerminateMessage());
            }
        );
        actor.Send(
            new ActorItemMessage(
                uiActor,
                "/Product",
                "_itemDataWrite",
                new Product { Exists = true }
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
                var location = (string)DictPath.Start(message.Payload)["Location"].Value!;
                var newValue = (bool)DictPath.Start(message.Payload)["NewValue"].Value!;
                Assert.Equal("Exists", location);
                Assert.True(newValue);
                actor.Send(new TerminateMessage());
            }
        );
        actor.Send(new Message(uiActor, "ChangeData"));

        actor.Start();
        actor.Join();
    }

    [Fact]
    public void DataModifyTest()
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
                var location = (string)DictPath.Start(message.Payload)["Location"].Value!;
                var newValue = (bool)DictPath.Start(message.Payload)["NewValue"].Value!;
                Assert.Equal("Exists", location);
                Assert.True(newValue);
                actor.Send(new TerminateMessage());
            }
        );
        actor.Send(
            new ActorItemMessage(
                uiActor,
                "/Product",
                "_itemDataModify",
                new Dict { ["Exists"] = true }
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

    public class Product : INotifyPropertyChanged
    {
        private bool _exists;
        private bool _good;

        public bool Exists
        {
            get => _exists;
            set => SetField(ref _exists, value);
        }

        public bool Good
        {
            get => _good;
            set => SetField(ref _good, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(
            ref T field,
            T value,
            [CallerMemberName] string? propertyName = null
        )
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
