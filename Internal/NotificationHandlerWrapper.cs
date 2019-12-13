namespace MediatR.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using UniRx.Async;

    internal abstract class NotificationHandlerWrapper
    {
        public abstract UniTask Handle(INotification notification, CancellationToken cancellationToken, ServiceFactory serviceFactory,
                                    Func<IEnumerable<Func<INotification, CancellationToken, UniTask>>, INotification, CancellationToken, UniTask> publish);
    }

    internal class NotificationHandlerWrapperImpl<TNotification> : NotificationHandlerWrapper
        where TNotification : INotification
    {
        public override UniTask Handle(INotification notification, CancellationToken cancellationToken, ServiceFactory serviceFactory,
                                    Func<IEnumerable<Func<INotification, CancellationToken, UniTask>>, INotification, CancellationToken, UniTask> publish)
        {
            var handlers = serviceFactory
                .GetInstances<INotificationHandler<TNotification>>()
                .Select(x => new Func<INotification, CancellationToken, UniTask>((theNotification, theToken) => x.Handle((TNotification)theNotification, theToken)));

            return publish(handlers, notification, cancellationToken);
        }
    }
}