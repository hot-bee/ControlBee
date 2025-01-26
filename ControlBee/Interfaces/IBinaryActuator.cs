using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlBee.Interfaces
{
    public interface IBinaryActuator : IActorItem, IUsesPlaceholder
    {
        bool GetCommandOn();
        bool GetCommandOff();
        void On();
        void Off();
        bool? IsOn { get; }
        bool? IsOff { get; }
        public bool OnDetect { get; }
        public bool OffDetect { get; }
        void OnAndWait();

        void OffAndWait();
        void Wait();
    }
}
