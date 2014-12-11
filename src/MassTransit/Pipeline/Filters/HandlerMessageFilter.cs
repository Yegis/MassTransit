// Copyright 2007-2014 Chris Patterson, Dru Sellers, Travis Smith, et. al.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace MassTransit.Pipeline.Filters
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Util;

    /// <summary>
    /// Consumes a message via a message handler and reports the message as consumed or faulted
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    public class HandlerMessageFilter<TMessage> :
        IFilter<ConsumeContext<TMessage>>
        where TMessage : class
    {
        readonly MessageHandler<TMessage> _handler;

        // TODO this needs a pipe like instance and consumer, to handle things like retry, etc.
        public HandlerMessageFilter(MessageHandler<TMessage> handler)
        {
            if (handler == null)
                throw new ArgumentNullException("handler");

            _handler = handler;
        }

        async Task IFilter<ConsumeContext<TMessage>>.Send(ConsumeContext<TMessage> context, IPipe<ConsumeContext<TMessage>> next)
        {
            Stopwatch timer = Stopwatch.StartNew();
            try
            {
                await _handler(context);

                context.NotifyConsumed(timer.Elapsed, TypeMetadataCache<MessageHandler<TMessage>>.ShortName);

                await next.Send(context);
            }
            catch (Exception ex)
            {
                context.NotifyFaulted(TypeMetadataCache<MessageHandler<TMessage>>.ShortName, ex);
                throw;
            }
        }

        bool IFilter<ConsumeContext<TMessage>>.Inspect(IPipeInspector inspector)
        {
            return inspector.Inspect(this);
        }
    }
}