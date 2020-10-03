using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MicroRabbit.Domain.Core.Events;

namespace MicroRabbit.Domain.Core.Bus
{
   public interface IEventHandler<in TEvent> : IEventHandler
        where TEvent : Event
        //where THEvent : IEventHandler<T>
    {
        Task Handle(TEvent @event);
    }
    
    public interface IEventHandler
    {

    }
}
