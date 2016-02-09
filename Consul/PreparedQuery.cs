﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Consul
{
    /// <summary>
    /// QueryDatacenterOptions sets options about how we fail over if there are no healthy nodes in the local datacenter.
    /// </summary>
    public class QueryDatacenterOptions
    {
        /// <summary>
        /// NearestN is set to the number of remote datacenters to try, based on network coordinates.
        /// </summary>
        public int NearestN { get; set; }

        /// <summary>
        /// Datacenters is a fixed list of datacenters to try after NearestN. We
        /// never try a datacenter multiple times, so those are subtracted from
        /// this list before proceeding.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Datacenters { get; set; }

        public QueryDatacenterOptions()
        {
            Datacenters = new List<string>();
        }
    }

    /// <summary>
    /// QueryDNSOptions controls settings when query results are served over DNS.
    /// </summary>
    public class QueryDNSOptions
    {
        /// <summary>
        /// TTL is the time to live for the served DNS results.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public TimeSpan? TTL { get; set; }
    }

    /// <summary>
    /// ServiceQuery is used to query for a set of healthy nodes offering a specific service.
    /// </summary>
    public class ServiceQuery
    {
        /// <summary>
        /// Service is the service to query.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Service { get; set; }

        /// <summary>
        /// Failover controls what we do if there are no healthy nodes in the local datacenter.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public QueryDatacenterOptions Failover { get; set; }

        /// <summary>
        /// If OnlyPassing is true then we will only include nodes with passing
        /// health checks (critical AND warning checks will cause a node to be
        /// discarded)
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool OnlyPassing { get; set; }

        /// <summary>
        /// Tags are a set of required and/or disallowed tags. If a tag is in
        /// this list it must be present. If the tag is preceded with "!" then
        /// it is disallowed.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Tags { get; set; }
    }

    /// <summary>
    /// PrepatedQueryDefinition defines a complete prepared query.
    /// </summary>
    public class PreparedQueryDefinition
    {
        /// <summary>
        /// ID is this UUID-based ID for the query, always generated by Consul.
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// Name is an optional friendly name for the query supplied by the
        /// user. NOTE - if this feature is used then it will reduce the security
        /// of any read ACL associated with this query/service since this name
        /// can be used to locate nodes with supplying any ACL.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Session is an optional session to tie this query's lifetime to. If
        /// this is omitted then the query will not expire.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Session { get; set; }

        /// <summary>
        /// Token is the ACL token used when the query was created, and it is
        /// used when a query is subsequently executed. This token, or a token
        /// with management privileges, must be used to change the query later.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Token { get; set; }

        /// <summary>
        /// Service defines a service query (leaving things open for other types
        /// later).
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ServiceQuery Service { get; set; }

        /// <summary>
        /// DNS has options that control how the results of this query are
        /// served over DNS.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public QueryDNSOptions DNS { get; set; }
    }

    /// <summary>
    /// PreparedQueryExecuteResponse has the results of executing a query.
    /// </summary>
    public class PreparedQueryExecuteResponse
    {
        /// <summary>
        /// Service is the service that was queried.
        /// </summary>
        public string Service { get; set; }

        /// <summary>
        /// Nodes has the nodes that were output by the query.
        /// </summary>
        public ServiceEntry[] Nodes { get; set; }

        /// <summary>
        /// DNS has the options for serving these results over DNS.
        /// </summary>
        public QueryDNSOptions DNS { get; set; }

        /// <summary>
        /// Datacenter is the datacenter that these results came from.
        /// </summary>
        public string Datacenter { get; set; }

        /// <summary>
        /// Failovers is a count of how many times we had to query a remote
        /// datacenter.
        /// </summary>
        public int Failovers { get; set; }
    }

    public class PreparedQuery : IPreparedQueryEndpoint
    {
        private class PreparedQueryCreationResult
        {
            [JsonProperty]
            internal string ID { get; set; }
        }
        private readonly ConsulClient _client;

        internal PreparedQuery(ConsulClient c)
        {
            _client = c;
        }

        public Task<WriteResult<string>> Create(PreparedQueryDefinition query)
        {
            return Create(query, WriteOptions.Default);
        }

        public async Task<WriteResult<string>> Create(PreparedQueryDefinition query, WriteOptions q)
        {
            var res = await _client.Post<PreparedQueryDefinition, PreparedQueryCreationResult>("/v1/query", query, q).Execute().ConfigureAwait(false);
            return new WriteResult<string>()
            {
                RequestTime = res.RequestTime,
                Response = res.Response.ID
            };
        }

        public Task<WriteResult> Delete(string queryID)
        {
            return Delete(queryID, WriteOptions.Default);
        }

        public async Task<WriteResult> Delete(string queryID, WriteOptions q)
        {
            var res = await _client.Delete<string>(string.Format("/v1/query/{0}", queryID), q).Execute();
            return new WriteResult
            {
                RequestTime = res.RequestTime
            };
        }

        public Task<QueryResult<PreparedQueryExecuteResponse>> Execute(string queryIDOrName)
        {
            return Execute(queryIDOrName, QueryOptions.Default);
        }

        public Task<QueryResult<PreparedQueryExecuteResponse>> Execute(string queryIDOrName, QueryOptions q)
        {
            return _client.Get<PreparedQueryExecuteResponse>(string.Format("/v1/query/{0}/execute", queryIDOrName), q).Execute();
        }

        public Task<QueryResult<PreparedQueryDefinition[]>> Get(string queryID)
        {
            return Get(queryID, QueryOptions.Default);
        }

        public Task<QueryResult<PreparedQueryDefinition[]>> Get(string queryID, QueryOptions q)
        {
            return _client.Get<PreparedQueryDefinition[]>(string.Format("/v1/query/{0}", queryID), q).Execute();
        }

        public Task<QueryResult<PreparedQueryDefinition[]>> List()
        {
            return List(QueryOptions.Default);
        }

        public Task<QueryResult<PreparedQueryDefinition[]>> List(QueryOptions q)
        {
            return _client.Get<PreparedQueryDefinition[]>("/v1/query", q).Execute();
        }

        public Task<WriteResult> Update(PreparedQueryDefinition query)
        {
            return Update(query, WriteOptions.Default);
        }

        public Task<WriteResult> Update(PreparedQueryDefinition query, WriteOptions q)
        {
            return _client.Put(string.Format("/v1/query/{0}", query.ID), query, q).Execute();
        }
    }

    public partial class ConsulClient : IConsulClient
    {
        private PreparedQuery _preparedquery;

        /// <summary>
        /// Catalog returns a handle to the catalog endpoints
        /// </summary>
        public IPreparedQueryEndpoint PreparedQuery
        {
            get
            {
                if (_preparedquery == null)
                {
                    lock (_lock)
                    {
                        if (_preparedquery == null)
                        {
                            _preparedquery = new PreparedQuery(this);
                        }
                    }
                }
                return _preparedquery;
            }
        }
    }
}
