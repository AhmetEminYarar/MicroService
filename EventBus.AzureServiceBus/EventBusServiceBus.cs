using EventBus.Base.Events;
using EventBus.Base.SubscriptionManager;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;

namespace EventBus.AzureServiceBus
{
    public class EventBusServiceBus : BaseEventBus
    {
        private ITopicClient topicClient;
        private ManagementClient managementClient;
        private ILogger logger;

        public EventBusServiceBus(IServiceProvider serviceProvider, EventBusConfig config) : base(serviceProvider, config)
        {
            logger = serviceProvider.GetService(typeof(ILogger<EventBusServiceBus>)) as ILogger<EventBusServiceBus>;
            managementClient = new ManagementClient(config.EventBusConnectionString);
            topicClient = createTopicClient();
        }

        private ITopicClient createTopicClient()
        {
            if (topicClient == null || topicClient.IsClosedOrClosing)
            {
                topicClient = new TopicClient(EventBusConfig.EventBusConnectionString, EventBusConfig.DefaultTopicName, RetryPolicy.Default);
            }
            if (managementClient.TopicExistsAsync(EventBusConfig.DefaultTopicName).GetAwaiter().GetResult())
                managementClient.CreateTopicAsync(EventBusConfig.DefaultTopicName).GetAwaiter().GetResult();

            return topicClient;
        }

        public override void Publish(IntegrationEvent @event)
        {
            var eventName = @event.GetType().Name;
            eventName = ProcessEventName(eventName);
            var eventStr = JsonConvert.SerializeObject(@event);
            var bodyArr = Encoding.UTF8.GetBytes(eventStr);

            var message = new Message()
            {
                MessageId = Guid.NewGuid().ToString(),
                Body = bodyArr,
                Label = eventName

            };

            topicClient.SendAsync(message).GetAwaiter().GetResult();
        }

        public override void Subscribe<T, TH>()
        {
            var eventName = typeof(T).Name;
            eventName = ProcessEventName(eventName);

            if (!SubsManager.HasSubscripionForEvent(eventName))
            {
                var subsClient= CreateSubsClientIfNotExists(eventName);

            }
        }

        public override void UnSubscribe<T, TH>()
        {

        }

        private ISubscriptionClient CreateSubsClientIfNotExists(String eventName)
        {
            var subClient = CreateSubsClient(eventName);

            var exists = managementClient.SubscriptionExistsAsync(EventBusConfig.DefaultTopicName, GetSubName(eventName)).GetAwaiter().GetResult();,

            if (!exists)
            {
                managementClient.SubscriptionExistsAsync(EventBusConfig.DefaultTopicName, GetSubName(eventName)).GetAwaiter().GetResult();
                RemoveDefultRule(subClient);
            }

            CreateRuleIfNotExists(ProcessEventName(eventName), subClient); 

            return subClient;

        }

        private void CreateRuleIfNotExists(string eventName, ISubscriptionClient subscriptionClient)
        {
            bool ruleExists;

            try
            {
                var rule = managementClient.GetRuleAsync(EventBusConfig.DefaultTopicName, eventName, eventName).GetAwaiter().GetResult();
                ruleExists = rule != null;
            }
            catch (MessagingEntityNotFoundException)
            {
                ruleExists = false;
            }

            if (!ruleExists)
            {
                subscriptionClient.AddRuleAsync(new RuleDescription
                {
                    Filter = new CorrelationFilter { Label = eventName },
                    Name = eventName
                }).GetAwaiter().GetResult();
            }
        }

        private SubscriptionClient CreateSubsClient(string eventName)
        {
            return new SubscriptionClient(EventBusConfig.EventBusConnectionString, EventBusConfig.DefaultTopicName, GetSubName(eventName));
        }

        private void RemoveDefultRule(SubscriptionClient client)
        {
            try
            {
                client
                    .RemoveRuleAsync(RuleDescription.DefaultRuleName)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (MessageNotFoundException)
            {
                logger.LogWarning("the messing entity", RuleDescription.DefaultRuleName);
            }
        }
    }
}
