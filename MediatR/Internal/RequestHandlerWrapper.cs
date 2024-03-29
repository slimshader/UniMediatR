namespace MediatR.Internal
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using UniRx.Async;

    internal abstract class RequestHandlerBase
    {
        public abstract UniTask<object> Handle(object request, CancellationToken cancellationToken,
            ServiceFactory serviceFactory);

        protected static THandler GetHandler<THandler>(ServiceFactory factory)
        {
            THandler handler;

            try
            {
                handler = factory.GetInstance<THandler>();
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Error constructing handler for request of type {typeof(THandler)}. Register your handlers with the container. See the samples in GitHub for examples.", e);
            }

            if (handler == null)
            {
                throw new InvalidOperationException($"Handler was not found for request of type {typeof(THandler)}. Register your handlers with the container. See the samples in GitHub for examples.");
            }

            return handler;
        }
    }

    internal abstract class RequestHandlerWrapper<TResponse> : RequestHandlerBase
    {
        public abstract UniTask<TResponse> Handle(IRequest<TResponse> request, CancellationToken cancellationToken,
            ServiceFactory serviceFactory);
    }

    internal class RequestHandlerWrapperImpl<TRequest, TResponse> : RequestHandlerWrapper<TResponse>
        where TRequest : IRequest<TResponse>
    {
        public override UniTask<object> Handle(object request, CancellationToken cancellationToken,
            ServiceFactory serviceFactory)
        {
            return Handle((IRequest<TResponse>)request, cancellationToken, serviceFactory)
                .ContinueWith(t => (object) t);
        }

        public override UniTask<TResponse> Handle(IRequest<TResponse> request, CancellationToken cancellationToken,
            ServiceFactory serviceFactory)
        {
            UniTask<TResponse> Handler() => GetHandler<IRequestHandler<TRequest, TResponse>>(serviceFactory).Handle((TRequest) request, cancellationToken);

            return serviceFactory
                .GetInstances<IPipelineBehavior<TRequest, TResponse>>()
                .Reverse()
                .Aggregate((RequestHandlerDelegate<TResponse>) Handler, (next, pipeline) => () => pipeline.Handle((TRequest)request, cancellationToken, next))();
        }
    }
}
