namespace EventBus.Base.Subscription
{
    public class SubscriptionInfo
    {
        public SubscriptionInfo(Type handlerType)
        {
            HandlerType = handlerType ?? throw new ArgumentNullException(nameof(handlerType));
        }

        public Type HandlerType { get; set; }
        public static SubscriptionInfo Typed (Type handlerType) 
        {
            return new SubscriptionInfo (handlerType);
        }
    }
}
