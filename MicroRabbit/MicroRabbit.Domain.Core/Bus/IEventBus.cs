using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MicroRabbit.Domain.Core.Commands;
using MicroRabbit.Domain.Core.Events;

namespace MicroRabbit.Domain.Core.Bus
{
    public interface IEventBus
    {
        public Task SendCommand<T>(T command) where T : Command;

        public void Publish<T>(T @event) where T : Event;

        public void Subscribe<T, TH>()
            where T : Event
            where TH : IEventHandler<T>;
    }
}
