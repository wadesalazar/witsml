﻿//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2017.1
//
// Copyright 2017 Petrotechnical Data Systems
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Serialization;
using log4net;
using PDS.WITSMLstudio.Data.ChangeLogs;
using PDS.WITSMLstudio.Store.Properties;

namespace PDS.WITSMLstudio.Store.Providers.StoreNotification
{
    /// <summary>
    /// A basic implementation of the <see cref="IStoreNotificationProducer"/> interface.
    /// </summary>
    /// <seealso cref="PDS.WITSMLstudio.Store.Providers.StoreNotification.IStoreNotificationProducer" />
    [Export(typeof(IStoreNotificationProducer))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class StoreNotificationProducer : IStoreNotificationProducer
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(StoreNotificationProducer));
        private readonly IDictionary<string, object> _config;
        private readonly StringSerializer _serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="StoreNotificationProducer"/> class.
        /// </summary>
        public StoreNotificationProducer()
        {
            _config = new Dictionary<string, object> { { "bootstrap.servers", Settings.Default.KafkaBrokerList } };
            _serializer = new StringSerializer(Encoding.UTF8);
        }

        /// <summary>
        /// Sends the notification messages for the speficed entity.
        /// </summary>
        /// <typeparam name="T">The data object type.</typeparam>
        /// <param name="entity">The changed entity.</param>
        /// <param name="auditHistory">The audit history.</param>
        public void SendNotifications<T>(T entity, DbAuditHistory auditHistory)
        {
            var uri = auditHistory.Uri.ToLowerInvariant();
            var xml = WitsmlParser.ToXml(entity);
            var topic = "test";

            Task.Run(async() =>
            {
                using (var producer = new Producer<string, string>(_config, _serializer, _serializer))
                {
                    _log.Debug($"{producer.Name} producing on {topic}.");

                    var result = await producer.ProduceAsync(topic, uri, xml);

                    _log.Debug($"Partition: {result.Partition}, Offset: {result.Offset}");

                    producer.Flush();
                }
            });
        }
    }
}
