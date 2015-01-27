using System;
using System.Collections.Generic;
using System.Configuration;
using Glimpse.Core.Extensibility;
using Glimpse.Core.Message;
using Glimpse.RavenDb.Message;
using Raven.Client;
using Raven.Client.Connection.Profiling;
using Raven.Client.Document;
using Raven.Client.Listeners;

namespace Glimpse.RavenDb
{
    public class RavenDbInspector : IInspector
    {
        public void Setup(IInspectorContext context)
        {
            Profiler.MessageBroker = context.MessageBroker;
            Profiler.ExecutionTimerFactory =  () =>
            {
                try
                {
                    return context.TimerStrategy();
                }
                catch
                {
                    // Avoid exception being thrown from threads without access to request store
                    return null;
                }
            };
        }
    }

    public static class Profiler
    {
        private static List<string> jsonKeysToHide = new List<string>();
        private static List<DocumentStore> stores = new List<DocumentStore>();

        public static IEnumerable<DocumentStore> Stores { get { return stores; } }

        private static bool Enabled { get { return MessageBroker != null && ExecutionTimerFactory != null; } }

        public static IEnumerable<string> HiddenKeys { get { return jsonKeysToHide; } }

        internal static IMessageBroker MessageBroker { get; set; }

        internal static Func<IExecutionTimer> ExecutionTimerFactory { get; set; }

        static Profiler()
        {
            var fields = ConfigurationManager.AppSettings["Glimpse.RavenDb.HiddenFields"];
            if (!String.IsNullOrEmpty(fields))
            {
                HideFields(fields.Split(','));
            }
        }

        /// <summary>
        /// Attach a DocumentStore instance to the profiler
        /// </summary>
        /// <param name="store">The instance to profile</param>
        public static void AttachTo(DocumentStore store)
        {
            Trace("Document store attached");
            store.InitializeProfiling();
            store.SessionCreatedInternal += TrackSession;
            store.AfterDispose += StopTrackingStore;
            store.JsonRequestFactory.LogRequest += EndRequest;
            stores.Add(store);
        }

        /// <summary>
        /// Set a list of fields to be hidden in your json result
        /// </summary>
        /// <param name="keys">The field names that will be hidden</param>
        public static void HideFields(params string[] keys)
        {
            jsonKeysToHide.AddRange(keys);
        }

        private static void StopTrackingStore(object sender, EventArgs e)
        {
            var store = sender as DocumentStore;
            if (store == null) return;

            stores.Remove(sender as DocumentStore);
            store.SessionCreatedInternal -= TrackSession;
            store.AfterDispose -= StopTrackingStore;
            if (store.HasJsonRequestFactory)
            {
                store.JsonRequestFactory.LogRequest -= EndRequest;
            }
        }

        private static void TrackSession(InMemoryDocumentSessionOperations session)
        {
            PointOnTimeline("RavenDb session created");
            Publish(new RavenDbSessionMessage(session.Id));
        }

        private static void EndRequest(object sender, RequestResultArgs e)
        {
            DurationOnTimeline("Query - " + e.Url, e.At.Subtract(TimeSpan.FromMilliseconds(e.DurationMilliseconds)), TimeSpan.FromMilliseconds(e.DurationMilliseconds));
        }

        private static void Trace(string message)
        {
            Publish(new TraceMessage { Category = "RavenDb", Message = message });
        }

        private static void PointOnTimeline(string message)
        {
            if (!Enabled) return;
            var timer = ExecutionTimerFactory();
            if (timer == null) return;

            Timeline(message, timer.Point());
        }

        private static void DurationOnTimeline(string message, DateTime startTime, TimeSpan duration)
        {
            if (!Enabled) return;
            var timer = ExecutionTimerFactory();
            if (timer == null) return;

            Timeline(message, new TimerResult
            {
                StartTime = startTime,
                Offset = startTime.Subtract(timer.RequestStart.ToUniversalTime()),
                Duration = duration
            });
        }

        private static void Timeline(string message, TimerResult timerResult)
        {
            Publish(new RavenDbTimelineMessage()
                .AsTimelineMessage(message, RavenDbTimelineMessage.RavenDbTimelineCategory)
                .AsTimedMessage(timerResult));
        }

        private static void Publish<T>(T message)
        {
            if (MessageBroker == null) return;
            MessageBroker.Publish(message);
        }
    }
}