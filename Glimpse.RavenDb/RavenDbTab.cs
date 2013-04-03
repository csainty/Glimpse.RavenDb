using System;
using System.Collections.Generic;
using System.Linq;
using Glimpse.Core.Extensibility;
using Glimpse.Core.Extensions;
using Glimpse.RavenDb.Message;
using Raven.Client.Document;
using Raven.Imports.Newtonsoft.Json.Linq;
using Raven.Json.Linq;

namespace Glimpse.RavenDb
{
    public class RavenDbTab : ITab, ITabSetup
    {
        public void Setup(ITabSetupContext context)
        {
            context.PersistMessages<RavenDbSessionMessage>();
        }

        public object GetData(ITabContext context)
        {
            var sessions = context.GetMessages<RavenDbSessionMessage>().Select(x => x.Id).ToArray();

            var data = new List<object[]>();
            data.Add(new object[] { "Key", "Value" });
            data.Add(new object[] { "Stores", GetStoreList() });
            data.Add(new object[] { "Sessions", GetSessionList(sessions) });
            data.Add(new object[] { "Requests", GetRequestList(sessions) });
            return data;
        }

        private List<object[]> GetStoreList()
        {
            List<object[]> data = new List<object[]>();
            data.Add(new object[] { "Url", "Database", "Conn. String Name", "Identity Separator", "Max Requests Per Session", "Embedded?" });
            data.AddRange(Profiler.Stores.Select(store => new object[] {
				store.Url,
				store.DefaultDatabase,
				store.ConnectionStringName,
				store.Conventions.IdentityPartsSeparator,
				store.Conventions.MaxNumberOfRequestsPerSession,
				IsEmbedded(store)
			}));
            return data;
        }

        private List<object[]> GetSessionList(IEnumerable<Guid> sessionIds)
        {
            List<object[]> data = new List<object[]>();
            if (!Profiler.Stores.Any())
            {
                data.Add(new object[] { "Message" });
                data.Add(new object[] { "Profiling has not been enabled for any RavenDb stores." });
            }
            else if (Profiler.Stores.All(d => IsEmbedded(d)))
            {
                // All the profiled stores are embedded and do not support profiling
                data.Add(new object[] { "Message" });
                data.Add(new object[] { "Profiling is currently not supported for EmbeddableDocumentStore." });
            }
            else
            {
                data.Add(new object[] { "Session Id", "Request Count", "At", "Duration" });

                var sessions = from id in sessionIds
                               from store in Profiler.Stores
                               let info = store.GetProfilingInformationFor(id)
                               where info != null
                               select info;
                data.AddRange(sessions.Select(session => new object[] {
					session.Id,
					session.Requests.Count,
					session.At,
					session.Requests.Sum(d => d.DurationMilliseconds)
				}));
            }
            return data;
        }

        private List<object[]> GetRequestList(IEnumerable<Guid> sessionIds)
        {
            List<object[]> data = new List<object[]>();
            data.Add(new object[] { "At", "Duration", "HttpMetod", "Url", "Data", "HttpResult", "Status", "Result" });
            var requests = from id in sessionIds
                           from store in Profiler.Stores
                           let info = store.GetProfilingInformationFor(id)
                           where info != null
                           from request in info.Requests
                           select request;
            data.AddRange(requests.Select(req => new object[] {
		        req.At,
				req.DurationMilliseconds,
		        req.Method,
		        req.Url,
		        ParseJsonResult(req.PostedData),
		        req.HttpResult,
		        req.Status.ToString(),
		        ParseJsonResult(req.Result)
		    }));
            return data;
        }

        public object ParseJsonResult(string json)
        {
            if (null == json) return null;
            if (json == "") return "";

            try
            {
                var token = RavenJToken.Parse(json);
                return Visit(token);
            }
            catch
            {
                return json;
            }
        }

        private object Visit(RavenJToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    List<object[]> data = new List<object[]>();
                    data.Add(new object[] { "Key", "Value" });
                    var obj = (RavenJObject)token;
                    foreach (var child in obj)
                    {
                        if (!Profiler.HiddenKeys.Contains(child.Key))
                        {
                            data.Add(new object[] { child.Key, Visit(child.Value) });
                        }
                    }
                    return data;
                case JTokenType.Array:
                    var arr = (RavenJArray)token;
                    if (arr.Length == 0)
                        return null;
                    List<object[]> arrayItems = new List<object[]>();
                    for (int i = 0; i < arr.Length; i++)
                    {
                        arrayItems.Add(new object[] { Visit(arr[i]) });
                    }

                    if (arr[0].Type == JTokenType.Object)
                    {
                        // Handle objects in a special way by pivoting them
                        List<object[]> pivotData = new List<object[]>();
                        var keys = arrayItems.SelectMany(d => (List<object[]>)d[0]).Cast<object[]>().Where(d => (string)d[0] != "Key" || (string)d[1] != "Value").Select(d => (string)d[0]).Distinct().ToArray();
                        pivotData.Add(keys);
                        foreach (var row in arrayItems.Select(d => d[0]).Cast<List<object[]>>())
                        {
                            object[] vals = new object[keys.Length];
                            for (int i = 0; i < keys.Length; i++)
                            {
                                var keyVal = row.FirstOrDefault(d => (string)d[0] == keys[i]);
                                if (keyVal != null)
                                {
                                    vals[i] = keyVal[1];
                                }
                            }
                            pivotData.Add(vals);
                        }

                        return pivotData;
                    }
                    else
                    {
                        arrayItems.Insert(0, new[] { "Values" });
                        return arrayItems;
                    }
                case JTokenType.String:
                case JTokenType.Boolean:
                case JTokenType.Float:
                case JTokenType.Integer:
                case JTokenType.Bytes:
                case JTokenType.Date:
                    return ((RavenJValue)token).Value;
                default:
                    return null;
            }
        }

        private bool IsEmbedded(DocumentStore store)
        {
            return store.GetType().Name == "EmbeddableDocumentStore";
        }

        public RuntimeEvent ExecuteOn
        {
            get { return RuntimeEvent.EndRequest; }
        }

        public string Name
        {
            get { return "RavenDb"; }
        }

        public Type RequestContextType
        {
            get { return null; }
        }
    }
}