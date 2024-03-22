using EventBus.Base.Events;
using EventBus.Base.SubscriptionManager;
using Newtonsoft.Json;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System.Net.Sockets;
using System.Text;

namespace EventBus.RabbitMQ
{
    public class EventBusRabbitMQ : BaseEventBus
    {
        RabbitMQPersistentConnection persistentConnection;
        private readonly IConnectionFactory connectionFactory;
        private readonly IModel ConsumerChannel;
        public EventBusRabbitMQ(IServiceProvider serviceProvider, EventBusConfig config) : base(serviceProvider, config)
        {
            if (config.Connection != null)
            {
                var Conn = JsonConvert.SerializeObject(EventBusConfig.Connection, new JsonSerializerSettings()
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });
                connectionFactory = JsonConvert.DeserializeObject<ConnectionFactory>(Conn);
            }
            else
                connectionFactory = new ConnectionFactory();
            persistentConnection = new RabbitMQPersistentConnection(connectionFactory, config.ConnectionRetryCount);
            ConsumerChannel = CreateConsumerChannel();
            SubsManager.OnEventRemoved += SubsManager_OnEventRemoved;
        }

        private void SubsManager_OnEventRemoved(object? sender, string e)
        {
            e = ProcessEventName(e);
            if (!persistentConnection.IsConnection)
                persistentConnection.TryConnect();

            ConsumerChannel.QueueUnbind(queue: e, exchange: EventBusConfig.DefaultTopicName, routingKey: e);

            if (SubsManager.IsEmpty)
                ConsumerChannel.Close();
        }

        public override void Publish(IntegrationEvent @event)
        {
            if (!persistentConnection.IsConnection)
                persistentConnection.TryConnect();


            var policy = Policy.Handle<BrokerUnreachableException>()
                .Or<SocketException>()
                .WaitAndRetry(EventBusConfig.ConnectionRetryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) => { });

            var eventName = @event.GetType().Name;
            eventName = ProcessEventName(eventName);

            ConsumerChannel.ExchangeDeclare(exchange: EventBusConfig.DefaultTopicName, type: "direct");

            var message = JsonConvert.SerializeObject(@event);
            var body = Encoding.UTF8.GetBytes(message);


            policy.Execute(() =>
            {
                var properties = ConsumerChannel.CreateBasicProperties();
                properties.DeliveryMode = 2;
                ConsumerChannel.QueueDeclare(queue: GetSubName(eventName), durable: true, exclusive: false, autoDelete: false, arguments: null);
                ConsumerChannel.BasicPublish(exchange: EventBusConfig.DefaultTopicName, routingKey: eventName, mandatory: true, basicProperties: properties, body: body);

            });
        }

        public override void Subscribe<T, TH>()
        {
            var eventName = typeof(T).Name;
            eventName = ProcessEventName(eventName);
            if (!SubsManager.HasSubscripionForEvent(eventName))
            {
                if (!persistentConnection.IsConnection)
                    persistentConnection.TryConnect();

                ConsumerChannel.QueueDeclare(queue: GetSubName(eventName), durable: true, exclusive: false, autoDelete: false, arguments: null);
                ConsumerChannel.QueueBind(queue: GetSubName(eventName), exchange: EventBusConfig.DefaultTopicName, routingKey: eventName);
            }
            SubsManager.AddSubscription<T, TH>();
            StartBasicConsume(eventName);
        }

        public override void UnSubscribe<T, TH>()
        {
            SubsManager.RemoveSubscripion<T, TH>();
        }

        private IModel CreateConsumerChannel()
        {
            if (!persistentConnection.IsConnection)
                persistentConnection.TryConnect();

            var channel = persistentConnection.CreateModel();
            channel.ExchangeDeclare(exchange: EventBusConfig.DefaultTopicName, type: "direct");
            return channel;

        }

        private void StartBasicConsume(string eventName)
        {
            if (ConsumerChannel != null)
            {
                var Consumer = new EventingBasicConsumer(ConsumerChannel);

                Consumer.Received += Consumer_Received;
                ConsumerChannel.BasicConsume(queue: GetSubName(eventName), autoAck: false, consumer: Consumer);
            }
        }

        private async void Consumer_Received(object? sender, BasicDeliverEventArgs e)
        {
            var eventName = e.RoutingKey;
            eventName = ProcessEventName(eventName);
            var message = Encoding.UTF8.GetString(e.Body.Span);
            try
            {
                await ProcessEvent(eventName, message);
            }
            catch (Exception ex)
            {

            }
            ConsumerChannel.BasicAck(e.DeliveryTag, multiple: false);
        }
    }
}
