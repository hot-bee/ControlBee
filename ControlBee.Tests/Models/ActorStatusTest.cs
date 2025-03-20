using ControlBee.Interfaces;
using ControlBee.Models;
using ControlBee.Utils;
using ControlBeeTest.Utils;
using JetBrains.Annotations;
using Moq;
using Xunit;

namespace ControlBee.Tests.Models;

[TestSubject(typeof(Actor))]
public class ActorStatusTest : ActorFactoryBase
{
    [Fact]
    public void PublishOnlyOnceTest()
    {
        var sendMock = new SendMock();
        var client = MockActorFactory.Create("Client");
        var actor = ActorFactory.Create<TestActor>("MyActor");
        actor.SetPeer(client);
        sendMock.SetupActionOnMessage(
            actor,
            client,
            "_status",
            message =>
            {
                Assert.Equal("Leo", DictPath.Start(message.DictPayload)["Name"].Value);
                Assert.Equal("Male", DictPath.Start(message.DictPayload)["Gender"].Value);
                Assert.Equal(
                    true,
                    DictPath.Start(message.DictPayload)["Client"]["ReadyToHome"].Value
                );
                Assert.Equal(
                    "WhiteHouse",
                    DictPath.Start(message.DictPayload)["Client"]["HomeAddress"].Value
                );
            }
        );

        actor.Start();
        actor.Send(new Message(EmptyActor.Instance, "ChangeStatus"));
        actor.Send(new TerminateMessage());
        actor.Join();

        ActorUtils.VerifyGetMessage(actor, client, "_status", Times.Once);
    }

    public class TestActor : Actor
    {
        public IActor Client;

        public TestActor(ActorConfig config)
            : base(config) { }

        public void SetPeer(IActor client)
        {
            Client = client;
            InitPeers([client]);
        }

        protected override bool ProcessMessage(Message message)
        {
            switch (message.Name)
            {
                case "ChangeStatus":
                {
                    using var statusGroup = new StatusGroup(this);
                    SetStatus("Name", "Leo");
                    SetStatus("Gender", "Male");
                    SetStatusByActor(Client, "ReadyToHome", true);
                    SetStatusByActor(Client, "HomeAddress", "WhiteHouse");
                    return true;
                }
            }

            return base.ProcessMessage(message);
        }
    }
}
