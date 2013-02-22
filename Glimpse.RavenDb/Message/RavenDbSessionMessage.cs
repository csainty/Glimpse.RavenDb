using System;
using Glimpse.Core.Message;

namespace Glimpse.RavenDb.Message
{
    internal class RavenDbSessionMessage : IMessage
    {
        private readonly Guid sessionId;

        public Guid Id { get { return sessionId; } }

        public RavenDbSessionMessage(Guid sessionId)
        {
            this.sessionId = sessionId;
        }
    }
}