using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using MicroRabbit.Domain.Core.Bus;
using MediatR;
using RabbitMQ.Client;
using Newtonsoft.Json;
using MicroRabbit.Domain.Core.Events;
using RabbitMQ.Client.Events;

namespace MicroRabbit.Infra.Bus
{
    public sealed class RabbitMQBus : IEventBus
    {
        private readonly IMediator _mediator;
        private readonly Dictionary<string, List<Type>> _handler;
        private readonly List<Type> _eventTypes;

        public RabbitMQBus(IMediator mediator)
        {
            _mediator = mediator;
            _handler = new Dictionary<string, List<Type>>();
            _eventTypes = new List<Type>();
        }

        Task IEventBus.SendCommand<T>(T command)
        {
            return _mediator.Send(command);
        }

        void IEventBus.Publish<T>(T @event)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                var eventName = @event.GetType().Name;

                channel.QueueDeclare(eventName, false, false, false, null);
                var message = JsonConvert.SerializeObject(@event);
                var body = Encoding.UTF8.GetBytes(message);

                channel.BasicPublish("", eventName, null, body);
            }
        }

        void IEventBus.Subscribe<T, TH>()
        //where T : Event
        //where TH : IEventHandler<T>
        {
            var eventName = typeof(T).Name;
            var handlerType = typeof(TH);

            if (!_eventTypes.Contains(typeof(T)))
            {
                _eventTypes.Add(typeof(T));
            }
            if (!_handler.ContainsKey(eventName))
            {
                _handler.Add(eventName, new List<Type>());
            }
            if (_handler[eventName].Any(s => s.GetType() == handlerType))
            {
                // throw.Exception
            }

            _handler[eventName].Add(handlerType);

            startBasicConsume<T>();
        }

        private void startBasicConsume<T>() where T : Event
        {

            var factory = new ConnectionFactory()
            {
                HostName = "localhaost",
                DispatchConsumersAsync = true
            };

            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();

            var eventName = typeof(T).Name;
            channel.QueueDeclare(eventName, false, false, false, null);

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.Received += consumer_received;

            channel.BasicConsume(eventName, true, consumer);

        }

        private async Task consumer_received(object sender, BasicDeliverEventArgs e)
        {
            var eventName = e.RoutingKey;
            var message = Encoding.UTF8.GetString(e.Body.ToArray());

            try
            {
                await ProcessEvent(eventName, message).ConfigureAwait(false);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task ProcessEvent(string eventName, string message)
        {
            if (_handler.ContainsKey(eventName))
            {
                var subscriptions = _handler[eventName];

                foreach (var subscription in subscriptions)
                {
                    var handler = Activator.CreateInstance(subscription);
                    if (handler == null) continue;

                    var eventType = _eventTypes.SingleOrDefault(t => t.Name == eventName);
                    //var eventType = _eventTypes.Find(t => t.Name == eventName);
                    var @event = JsonConvert.DeserializeObject(message, eventType);

                    var concreteType = typeof(IEventHandler<>).MakeGenericType(eventType);

                    await (Task)concreteType.GetMethod("Handle").Invoke(handler, new object[] { @event });
                }

            }
        }
    }
}
