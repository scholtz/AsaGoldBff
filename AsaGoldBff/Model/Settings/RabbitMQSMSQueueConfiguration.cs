using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AsaGoldBff.Model.Settings
{
    /// <summary>
    /// SMS Gateway through rabbit mq
    /// </summary>
    public class RabbitMQSMSQueueConfiguration
    {
        /// <summary>
        /// Hostname
        /// </summary>
        public string HostName { get; set; }
        /// <summary>
        /// User
        /// </summary>
        public string RabbitUserName { get; set; }
        /// <summary>
        /// Password
        /// </summary>
        public string RabbitPassword { get; set; }
        /// <summary>
        /// Virtual host
        /// </summary>
        public string VirtualHost { get; set; }
        /// <summary>
        /// Queue name
        /// </summary>
        public string QueueName { get; set; }
        /// <summary>
        /// Exchange
        /// </summary>
        public string Exchange { get; set; }
        /// <summary>
        /// User for gateway
        /// </summary>
        public string GatewayUser { get; set; }
        /// <summary>
        /// Cohash for loging purposes
        /// </summary>
        public string CoHash { get; set; }
    }
}
