using System;
using System.Collections.Generic;
using System.Configuration;
using Glimpse.Core.Extensibility;
using Glimpse.Core.Message;
using Glimpse.RavenDb.Message;
using Raven.Abstractions.Connection;
using Raven.Client.Connection.Profiling;
using Raven.Client.Document;

namespace Glimpse.RavenDb
{
    public static class Profiler
    {
        private static List<string> jsonKeysToHide = new List<string>();
        private static List<DocumentStore> stores = new List<DocumentStore>();

        public static IEnumerable<DocumentStore> Stores { get { return stores; } }

        public static IEnumerable<string> HiddenKeys { get { return jsonKeysToHide; } }

        public static IMessageBroker MessageBroker { get; set; }

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
            Trace("Document store created");
            store.InitializeProfiling();
            store.SessionCreatedInternal += TrackSession;
            store.AfterDispose += StopTrackingStore;
            store.JsonRequestFactory.ConfigureRequest += BeginRequest;
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
            Trace("Stopped tracking store");
            stores.Remove(sender as DocumentStore);
        }

        private static void TrackSession(InMemoryDocumentSessionOperations session)
        {
            MessageBroker.Publish(new RavenDbSessionMessage(session.Id));
            Trace("Session created");
        }

        private static void BeginRequest(object sender, WebRequestEventArgs e)
        {
            //GlimpseTimer.Start("Query - " + e.Request.RequestUri.PathAndQuery, GlimpseTimerCategory);
        }

        private static void EndRequest(object sender, RequestResultArgs e)
        {
            //GlimpseTimer.Stop("Query - " + e.Url);
            Timeline("Query - " + e.Url, new TimerResult { Duration = TimeSpan.FromMilliseconds(e.DurationMilliseconds) });
        }

        private static void Trace(string message)
        {
            Publish(new TraceMessage { Category = "RavenDb", Message = message });
        }

        private static void Timeline(string message, TimerResult timer)
        {
            Publish(new RavenDbTimelineMessage()
                .AsTimelineMessage(message, RavenDbTimelineMessage.RavenDbTimelineCategory)
                .AsTimedMessage(timer));
        }

        private static void Publish<T>(T message)
        {
            var mb = MessageBroker;
            if (mb != null)
            {
                mb.Publish(message);
            }
        }
    }
}