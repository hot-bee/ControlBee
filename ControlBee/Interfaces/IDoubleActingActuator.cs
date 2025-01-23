using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlBee.Interfaces
{
    public interface IDoubleActingActuator : IActorItem, IUsesPlaceholder
    {
        bool On { get; set; }
        bool Off { get; set; }
        bool IsOn { get; }
        bool IsOff { get; }
        public bool InputOnValue { get; }
        public bool InputOffValue { get; }
        void OnAndWait(int millisecondsTimeout);

        void OffAndWait(int millisecondsTimeout);
    }
}
