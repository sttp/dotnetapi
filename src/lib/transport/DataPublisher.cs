//******************************************************************************************************
//  DataPublisher.cs - Gbtc
//
//  Copyright © 2019, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the MIT License (MIT), the "License"; you may not use this
//  file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://opensource.org/licenses/MIT
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  04/12/2019 - J. Ritchie Carroll
//       Imported source code from Grid Solutions Framework.
//
//******************************************************************************************************

using sttp.communication;
using sttp.security;
using sttp.units;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Timers;
using System.Xml;
using Timer = System.Timers.Timer;

namespace sttp.transport
{
    /// <summary>
    /// Represents a data publishing server that allows multiple connections for data subscriptions.
    /// </summary>
    public class DataPublisher
    {
        #region [ Members ]

        // Events

        /// <summary>
        /// Indicates that a new client has connected to the publisher.
        /// </summary>
        /// <remarks>
        /// <see cref="EventArgs{T1, T2, T3}.Argument1"/> is the <see cref="Guid"/> based subscriber ID.<br/>
        /// <see cref="EventArgs{T1, T2, T3}.Argument2"/> is the connection identification (e.g., IP and DNS name, if available).<br/>
        /// <see cref="EventArgs{T1, T2, T3}.Argument3"/> is the subscriber information as reported by the client.
        /// </remarks>
        public event EventHandler<EventArgs<Guid, string, string>> ClientConnected;

        /// <summary>
        /// Indicates to the host that processing for an input adapter (via temporal session) has completed.
        /// </summary>
        /// <remarks>
        /// This event is expected to only be raised when an input adapter has been designed to process
        /// a finite amount of data, e.g., reading a historical range of data during temporal processing.
        /// </remarks>
        public event EventHandler ProcessingComplete;

        // Constants

        /// <summary>
        /// Default value for <see cref="SecurityMode"/>.
        /// </summary>
        public const SecurityMode DefaultSecurityMode = SecurityMode.None;

        /// <summary>
        /// Default value for <see cref="EncryptPayload"/>.
        /// </summary>
        public const bool DefaultEncryptPayload = false;

        /// <summary>
        /// Default value for <see cref="SharedDatabase"/>.
        /// </summary>
        public const bool DefaultSharedDatabase = false;

        /// <summary>
        /// Default value for <see cref="AllowPayloadCompression"/>.
        /// </summary>
        public const bool DefaultAllowPayloadCompression = true;

        /// <summary>
        /// Default value for <see cref="AllowMetadataRefresh"/>.
        /// </summary>
        public const bool DefaultAllowMetadataRefresh = true;

        /// <summary>
        /// Default value for <see cref="AllowNaNValueFilter"/>.
        /// </summary>
        public const bool DefaultAllowNaNValueFilter = true;

        /// <summary>
        /// Default value for <see cref="ForceNaNValueFilter"/>.
        /// </summary>
        public const bool DefaultForceNaNValueFilter = false;

        /// <summary>
        /// Default value for <see cref="UseBaseTimeOffsets"/>.
        /// </summary>
        public const bool DefaultUseBaseTimeOffsets = false;

        /// <summary>
        /// Default value for <see cref="CipherKeyRotationPeriod"/>.
        /// </summary>
        public const double DefaultCipherKeyRotationPeriod = 60000.0D;

        /// <summary>
        /// Default value for <see cref="MetadataTables"/>.
        /// </summary>
        public const string DefaultMetadataTables =
            "SELECT NodeID, UniqueID, OriginalSource, IsConcentrator, Acronym, Name, AccessID, ParentAcronym, ProtocolName, FramesPerSecond, CompanyAcronym, VendorAcronym, VendorDeviceName, Longitude, Latitude, InterconnectionName, ContactList, Enabled, UpdatedOn FROM DeviceDetail WHERE IsConcentrator = 0;" +
            "SELECT DeviceAcronym, ID, SignalID, PointTag, SignalReference, SignalAcronym, PhasorSourceIndex, Description, Internal, Enabled, UpdatedOn FROM MeasurementDetail;" +
            "SELECT ID, DeviceAcronym, Label, Type, Phase, DestinationPhasorID, SourceIndex, UpdatedOn FROM PhasorDetail;" +
            "SELECT VersionNumber FROM SchemaVersion";

        /// <summary>
        /// Maximum packet size before software fragmentation of payload.
        /// </summary>
        public const int MaxPacketSize = ushort.MaxValue / 2;

        /// <summary>
        /// Size of client response header in bytes.
        /// </summary>
        /// <remarks>
        /// Header consists of response byte, in-response-to server command byte, 4-byte int representing payload length.
        /// </remarks>
        public const int ClientResponseHeaderSize = 6;

        // Length of random salt prefix
        internal const int CipherSaltLength = 8;

        // Fields
        private IServer m_commandChannel;
        private CertificatePolicyChecker m_certificateChecker;
        private Dictionary<X509Certificate, DataRow> m_subscriberIdentities;
        private ConcurrentDictionary<Guid, SubscriberConnection> m_clientConnections;
        private DataSet m_dataSource;
        private readonly ConcurrentDictionary<Guid, IServer> m_clientPublicationChannels;
        private readonly Dictionary<Guid, Dictionary<int, string>> m_clientNotifications;
        private readonly object m_clientNotificationsLock;
        private Timer m_commandChannelRestartTimer;
        private Timer m_cipherKeyRotationTimer;
        private RoutingTables m_routingTables;
        private string m_metadataTables;
        private string m_cacheMeasurementKeys;
        private SecurityMode m_securityMode;
        private bool m_encryptPayload;
        private bool m_sharedDatabase;
        private bool m_allowPayloadCompression;
        private bool m_allowMetadataRefresh;
        private bool m_allowNaNValueFilter;
        private bool m_forceNaNValueFilter;
        private bool m_useBaseTimeOffsets;
        private int m_measurementReportingInterval;

        private long m_totalBytesSent;
        private long m_lifetimeMeasurements;
        private long m_minimumMeasurementsPerSecond;
        private long m_maximumMeasurementsPerSecond;
        private long m_totalMeasurementsPerSecond;
        private long m_measurementsPerSecondCount;
        private long m_measurementsInSecond;
        private long m_lastSecondsSinceEpoch;
        private long m_lifetimeTotalLatency;
        private long m_lifetimeMinimumLatency;
        private long m_lifetimeMaximumLatency;
        private long m_lifetimeLatencyMeasurements;
        private long m_bufferBlockRetransmissions;

        //// For backwards compatibility.
        //// If a client requests metadata, but fails to define the type of
        //// metadata they would like to receive, we assume the client is
        //// using an old version of the protocol. These flags will define
        //// what type of metadata this type of client should actually receive.
        //private OperationalModes m_forceReceiveMetadataFlags;

        private bool m_disposed;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Creates a new <see cref="DataPublisher"/>.
        /// </summary>
        public DataPublisher()
        {
            m_clientConnections = new ConcurrentDictionary<Guid, SubscriberConnection>();
            m_clientPublicationChannels = new ConcurrentDictionary<Guid, IServer>();
            m_clientNotifications = new Dictionary<Guid, Dictionary<int, string>>();
            m_clientNotificationsLock = new object();
            m_securityMode = DefaultSecurityMode;
            m_encryptPayload = DefaultEncryptPayload;
            m_sharedDatabase = DefaultSharedDatabase;
            m_allowPayloadCompression = DefaultAllowPayloadCompression;
            m_allowMetadataRefresh = DefaultAllowMetadataRefresh;
            m_allowNaNValueFilter = DefaultAllowNaNValueFilter;
            m_forceNaNValueFilter = DefaultForceNaNValueFilter;
            m_useBaseTimeOffsets = DefaultUseBaseTimeOffsets;
            m_metadataTables = DefaultMetadataTables;

            m_routingTables = new RoutingTables();
            m_routingTables.StatusMessage += m_routingTables_StatusMessage;
            m_routingTables.ProcessException += m_routingTables_ProcessException;

            // Setup a timer for restarting the command channel if it fails
            m_commandChannelRestartTimer = new Timer(2000);
            m_commandChannelRestartTimer.AutoReset = false;
            m_commandChannelRestartTimer.Enabled = false;
            m_commandChannelRestartTimer.Elapsed += m_commandChannelRestartTimer_Elapsed;

            // Setup a timer for rotating cipher keys
            m_cipherKeyRotationTimer = new Timer((int)DefaultCipherKeyRotationPeriod);
            m_cipherKeyRotationTimer.AutoReset = true;
            m_cipherKeyRotationTimer.Enabled = false;
            m_cipherKeyRotationTimer.Elapsed += m_cipherKeyRotationTimer_Elapsed;
        }

        /// <summary>
        /// Releases the unmanaged resources before the <see cref="DataPublisher"/> object is reclaimed by <see cref="GC"/>.
        /// </summary>
        ~DataPublisher()
        {
            Dispose(false);
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets or sets the security mode of the <see cref="DataPublisher"/>'s command channel.
        /// </summary>
        public SecurityMode SecurityMode
        {
            get => m_securityMode;
            set => m_securityMode = value;
        }

        /// <summary>
        /// Gets or sets flag that determines whether data sent over the data channel should be encrypted.
        /// </summary>
        public bool EncryptPayload
        {
            get => m_encryptPayload;
            set
            {
                m_encryptPayload = value;

                // Start cipher key rotation timer when encrypting payload
                if ((object)m_cipherKeyRotationTimer != null)
                    m_cipherKeyRotationTimer.Enabled = value;
            }
        }

        /// <summary>
        /// Gets or sets flag that indicates whether this publisher is publishing
        /// data that this node subscribed to from another node in a shared database.
        /// </summary>
        public bool SharedDatabase
        {
            get => m_sharedDatabase;
            set => m_sharedDatabase = value;
        }

        /// <summary>
        /// Gets or sets flag that indicates if this publisher will allow payload compression when requested by subscribers.
        /// </summary>
        public bool AllowPayloadCompression
        {
            get => m_allowPayloadCompression;
            set => m_allowPayloadCompression = value;
        }

        /// <summary>
        /// Gets or sets flag that indicates if this publisher will allow synchronized subscriptions when requested by subscribers.
        /// </summary>
        public bool AllowMetadataRefresh
        {
            get => m_allowMetadataRefresh;
            set => m_allowMetadataRefresh = value;
        }

        /// <summary>
        /// Gets or sets flag that indicates if this publisher will allow filtering of data which is not a number.
        /// </summary>
        public bool AllowNaNValueFilter
        {
            get => m_allowNaNValueFilter;
            set => m_allowNaNValueFilter = value;
        }

        /// <summary>
        /// Gets or sets flag that indicates if this publisher will force filtering of data which is not a number.
        /// </summary>
        public bool ForceNaNValueFilter
        {
            get => m_forceNaNValueFilter;
            set => m_forceNaNValueFilter = value;
        }

        /// <summary>
        /// Gets or sets flag that determines whether to use base time offsets to decrease the size of compact measurements.
        /// </summary>
        public bool UseBaseTimeOffsets
        {
            get => m_useBaseTimeOffsets;
            set => m_useBaseTimeOffsets = value;
        }

        /// <summary>
        /// Gets or sets the cipher key rotation period.
        /// </summary>
        public double CipherKeyRotationPeriod
        {
            get
            {
                if ((object)m_cipherKeyRotationTimer != null)
                    return m_cipherKeyRotationTimer.Interval;

                return double.NaN;
            }
            set
            {
                if (value < 1000.0D)
                    throw new ArgumentOutOfRangeException(nameof(value), "Cipher key rotation period should not be set to less than 1000 milliseconds.");

                if ((object)m_cipherKeyRotationTimer != null)
                    m_cipherKeyRotationTimer.Interval = (int)value;

                throw new ArgumentException("Cannot assign new cipher rotation period, timer is not defined.");
            }
        }

        /// <summary>
        /// Gets or sets the measurement reporting interval.
        /// </summary>
        /// <remarks>
        /// This is used to determined how many measurements should be processed before reporting status.
        /// </remarks>
        public int MeasurementReportingInterval
        {
            get => m_measurementReportingInterval;
            set => m_measurementReportingInterval = value;
        }

        /// <summary>
        /// Gets or sets <see cref="DataSet"/> based data source.
        /// </summary>
        public virtual DataSet DataSource
        {
            get => m_dataSource;
            set
            {
                if (DataSourceChanged(value))
                {
                    m_dataSource = value;

                    UpdateRights();
                    UpdateCertificateChecker();
                    UpdateClientNotifications();
                    NotifyClientsOfConfigurationChange();
                }
            }
        }

        /// <summary>
        /// Gets the status of this <see cref="DataPublisher"/>.
        /// </summary>
        /// <remarks>
        /// Derived classes should provide current status information about the adapter for display purposes.
        /// </remarks>
        public virtual string Status
        {
            get
            {
                StringBuilder status = new StringBuilder();

                if ((object)m_commandChannel != null)
                    status.Append(m_commandChannel.Status);

                status.AppendFormat("        Reporting interval: {0:N0} per subscriber", MeasurementReportingInterval);
                status.AppendLine();
                status.AppendFormat("  Buffer block retransmits: {0:N0}", m_bufferBlockRetransmissions);
                status.AppendLine();

                return status.ToString();
            }
        }

        /// <summary>
        /// Gets or sets the name of this <see cref="DataPublisher"/>.
        /// </summary>
        /// <remarks>
        /// The assigned name is used as the settings category when persisting the TCP server settings.
        /// </remarks>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets semi-colon separated list of SQL select statements used to create data for meta-data exchange.
        /// </summary>
        public string MetadataTables
        {
            get => m_metadataTables;
            set => m_metadataTables = value;
        }

        /// <summary>
        /// Gets dictionary of connected clients.
        /// </summary>
        protected internal ConcurrentDictionary<Guid, SubscriberConnection> ClientConnections => m_clientConnections;

        /// <summary>
        /// Gets or sets reference to <see cref="TcpServer"/> command channel, attaching and/or detaching to events as needed.
        /// </summary>
        protected IServer CommandChannel
        {
            get => m_commandChannel;
            set
            {
                if ((object)m_commandChannel != null)
                {
                    // Detach from events on existing command channel reference
                    m_commandChannel.ClientConnected -= m_commandChannel_ClientConnected;
                    m_commandChannel.ClientDisconnected -= m_commandChannel_ClientDisconnected;
                    m_commandChannel.ClientConnectingException -= m_commandChannel_ClientConnectingException;
                    m_commandChannel.ReceiveClientDataComplete -= m_commandChannel_ReceiveClientDataComplete;
                    m_commandChannel.ReceiveClientDataException -= m_commandChannel_ReceiveClientDataException;
                    m_commandChannel.SendClientDataException -= m_commandChannel_SendClientDataException;
                    m_commandChannel.ServerStarted -= m_commandChannel_ServerStarted;
                    m_commandChannel.ServerStopped -= m_commandChannel_ServerStopped;

                    if (m_commandChannel != value)
                        m_commandChannel.Dispose();
                }

                // Assign new command channel reference
                m_commandChannel = value;

                if ((object)m_commandChannel != null)
                {
                    // Attach to desired events on new command channel reference
                    m_commandChannel.ClientConnected += m_commandChannel_ClientConnected;
                    m_commandChannel.ClientDisconnected += m_commandChannel_ClientDisconnected;
                    m_commandChannel.ClientConnectingException += m_commandChannel_ClientConnectingException;
                    m_commandChannel.ReceiveClientDataComplete += m_commandChannel_ReceiveClientDataComplete;
                    m_commandChannel.ReceiveClientDataException += m_commandChannel_ReceiveClientDataException;
                    m_commandChannel.SendClientDataException += m_commandChannel_SendClientDataException;
                    m_commandChannel.ServerStarted += m_commandChannel_ServerStarted;
                    m_commandChannel.ServerStopped += m_commandChannel_ServerStopped;
                }
            }
        }

        /// <summary>
        /// Gets flag indicating if publisher is connected and listening.
        /// </summary>
        public bool IsConnected => m_commandChannel.Enabled;

        /// <summary>
        /// Gets the total number of buffer block retransmissions on all subscriptions over the lifetime of the publisher.
        /// </summary>
        public long BufferBlockRetransmissions => m_bufferBlockRetransmissions;

        /// <summary>
        /// Gets the total number of bytes sent to clients of this data publisher.
        /// </summary>
        public long TotalBytesSent => m_totalBytesSent;

        /// <summary>
        /// Gets the total number of measurements processed through this data publisher over the lifetime of the publisher.
        /// </summary>
        public long LifetimeMeasurements => m_lifetimeMeasurements;

        /// <summary>
        /// Gets the minimum value of the measurements per second calculation.
        /// </summary>
        public long MinimumMeasurementsPerSecond => m_minimumMeasurementsPerSecond;

        /// <summary>
        /// Gets the maximum value of the measurements per second calculation.
        /// </summary>
        public long MaximumMeasurementsPerSecond => m_maximumMeasurementsPerSecond;

        /// <summary>
        /// Gets the average value of the measurements per second calculation.
        /// </summary>
        public long AverageMeasurementsPerSecond
        {
            get
            {
                if (m_measurementsPerSecondCount == 0L)
                    return 0L;

                return m_totalMeasurementsPerSecond / m_measurementsPerSecondCount;
            }
        }

        /// <summary>
        /// Gets the minimum latency calculated over the full lifetime of the publisher.
        /// </summary>
        public int LifetimeMinimumLatency => (int)Ticks.ToMilliseconds(m_lifetimeMinimumLatency);

        /// <summary>
        /// Gets the maximum latency calculated over the full lifetime of the publisher.
        /// </summary>
        public int LifetimeMaximumLatency => (int)Ticks.ToMilliseconds(m_lifetimeMaximumLatency);

        /// <summary>
        /// Gets the average latency calculated over the full lifetime of the publisher.
        /// </summary>
        public int LifetimeAverageLatency
        {
            get
            {
                if (m_lifetimeLatencyMeasurements == 0)
                    return -1;

                return (int)Ticks.ToMilliseconds(m_lifetimeTotalLatency / m_lifetimeLatencyMeasurements);
            }
        }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="DataPublisher"/> object and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!m_disposed)
            {
                try
                {
                    if (disposing)
                    {
                        CommandChannel = null;

                        if ((object)m_clientConnections != null)
                            m_clientConnections.Values.AsParallel().ForAll(cc => cc.Dispose());

                        m_clientConnections = null;

                        if ((object)m_routingTables != null)
                        {
                            m_routingTables.StatusMessage -= m_routingTables_StatusMessage;
                            m_routingTables.ProcessException -= m_routingTables_ProcessException;
                            m_routingTables.Dispose();
                        }
                        m_routingTables = null;

                        // Dispose command channel restart timer
                        if ((object)m_commandChannelRestartTimer != null)
                        {
                            m_commandChannelRestartTimer.Elapsed -= m_commandChannelRestartTimer_Elapsed;
                            m_commandChannelRestartTimer.Dispose();
                        }
                        m_commandChannelRestartTimer = null;

                        // Dispose the cipher key rotation timer
                        if ((object)m_cipherKeyRotationTimer != null)
                        {
                            m_cipherKeyRotationTimer.Elapsed -= m_cipherKeyRotationTimer_Elapsed;
                            m_cipherKeyRotationTimer.Dispose();
                        }
                        m_cipherKeyRotationTimer = null;
                    }
                }
                finally
                {
                    m_disposed = true;          // Prevent duplicate dispose.
                }
            }
        }

        /// <summary>
        /// Initializes <see cref="DataPublisher"/>.
        /// </summary>
        public virtual void Initialize()
        {
            Dictionary<string, string> settings = Settings;

            // Check flag that will determine if subscriber payloads should be encrypted by default
            if (settings.TryGetValue("encryptPayload", out string setting))
                m_encryptPayload = setting.ParseBoolean();

            // Check flag that indicates whether publisher is publishing data
            // that its node subscribed to from another node in a shared database
            if (settings.TryGetValue("sharedDatabase", out setting))
                m_sharedDatabase = setting.ParseBoolean();

            // Extract custom metadata table expressions if provided
            if (settings.TryGetValue("metadataTables", out setting) && !string.IsNullOrWhiteSpace(setting))
                m_metadataTables = setting;

            // Check flag to see if payload compression is allowed
            if (settings.TryGetValue("allowPayloadCompression", out setting))
                m_allowPayloadCompression = setting.ParseBoolean();

            // Check flag to see if metadata refresh commands are allowed
            if (settings.TryGetValue("allowMetadataRefresh", out setting))
                m_allowMetadataRefresh = setting.ParseBoolean();

            // Check flag to see if NaN value filtering is allowed
            if (settings.TryGetValue("allowNaNValueFilter", out setting))
                m_allowNaNValueFilter = setting.ParseBoolean();

            // Check flag to see if NaN value filtering is forced
            if (settings.TryGetValue("forceNaNValueFilter", out setting))
                m_forceNaNValueFilter = setting.ParseBoolean();

            if (settings.TryGetValue("useBaseTimeOffsets", out setting))
                m_useBaseTimeOffsets = setting.ParseBoolean();

            // Get user specified period for cipher key rotation
            if (settings.TryGetValue("cipherKeyRotationPeriod", out setting) && double.TryParse(setting, out double period))
                CipherKeyRotationPeriod = period;

            // Get security mode used for the command channel
            if (settings.TryGetValue("securityMode", out setting))
                m_securityMode = (SecurityMode)Enum.Parse(typeof(SecurityMode), setting);

            if (m_securityMode == SecurityMode.TLS)
            {
                // Create a new TLS server
                TlsServer commandChannel = new TlsServer();

                // Create certificate checker
                m_certificateChecker = new CertificatePolicyChecker();
                m_subscriberIdentities = new Dictionary<X509Certificate, DataRow>();
                UpdateCertificateChecker();

                // Initialize default settings
                commandChannel.SettingsCategory = Name.Replace("!", "").ToLower();
                commandChannel.ConfigurationString = "port=6165";
                commandChannel.PayloadAware = false;
                commandChannel.RequireClientCertificate = true;
                commandChannel.CertificateChecker = m_certificateChecker;
                commandChannel.PersistSettings = true;
                commandChannel.NoDelay = true;

                // Assign command channel client reference and attach to needed events
                CommandChannel = commandChannel;
            }
            else
            {
                // Create a new TCP server
                TcpServer commandChannel = new TcpServer();

                // Initialize default settings
                commandChannel.SettingsCategory = Name.Replace("!", "").ToLower();
                commandChannel.ConfigurationString = "port=6165";
                commandChannel.PayloadAware = false;
                commandChannel.PersistSettings = true;
                commandChannel.NoDelay = true;

                // Assign command channel client reference and attach to needed events
                CommandChannel = commandChannel;
            }

            // Initialize TCP server - this will load persisted settings
            m_commandChannel.Initialize();

            // Allow user to override persisted settings by specifying a command channel setting
            if (settings.TryGetValue("commandChannel", out setting) && !string.IsNullOrWhiteSpace(setting))
                m_commandChannel.ConfigurationString = setting;

            // Start cipher key rotation timer when encrypting payload
            if (m_encryptPayload && (object)m_cipherKeyRotationTimer != null)
                m_cipherKeyRotationTimer.Start();
        }

        /// <summary>
        /// Queues a collection of measurements for processing to each subscriber connected to this <see cref="DataPublisher"/>.
        /// </summary>
        /// <param name="measurements">Measurements to queue for processing.</param>
        public virtual void QueueMeasurementsForProcessing(IEnumerable<IMeasurement> measurements)
        {
            int measurementCount;

            IList<IMeasurement> measurementList = measurements as IList<IMeasurement> ?? measurements.ToList();
            m_routingTables.InjectMeasurements(this, new EventArgs<ICollection<IMeasurement>>(measurementList));

            measurementCount = measurementList.Count;
            m_lifetimeMeasurements += measurementCount;
            UpdateMeasurementsPerSecond(measurementCount);
        }

        /// <summary>
        /// Establish <see cref="DataPublisher"/> and start listening for client connections.
        /// </summary>
        public virtual void Start()
        {
            if (!Enabled)
            {
                if ((object)m_commandChannel != null)
                    m_commandChannel.Start();
            }
        }

        /// <summary>
        /// Terminate <see cref="DataPublisher"/> and stop listening for client connections.
        /// </summary>
        public virtual void Stop()
        {
            if ((object)m_commandChannel != null)
                m_commandChannel.Stop();
        }

        /// <summary>
        /// Gets a short one-line status of this <see cref="DataPublisher"/>.
        /// </summary>
        /// <param name="maxLength">Maximum number of available characters for display.</param>
        /// <returns>A short one-line summary of the current status of the <see cref="DataPublisher"/>.</returns>
        public string GetShortStatus(int maxLength)
        {
            if ((object)m_commandChannel != null)
                return $"Publishing data to {m_commandChannel.ClientIDs.Length} clients.".CenterText(maxLength);

            return "Currently not connected".CenterText(maxLength);
        }

        public string EnumerateClients(bool filterToTemporalSessions)
        {
            StringBuilder clientEnumeration = new StringBuilder();
            Guid[] clientIDs = (Guid[])m_commandChannel.ClientIDs.Clone();
            bool hasActiveTemporalSession;

            if (filterToTemporalSessions)
                clientEnumeration.AppendFormat("\r\nIndices for connected clients with active temporal sessions:\r\n\r\n");
            else
                clientEnumeration.AppendFormat("\r\nIndices for {0} connected clients:\r\n\r\n", clientIDs.Length);

            for (int i = 0; i < clientIDs.Length; i++)
            {
                if (m_clientConnections.TryGetValue(clientIDs[i], out SubscriberConnection connection) && (object)connection != null && (object)connection.Subscription != null)
                {
                    hasActiveTemporalSession = connection.Subscription.TemporalConstraintIsDefined();

                    if (!filterToTemporalSessions || hasActiveTemporalSession)
                    {
                        clientEnumeration.Append(
                            $"  {i.ToString().PadLeft(3)} - {connection.ConnectionID}\r\n" +
                            $"          {connection.SubscriberInfo}\r\n" + GetOperationalModes(connection) + "\r\n\r\n");
                           // + $"          Active Temporal Session = {(hasActiveTemporalSession ? "Yes" : "No")}\r\n\r\n");
                    }
                }
            }

            // Return enumeration
            return clientEnumeration.ToString();
        }
        private string GetOperationalModes(SubscriberConnection connection)
        {
            StringBuilder description = new StringBuilder();
            OperationalModes operationalModes = connection.OperationalModes;
            CompressionModes compressionModes = (CompressionModes)(operationalModes & OperationalModes.CompressionModeMask);
            bool tsscEnabled = (compressionModes & CompressionModes.TSSC) > 0;
            bool gzipEnabled = (compressionModes & CompressionModes.GZip) > 0;

            if ((operationalModes & OperationalModes.CompressPayloadData) > 0 && tsscEnabled)
            {
                description.Append($"          CompressPayloadData[TSSC]\r\n");
            }
            else
            {
                if (connection.Subscription.UseCompactMeasurementFormat)
                    description.Append("          CompactPayloadData[");
                else
                    description.Append("          FullSizePayloadData[");

                description.Append($"{connection.Subscription.TimestampSize}-byte Timestamps]\r\n");
            }

            if ((operationalModes & OperationalModes.CompressSignalIndexCache) > 0 && gzipEnabled)
                description.Append("          CompressSignalIndexCache\r\n");

            if ((operationalModes & OperationalModes.CompressMetadata) > 0 && gzipEnabled)
                description.Append("          CompressMetadata\r\n");

            if ((operationalModes & OperationalModes.ReceiveExternalMetadata) > 0)
                description.Append("          ReceiveExternalMetadata\r\n");

            if ((operationalModes & OperationalModes.ReceiveInternalMetadata) > 0)
                description.Append("          ReceiveInternalMetadata\r\n");

            return description.ToString();
        }

        /// <summary>
        /// Rotates cipher keys for specified client connection.
        /// </summary>
        /// <param name="clientIndex">Enumerated index for client connection.</param>
        public virtual void RotateCipherKeys(int clientIndex)
        {
            Guid clientID = Guid.Empty;
            bool success = true;

            try
            {
                clientID = m_commandChannel.ClientIDs[clientIndex];
            }
            catch
            {
                success = false;
                OnStatusMessage(MessageLevel.Error, $"Failed to find connected client with enumerated index {clientIndex}");
            }

            if (success)
            {
                if (m_clientConnections.TryGetValue(clientID, out SubscriberConnection connection))
                    connection.RotateCipherKeys();
                else
                    OnStatusMessage(MessageLevel.Error, $"Failed to find connected client {clientID}");
            }
        }

        /// <summary>
        /// Gets subscriber information for specified client connection.
        /// </summary>
        /// <param name="clientIndex">Enumerated index for client connection.</param>
        public virtual string GetSubscriberInfo(int clientIndex)
        {
            Guid clientID = Guid.Empty;
            bool success = true;

            try
            {
                clientID = m_commandChannel.ClientIDs[clientIndex];
            }
            catch
            {
                success = false;
                OnStatusMessage(MessageLevel.Error, $"Failed to find connected client with enumerated index {clientIndex}");
            }

            if (success)
            {
                if (m_clientConnections.TryGetValue(clientID, out SubscriberConnection connection))
                    return connection.SubscriberInfo;

                OnStatusMessage(MessageLevel.Error, $"Failed to find connected client {clientID}");
            }

            return "";
        }

        /// <summary>
        /// Gets the local certificate currently in use by the data publisher.
        /// </summary>
        /// <returns>The local certificate file read directly from the certificate file as an array of bytes.</returns>
        public virtual byte[] GetLocalCertificate()
        {
            TlsServer commandChannel;

            commandChannel = m_commandChannel as TlsServer;

            if ((object)commandChannel == null)
                throw new InvalidOperationException("Certificates can only be imported in TLS security mode.");

            return File.ReadAllBytes(FilePath.GetAbsolutePath(commandChannel.CertificateFile));
        }

        /// <summary>
        /// Imports a certificate to the trusted certificates path.
        /// </summary>
        /// <param name="fileName">The file name to give to the certificate when imported.</param>
        /// <param name="certificateData">The data to be written to the certificate file.</param>
        /// <returns>The local path on the server where the file was written.</returns>
        public virtual string ImportCertificate(string fileName, byte[] certificateData)
        {
            TlsServer commandChannel;
            string trustedCertificatesPath;
            string filePath;

            commandChannel = m_commandChannel as TlsServer;

            if ((object)commandChannel == null)
                throw new InvalidOperationException("Certificates can only be imported in TLS security mode.");

            trustedCertificatesPath = FilePath.GetAbsolutePath(commandChannel.TrustedCertificatesPath);
            filePath = Path.Combine(trustedCertificatesPath, fileName);
            filePath = FilePath.GetUniqueFilePathWithBinarySearch(filePath);

            if (!Directory.Exists(trustedCertificatesPath))
                Directory.CreateDirectory(trustedCertificatesPath);

            File.WriteAllBytes(filePath, certificateData);

            return filePath;
        }

        /// <summary>
        /// Gets subscriber status for specified subscriber ID.
        /// </summary>
        /// <param name="subscriberID">Guid based subscriber ID for client connection.</param>
        public virtual Tuple<Guid, bool, string> GetSubscriberStatus(Guid subscriberID)
        {
            return new Tuple<Guid, bool, string>(subscriberID, GetConnectionProperty(subscriberID, cc => cc.IsConnected), GetConnectionProperty(subscriberID, cc => cc.SubscriberInfo));
        }

        /// <summary>
        /// Resets the counters for the lifetime statistics without interrupting the adapter's operations.
        /// </summary>
        public virtual void ResetLifetimeCounters()
        {
            m_lifetimeMeasurements = 0L;
            m_totalBytesSent = 0L;
            m_lifetimeTotalLatency = 0L;
            m_lifetimeMinimumLatency = 0L;
            m_lifetimeMaximumLatency = 0L;
            m_lifetimeLatencyMeasurements = 0L;
        }

        /// <summary>
        /// Sends a notification to all subscribers.
        /// </summary>
        /// <param name="message">The message to be sent.</param>
        public virtual void SendNotification(string message)
        {
            string notification = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] {message}";

            lock (m_clientNotificationsLock)
            {
                foreach (Dictionary<int, string> notifications in m_clientNotifications.Values)
                    notifications.Add(notification.GetHashCode(), notification);

                SerializeClientNotifications();
                SendAllNotifications();
            }

            OnStatusMessage(MessageLevel.Info, "Sent notification: {0}", message);
        }

        /// <summary>
        /// Updates signal index cache based on input measurement keys.
        /// </summary>
        /// <param name="clientID">Client ID of connection over which to update signal index cache.</param>
        /// <param name="signalIndexCache">New signal index cache.</param>
        /// <param name="inputMeasurementKeys">Subscribed measurement keys.</param>
        public void UpdateSignalIndexCache(Guid clientID, SignalIndexCache signalIndexCache, MeasurementKey[] inputMeasurementKeys)
        {
            ConcurrentDictionary<int, MeasurementKey> reference = new ConcurrentDictionary<int, MeasurementKey>();
            List<Guid> unauthorizedKeys = new List<Guid>();
            int index = 0;
            Guid signalID;

            byte[] serializedSignalIndexCache;

            Func<Guid, bool> hasRightsFunc = id => true;

            if ((object)inputMeasurementKeys != null)
            {
                //hasRightsFunc = RequireAuthentication
                //    ? new SubscriberRightsLookup(DataSource, signalIndexCache.SubscriberID).HasRightsFunc
                //    : id => true;

                // We will now go through the client's requested keys and see which ones are authorized for subscription,
                // this information will be available through the returned signal index cache which will also define
                // a runtime index optimization for the allowed measurements.
                foreach (MeasurementKey key in inputMeasurementKeys)
                {
                    signalID = key.SignalID;

                    // Validate that subscriber has rights to this signal
                    if (signalID != Guid.Empty && hasRightsFunc(signalID))
                        reference.TryAdd(index++, key);
                    else
                        unauthorizedKeys.Add(key.SignalID);
                }
            }

            signalIndexCache.Reference = reference;
            signalIndexCache.UnauthorizedSignalIDs = unauthorizedKeys.ToArray();
            serializedSignalIndexCache = SerializeSignalIndexCache(clientID, signalIndexCache);

            // Send client updated signal index cache
            if (m_clientConnections.TryGetValue(clientID, out SubscriberConnection connection) && connection.IsSubscribed)
                SendClientResponse(clientID, ServerResponse.UpdateSignalIndexCache, ServerCommand.Subscribe, serializedSignalIndexCache);
        }

        /// <summary>
        /// Updates each subscription's inputs based on
        /// possible updates to that subscriber's rights.
        /// </summary>
        protected void UpdateRights()
        {
            foreach (SubscriberConnection connection in m_clientConnections.Values)
                UpdateRights(connection);

            m_routingTables.CalculateRoutingTables(null);
        }

        private void UpdateClientNotifications()
        {
            try
            {
                lock (m_clientNotificationsLock)
                {
                    m_clientNotifications.Clear();

                    if (DataSource.Tables.Contains("Subscribers"))
                    {
                        foreach (DataRow row in DataSource.Tables["Subscribers"].Rows)
                        {
                            if (Guid.TryParse(row["ID"].ToNonNullString(), out Guid subscriberID))
                                m_clientNotifications.Add(subscriberID, new Dictionary<int, string>());
                        }
                    }

                    DeserializeClientNotifications();
                }
            }
            catch (Exception ex)
            {
                OnProcessException(MessageLevel.Info, new InvalidOperationException($"Failed to initialize client notification dictionary: {ex.Message}", ex));
            }
        }

        private void NotifyClientsOfConfigurationChange()
        {
            // Make sure publisher allows meta-data refresh, no need to notify clients of configuration change if they can't receive updates
            if (m_allowMetadataRefresh)
            {
                // This can be a lazy notification so we queue up work and return quickly
                ThreadPool.QueueUserWorkItem(state =>
                {
                    // Make a copy of client connection enumeration with ToArray() in case connections are added or dropped during notification
                    foreach (SubscriberConnection connection in m_clientConnections.Values)
                        SendClientResponse(connection.ClientID, ServerResponse.ConfigurationChanged, ServerCommand.Subscribe);
                });
            }
        }

        private void SerializeClientNotifications()
        {
            string notificationsFileName = FilePath.GetAbsolutePath($"{Name}Notifications.txt");

            // Delete existing file for re-serialization
            if (File.Exists(notificationsFileName))
                File.Delete(notificationsFileName);

            using (FileStream fileStream = File.OpenWrite(notificationsFileName))
            using (TextWriter writer = new StreamWriter(fileStream))
            {
                foreach (KeyValuePair<Guid, Dictionary<int, string>> pair in m_clientNotifications)
                {
                    foreach (string notification in pair.Value.Values)
                        writer.WriteLine("{0},{1}", pair.Key, notification);
                }
            }
        }

        private void DeserializeClientNotifications()
        {
            string notificationsFileName = FilePath.GetAbsolutePath($"{Name}Notifications.txt");
            string notification;

            if (File.Exists(notificationsFileName))
            {
                using (FileStream fileStream = File.OpenRead(notificationsFileName))
                using (TextReader reader = new StreamReader(fileStream))
                {
                    string line = reader.ReadLine();

                    while ((object)line != null)
                    {
                        int separatorIndex = line.IndexOf(',');

                        if (Guid.TryParse(line.Substring(0, separatorIndex), out Guid subscriberID))
                        {
                            if (m_clientNotifications.TryGetValue(subscriberID, out Dictionary<int, string> notifications))
                            {
                                notification = line.Substring(separatorIndex + 1);
                                notifications.Add(notification.GetHashCode(), notification);
                            }
                        }

                        line = reader.ReadLine();
                    }
                }
            }
        }

        private void SendAllNotifications()
        {
            foreach (SubscriberConnection connection in m_clientConnections.Values)
                SendNotifications(connection);
        }

        private void SendNotifications(SubscriberConnection connection)
        {
            byte[] hash;
            byte[] message;

            using (MemoryStream buffer = new MemoryStream())
            {
                if (m_clientNotifications.TryGetValue(connection.SubscriberID, out Dictionary<int, string> notifications))
                {
                    foreach (KeyValuePair<int, string> pair in notifications)
                    {
                        hash = BigEndian.GetBytes(pair.Key);
                        message = connection.Encoding.GetBytes(pair.Value);

                        buffer.Write(hash, 0, hash.Length);
                        buffer.Write(message, 0, message.Length);
                        SendClientResponse(connection.ClientID, ServerResponse.Notify, ServerCommand.Subscribe, buffer.ToArray());
                        buffer.Position = 0;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the text encoding associated with a particular client.
        /// </summary>
        /// <param name="clientID">ID of client.</param>
        /// <returns>Text encoding associated with a particular client.</returns>
        protected internal Encoding GetClientEncoding(Guid clientID)
        {
            if (m_clientConnections.TryGetValue(clientID, out SubscriberConnection connection))
            {
                Encoding clientEncoding = connection.Encoding;

                if ((object)clientEncoding != null)
                    return clientEncoding;
            }

            // Default to Unicode
            return Encoding.Unicode;
        }

        /// <summary>
        /// Sends the start time of the first measurement in a connection transmission.
        /// </summary>
        /// <param name="clientID">ID of client to send response.</param>
        /// <param name="startTime">Start time, in <see cref="Ticks"/>, of first measurement transmitted.</param>
        protected internal virtual bool SendDataStartTime(Guid clientID, Ticks startTime)
        {
            bool result = SendClientResponse(clientID, ServerResponse.DataStartTime, ServerCommand.Subscribe, BigEndian.GetBytes((long)startTime));

            if (m_clientConnections.TryGetValue(clientID, out SubscriberConnection connection))
                OnStatusMessage(MessageLevel.Info, $"Start time sent to {connection.ConnectionID}.");

            return result;
        }

        // Handle input processing complete notifications

        /// <summary>
        /// Sends response back to specified client.
        /// </summary>
        /// <param name="clientID">ID of client to send response.</param>
        /// <param name="response">Server response.</param>
        /// <param name="command">In response to command.</param>
        /// <returns><c>true</c> if send was successful; otherwise <c>false</c>.</returns>
        protected internal virtual bool SendClientResponse(Guid clientID, ServerResponse response, ServerCommand command)
        {
            return SendClientResponse(clientID, response, command, (byte[])null);
        }

        /// <summary>
        /// Sends response back to specified client with a message.
        /// </summary>
        /// <param name="clientID">ID of client to send response.</param>
        /// <param name="response">Server response.</param>
        /// <param name="command">In response to command.</param>
        /// <param name="status">Status message to return.</param>
        /// <returns><c>true</c> if send was successful; otherwise <c>false</c>.</returns>
        protected internal virtual bool SendClientResponse(Guid clientID, ServerResponse response, ServerCommand command, string status)
        {
            if ((object)status != null)
                return SendClientResponse(clientID, response, command, GetClientEncoding(clientID).GetBytes(status));

            return SendClientResponse(clientID, response, command);
        }

        /// <summary>
        /// Sends response back to specified client with a formatted message.
        /// </summary>
        /// <param name="clientID">ID of client to send response.</param>
        /// <param name="response">Server response.</param>
        /// <param name="command">In response to command.</param>
        /// <param name="formattedStatus">Formatted status message to return.</param>
        /// <param name="args">Arguments for <paramref name="formattedStatus"/>.</param>
        /// <returns><c>true</c> if send was successful; otherwise <c>false</c>.</returns>
        protected internal virtual bool SendClientResponse(Guid clientID, ServerResponse response, ServerCommand command, string formattedStatus, params object[] args)
        {
            if (!string.IsNullOrWhiteSpace(formattedStatus))
                return SendClientResponse(clientID, response, command, GetClientEncoding(clientID).GetBytes(string.Format(formattedStatus, args)));

            return SendClientResponse(clientID, response, command);
        }

        /// <summary>
        /// Sends response back to specified client with attached data.
        /// </summary>
        /// <param name="clientID">ID of client to send response.</param>
        /// <param name="response">Server response.</param>
        /// <param name="command">In response to command.</param>
        /// <param name="data">Data to return to client; null if none.</param>
        /// <returns><c>true</c> if send was successful; otherwise <c>false</c>.</returns>
        protected internal virtual bool SendClientResponse(Guid clientID, ServerResponse response, ServerCommand command, byte[] data)
        {
            return SendClientResponse(clientID, (byte)response, (byte)command, data);
        }

        /// <summary>
        /// Updates latency statistics based on the collection of latencies passed into the method.
        /// </summary>
        /// <param name="latencies">The latencies of the measurements sent by the publisher.</param>
        protected internal virtual void UpdateLatencyStatistics(IEnumerable<long> latencies)
        {
            foreach (long latency in latencies)
            {
                // Throw out latencies that exceed one hour as invalid
                if (Math.Abs(latency) > Time.SecondsPerHour * Ticks.PerSecond)
                    continue;

                if (m_lifetimeMinimumLatency > latency || m_lifetimeMinimumLatency == 0)
                    m_lifetimeMinimumLatency = latency;

                if (m_lifetimeMaximumLatency < latency || m_lifetimeMaximumLatency == 0)
                    m_lifetimeMaximumLatency = latency;

                m_lifetimeTotalLatency += latency;
                m_lifetimeLatencyMeasurements++;
            }
        }

        // Attempts to get the subscriber for the given client based on that client's X.509 certificate.
        private void TryFindClientDetails(SubscriberConnection connection)
        {
            TlsServer commandChannel = m_commandChannel as TlsServer;
            X509Certificate remoteCertificate;
            X509Certificate trustedCertificate;

            // If connection is not TLS, there is no X.509 certificate
            if ((object)commandChannel == null)
                return;

            // If connection is not found, cannot get X.509 certificate
            if (!commandChannel.TryGetClient(connection.ClientID, out TransportProvider<TlsServer.TlsSocket> client))
                return;

            // Get remote certificate and corresponding trusted certificate
            remoteCertificate = client.Provider.SslStream.RemoteCertificate;
            trustedCertificate = m_certificateChecker.GetTrustedCertificate(remoteCertificate);

            if ((object)trustedCertificate != null)
            {
                if (m_subscriberIdentities.TryGetValue(trustedCertificate, out DataRow subscriber))
                {
                    // Load client details from subscriber identity
                    connection.SubscriberID = Guid.Parse(subscriber["ID"].ToNonNullString(Guid.Empty.ToString()).Trim());
                    connection.SubscriberAcronym = subscriber["Acronym"].ToNonNullString().Trim();
                    connection.SubscriberName = subscriber["Name"].ToNonNullString().Trim();
                    connection.ValidIPAddresses = ParseAddressList(subscriber["ValidIPAddresses"].ToNonNullString());
                }
            }
        }

        // Parses a list of IP addresses.
        private List<IPAddress> ParseAddressList(string addressList)
        {
            string[] splitList = addressList.Split(';', ',');
            List<IPAddress> ipAddressList = new List<IPAddress>();
            string dualStackAddress;

            foreach (string address in splitList)
            {
                // Attempt to parse the IP address
                if (!IPAddress.TryParse(address.Trim(), out IPAddress ipAddress))
                    continue;

                // Add the parsed address to the list
                ipAddressList.Add(ipAddress);

                // IPv4 addresses may connect as an IPv6 dual-stack equivalent,
                // so attempt to add that equivalent address to the list as well
                if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
                {
                    dualStackAddress = $"::ffff:{address.Trim()}";

                    if (IPAddress.TryParse(dualStackAddress, out ipAddress))
                        ipAddressList.Add(ipAddress);
                }
            }

            return ipAddressList;
        }

        // Update certificate validation routine.
        private void UpdateCertificateChecker()
        {
            try
            {
                CertificatePolicy policy;

                string remoteCertificateFile;
                X509Certificate certificate;

                if ((object)m_certificateChecker == null || (object)m_subscriberIdentities == null || m_securityMode != SecurityMode.TLS)
                    return;

                m_certificateChecker.DistrustAll();
                m_subscriberIdentities.Clear();

                foreach (DataRow subscriber in DataSource.Tables["Subscribers"].Select("Enabled <> 0"))
                {
                    try
                    {
                        policy = new CertificatePolicy();
                        remoteCertificateFile = subscriber["RemoteCertificateFile"].ToNonNullString();

                        if (Enum.TryParse(subscriber["ValidPolicyErrors"].ToNonNullString(), out SslPolicyErrors validPolicyErrors))
                            policy.ValidPolicyErrors = validPolicyErrors;

                        if (Enum.TryParse(subscriber["ValidChainFlags"].ToNonNullString(), out X509ChainStatusFlags validChainFlags))
                            policy.ValidChainFlags = validChainFlags;

                        if (File.Exists(remoteCertificateFile))
                        {
                            certificate = new X509Certificate2(remoteCertificateFile);
                            m_certificateChecker.Trust(certificate, policy);
                            m_subscriberIdentities.Add(certificate, subscriber);
                        }
                    }
                    catch (Exception ex)
                    {
                        OnProcessException(MessageLevel.Error, new InvalidOperationException($"Failed to add subscriber \"{subscriber["Acronym"].ToNonNullNorEmptyString("[UNKNOWN]")}\" certificate to trusted certificates: {ex.Message}", ex));
                    }
                }
            }
            catch (Exception ex)
            {
                OnProcessException(MessageLevel.Error, new InvalidOperationException($"Failed to update certificate checker: {ex.Message}", ex));
            }
        }

        // Update rights for the given subscription.
        private void UpdateRights(SubscriberConnection connection)
        {
            if ((object)connection == null)
                return;

            try
            {
                SubscriberAdapter subscription = connection.Subscription;
                MeasurementKey[] requestedInputs;
                HashSet<MeasurementKey> authorizedSignals;
                Func<Guid, bool> hasRightsFunc = id => true;
                string message;

                // Determine if the connection has been disabled or removed - make sure to set authenticated to false if necessary
                if ((object)DataSource != null && DataSource.Tables.Contains("Subscribers") &&
                    !DataSource.Tables["Subscribers"].Select($"ID = '{connection.SubscriberID}' AND Enabled <> 0").Any())
                    connection.Authenticated = false;

                if ((object)subscription != null)
                {
                    // It is important here that "SELECT" not be allowed in parsing the input measurement keys expression since this key comes
                    // from the remote subscription - this will prevent possible SQL injection attacks.
                    requestedInputs = FilterExpressionParser.ParseInputMeasurementKeys(DataSource, subscription.RequestedInputFilter);
                    authorizedSignals = new HashSet<MeasurementKey>();

                    foreach (MeasurementKey input in requestedInputs)
                    {
                        if (hasRightsFunc(input.SignalID))
                            authorizedSignals.Add(input);
                    }

                    if (!authorizedSignals.SetEquals(subscription.InputMeasurementKeys))
                    {
                        // Update the subscription associated with this connection based on newly acquired or revoked rights
                        message = $"Update to authorized signals caused subscription to change. Now subscribed to {authorizedSignals.Count} signals.";
                        subscription.InputMeasurementKeys = authorizedSignals.ToArray();
                        SendClientResponse(subscription.ClientID, ServerResponse.Succeeded, ServerCommand.Subscribe, message);
                    }
                }
            }
            catch (Exception ex)
            {
                OnProcessException(MessageLevel.Error, new InvalidOperationException($"Failed to update authorized signal rights for \"{connection.ConnectionID}\" - connection will be terminated: {ex.Message}", ex));

                // If we can't assign rights, terminate connection
                ThreadPool.QueueUserWorkItem(DisconnectClient, connection.ClientID);
            }
        }

        // Send binary response packet to client
        private bool SendClientResponse(Guid clientID, byte responseCode, byte commandCode, byte[] data)
        {
            bool success = false;

            // Attempt to lookup associated client connection
            if (m_clientConnections.TryGetValue(clientID, out SubscriberConnection connection) && (object)connection != null && !connection.ClientNotFoundExceptionOccurred)
            {
                try
                {
                    // Create a new working buffer
                    using (MemoryStream workingBuffer = new MemoryStream())
                    {
                        bool dataPacketResponse = responseCode == (byte)ServerResponse.DataPacket;
                        bool useDataChannel = dataPacketResponse || responseCode == (byte)ServerResponse.BufferBlock;

                        // Add response code
                        workingBuffer.WriteByte(responseCode);

                        // Add original in response to command code
                        workingBuffer.WriteByte(commandCode);

                        if ((object)data == null || data.Length == 0)
                        {
                            // Add zero sized data buffer to response packet
                            workingBuffer.Write(ZeroLengthBytes, 0, 4);
                        }
                        else
                        {
                            // If response is for a data packet and a connection key is defined, encrypt the data packet payload
                            if (dataPacketResponse && (object)connection.KeyIVs != null)
                            {
                                // Get a local copy of volatile keyIVs and cipher index since these can change at any time
                                byte[][][] keyIVs = connection.KeyIVs;
                                int cipherIndex = connection.CipherIndex;

                                // Reserve space for size of data buffer to go into response packet
                                workingBuffer.Write(ZeroLengthBytes, 0, 4);

                                // Get data packet flags
                                DataPacketFlags flags = (DataPacketFlags)data[0];

                                // Encode current cipher index into data packet flags
                                if (cipherIndex > 0)
                                    flags |= DataPacketFlags.CipherIndex;

                                // Write data packet flags into response packet
                                workingBuffer.WriteByte((byte)flags);

                                // Copy source data payload into a memory stream
                                MemoryStream sourceData = new MemoryStream(data, 1, data.Length - 1);

                                // Encrypt payload portion of data packet and copy into the response packet
                                Common.SymmetricAlgorithm.Encrypt(sourceData, workingBuffer, keyIVs[cipherIndex][0], keyIVs[cipherIndex][1]);

                                // Calculate length of encrypted data payload
                                int payloadLength = (int)workingBuffer.Length - 6;

                                // Move the response packet position back to the packet size reservation
                                workingBuffer.Seek(2, SeekOrigin.Begin);

                                // Add the actual size of payload length to response packet
                                workingBuffer.Write(BigEndian.GetBytes(payloadLength), 0, 4);
                            }
                            else
                            {
                                // Add size of data buffer to response packet
                                workingBuffer.Write(BigEndian.GetBytes(data.Length), 0, 4);

                                // Add data buffer
                                workingBuffer.Write(data, 0, data.Length);
                            }
                        }

                        IServer publishChannel;

                        // Data packets and buffer blocks can be published on a UDP data channel, so check for this...
                        if (useDataChannel)
                            publishChannel = m_clientPublicationChannels.GetOrAdd(clientID, id => (object)connection != null ? connection.PublishChannel : m_commandChannel);
                        else
                            publishChannel = m_commandChannel;

                        // Send response packet
                        if ((object)publishChannel != null && publishChannel.CurrentState == ServerState.Running)
                        {
                            byte[] responseData = workingBuffer.ToArray();

                            if (publishChannel is UdpServer)
                                publishChannel.MulticastAsync(responseData, 0, responseData.Length);
                            else
                                publishChannel.SendToAsync(clientID, responseData, 0, responseData.Length);

                            m_totalBytesSent += responseData.Length;
                            success = true;
                        }
                    }
                }
                catch (ObjectDisposedException)
                {
                    // This happens when there is still data to be sent to a disconnected client - we can safely ignore this exception
                }
                catch (NullReferenceException)
                {
                    // This happens when there is still data to be sent to a disconnected client - we can safely ignore this exception
                }
                catch (SocketException ex)
                {
                    if (!HandleSocketException(clientID, ex))
                        OnProcessException(MessageLevel.Info, new InvalidOperationException($"Failed to send response packet to client due to exception: {ex.Message}", ex));
                }
                catch (InvalidOperationException ex)
                {
                    // Could still be processing threads with client data after client has been disconnected, this can be safely ignored
                    if (ex.Message.StartsWith("No client found") && !connection.IsConnected)
                        connection.ClientNotFoundExceptionOccurred = true;
                    else
                        OnProcessException(MessageLevel.Info, new InvalidOperationException($"Failed to send response packet to client due to exception: {ex.Message}", ex));
                }
                catch (Exception ex)
                {
                    OnProcessException(MessageLevel.Info, new InvalidOperationException($"Failed to send response packet to client due to exception: {ex.Message}", ex));
                }
            }

            return success;
        }

        // Socket exception handler
        private bool HandleSocketException(Guid clientID, Exception ex)
        {
            SocketException socketException = ex as SocketException;

            if ((object)socketException != null)
            {
                // WSAECONNABORTED and WSAECONNRESET are common errors after a client disconnect,
                // if they happen for other reasons, make sure disconnect procedure is handled
                if (socketException.ErrorCode == 10053 || socketException.ErrorCode == 10054)
                {
                    try
                    {
                        ThreadPool.QueueUserWorkItem(DisconnectClient, clientID);
                    }
                    catch (Exception queueException)
                    {
                        // Process exception for logging
                        OnProcessException(MessageLevel.Info, new InvalidOperationException($"Failed to queue client disconnect due to exception: {queueException.Message}", queueException));
                    }

                    return true;
                }
            }

            if ((object)ex != null)
                HandleSocketException(clientID, ex.InnerException);

            return false;
        }

        // Disconnect client - this should be called from non-blocking thread (e.g., thread pool)
        private void DisconnectClient(object state)
        {
            try
            {
                Guid clientID = (Guid)state;

                RemoveClientSubscription(clientID);

                if (m_clientConnections.TryRemove(clientID, out SubscriberConnection connection))
                {
                    connection.Dispose();
                    OnStatusMessage(MessageLevel.Info, "Client disconnected from command channel.");
                }

                m_clientPublicationChannels.TryRemove(clientID, out IServer publicationChannel);
            }
            catch (Exception ex)
            {
                OnProcessException(MessageLevel.Info, new InvalidOperationException($"Encountered an exception while processing client disconnect: {ex.Message}", ex));
            }
        }

        // Remove client subscription
        private void RemoveClientSubscription(Guid clientID)
        {
            lock (this)
            {
                if (TryGetClientSubscription(clientID, out SubscriberAdapter clientSubscription))
                {
                    clientSubscription.Stop();
                    Remove(clientSubscription);

                    try
                    {
                        // Notify system that subscriber disconnected therefore demanded measurements may have changed
                        ThreadPool.QueueUserWorkItem(NotifyHostOfSubscriptionRemoval);
                    }
                    catch (Exception ex)
                    {
                        // Process exception for logging
                        OnProcessException(MessageLevel.Warning, new InvalidOperationException($"Failed to queue notification of subscription removal due to exception: {ex.Message}", ex));
                    }
                }
            }
        }

        // Handle notification on input measurement key change
        private void NotifyHostOfSubscriptionRemoval(object state)
        {
            OnInputMeasurementKeysUpdated();
        }

        // Attempt to find client subscription
        private bool TryGetClientSubscription(Guid clientID, out SubscriberAdapter subscription)
        {
            IActionAdapter adapter;

            // Lookup adapter by its client ID
            if (TryGetAdapter(clientID, GetClientSubscription, out adapter))
            {
                subscription = (SubscriberAdapter)adapter;
                return true;
            }

            subscription = null;
            return false;
        }

        private bool GetClientSubscription(IActionAdapter item, Guid value)
        {
            SubscriberAdapter subscription = item as SubscriberAdapter;

            if ((object)subscription != null)
                return subscription.ClientID == value;

            return false;
        }

        // Gets specified property from client connection based on subscriber ID
        private TResult GetConnectionProperty<TResult>(Guid subscriberID, Func<SubscriberConnection, TResult> predicate)
        {
            TResult result = default(TResult);

            // Lookup client connection by subscriber ID
            SubscriberConnection connection = m_clientConnections.Values.FirstOrDefault(cc => cc.SubscriberID == subscriberID);

            // Extract desired property from client connection using given predicate function
            if ((object)connection != null)
                result = predicate(connection);

            return result;
        }

        /// <summary>
        /// Raises the <see cref="ProcessingComplete"/> event.
        /// </summary>
        protected virtual void OnProcessingComplete()
        {
            try
            {
                ProcessingComplete?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                // We protect our code from consumer thrown exceptions
                OnProcessException(MessageLevel.Info, new InvalidOperationException($"Exception in consumer handler for ProcessingComplete event: {ex.Message}", ex), "ConsumerEventException");
            }
        }

        /// <summary>
        /// Raises the <see cref="ClientConnected"/> event.
        /// </summary>
        /// <param name="subscriberID">Subscriber <see cref="Guid"/> (normally <see cref="SubscriberConnection.SubscriberID"/>).</param>
        /// <param name="connectionID">Connection identification (normally <see cref="SubscriberConnection.ConnectionID"/>).</param>
        /// <param name="subscriberInfo">Subscriber information (normally <see cref="SubscriberConnection.SubscriberInfo"/>).</param>
        protected virtual void OnClientConnected(Guid subscriberID, string connectionID, string subscriberInfo)
        {
            try
            {
                ClientConnected?.Invoke(this, new EventArgs<Guid, string, string>(subscriberID, connectionID, subscriberInfo));
            }
            catch (Exception ex)
            {
                // We protect our code from consumer thrown exceptions
                OnProcessException(MessageLevel.Info, new InvalidOperationException($"Exception in consumer handler for ClientConnected event: {ex.Message}", ex), "ConsumerEventException");
            }
        }

        protected internal virtual void OnStatusMessage(MessageLevel level, string status, string eventName = null)
        {
            // TODO: Raise event
            //base.OnStatusMessage(level, status, eventName, flags);
        }

        protected internal virtual void OnProcessException(MessageLevel level, Exception exception, string eventName = null)
        {
            // TODO: Raise event
            //base.OnProcessException(level, exception, eventName, flags);
        }

        // Make sure to expose any routing table messages
        private void m_routingTables_StatusMessage(object sender, EventArgs<string> e) => OnStatusMessage(MessageLevel.Info, e.Argument);

        // Make sure to expose any routing table exceptions
        private void m_routingTables_ProcessException(object sender, EventArgs<Exception> e) => OnProcessException(MessageLevel.Warning, e.Argument);

        // Cipher key rotation timer handler
        private void m_cipherKeyRotationTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if ((object)m_clientConnections != null)
            {
                foreach (SubscriberConnection connection in m_clientConnections.Values)
                {
                    if ((object)connection != null && connection.Authenticated)
                        connection.RotateCipherKeys();
                }
            }
        }

        // Command channel restart timer handler
        private void m_commandChannelRestartTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if ((object)m_commandChannel != null)
            {
                try
                {
                    // After a short delay, we try to restart the command channel
                    m_commandChannel.Start();
                }
                catch (Exception ex)
                {
                    OnProcessException(MessageLevel.Warning, new InvalidOperationException($"Failed to restart data publisher command channel: {ex.Message}", ex));
                }
            }
        }

        // Determines whether the data in the data source has actually changed when receiving a new data source.
        private bool DataSourceChanged(DataSet newDataSource)
        {
            try
            {
                return !DataSetEqualityComparer.Default.Equals(DataSource, newDataSource);
            }
            catch
            {
                return true;
            }
        }

        #region [ Server Command Request Handlers ]

        // Handles subscribe request
        private void HandleSubscribeRequest(SubscriberConnection connection, byte[] buffer, int startIndex, int length)
        {
            Guid clientID = connection.ClientID;
            SubscriberAdapter subscription;
            string message;

            // Handle subscribe
            try
            {
                // Make sure there is enough buffer for flags and connection string length
                if (length >= 6)
                {
                    // Next byte is the data packet flags
                    DataPacketFlags flags = (DataPacketFlags)buffer[startIndex];
                    startIndex++;

                    bool usePayloadCompression = m_allowPayloadCompression && ((connection.OperationalModes & OperationalModes.CompressPayloadData) > 0);
                    CompressionModes compressionModes = (CompressionModes)(connection.OperationalModes & OperationalModes.CompressionModeMask);
                    bool useCompactMeasurementFormat = (byte)(flags & DataPacketFlags.Compact) > 0;
                    bool addSubscription = false;

                    // Next 4 bytes are an integer representing the length of the connection string that follows
                    int byteLength = BigEndian.ToInt32(buffer, startIndex);
                    startIndex += 4;

                    if (byteLength > 0 && length >= 6 + byteLength)
                    {
                        string connectionString = GetClientEncoding(clientID).GetString(buffer, startIndex, byteLength);
                        //startIndex += byteLength;

                        // Get client subscription
                        if ((object)connection.Subscription == null)
                            TryGetClientSubscription(clientID, out subscription);
                        else
                            subscription = connection.Subscription;

                        if ((object)subscription == null)
                        {
                            // Client subscription not established yet, so we create a new one
                            subscription = new SubscriberAdapter(this, clientID, connection.SubscriberID, compressionModes);
                            addSubscription = true;
                        }

                        // Update client subscription properties
                        subscription.ConnectionString = connectionString;
                        subscription.DataSource = DataSource;

                        // Pass subscriber assembly information to connection, if defined
                        if (subscription.Settings.TryGetValue("assemblyInfo", out string setting))
                            connection.SubscriberInfo = setting;

                        // Set up UDP data channel if client has requested this
                        connection.DataChannel = null;

                        if (subscription.Settings.TryGetValue("dataChannel", out setting))
                        {
                            Socket clientSocket = connection.GetCommandChannelSocket();
                            Dictionary<string, string> settings = setting.ParseKeyValuePairs();
                            IPEndPoint localEndPoint = null;
                            string networkInterface = "::0";

                            // Make sure return interface matches incoming client connection
                            if ((object)clientSocket != null)
                                localEndPoint = clientSocket.LocalEndPoint as IPEndPoint;

                            if ((object)localEndPoint != null)
                            {
                                networkInterface = localEndPoint.Address.ToString();

                                // Remove dual-stack prefix
                                if (networkInterface.StartsWith("::ffff:", true, CultureInfo.InvariantCulture))
                                    networkInterface = networkInterface.Substring(7);
                            }

                            if (settings.TryGetValue("port", out setting) || settings.TryGetValue("localport", out setting))
                            {
                                if ((compressionModes & CompressionModes.TSSC) > 0)
                                {
                                    // TSSC is a stateful compression algorithm which will not reliably support UDP
                                    OnStatusMessage(MessageLevel.Warning, "Cannot use TSSC compression mode with UDP - special compression mode disabled");

                                    // Disable TSSC compression processing
                                    compressionModes &= ~CompressionModes.TSSC;
                                    connection.OperationalModes &= ~OperationalModes.CompressionModeMask;
                                    connection.OperationalModes |= (OperationalModes)compressionModes;
                                }

                                connection.DataChannel = new UdpServer($"Port=-1; Clients={connection.IPAddress}:{int.Parse(setting)}; interface={networkInterface}");
                                connection.DataChannel.Start();
                            }

                            // Remove any existing cached publication channel since connection is changing
                            m_clientPublicationChannels.TryRemove(clientID, out IServer publicationChannel);

                            // Update payload compression state and strength
                            subscription.UsePayloadCompression = usePayloadCompression;

                            // Update measurement serialization format type
                            subscription.UseCompactMeasurementFormat = useCompactMeasurementFormat;

                            // Track subscription in connection information
                            connection.Subscription = subscription;

                            if (addSubscription)
                            {
                                // Adding client subscription to collection will not automatically
                                // initialize it because this class overrides the AutoInitialize property
                                lock (this)
                                {
                                    Add(subscription);
                                }

                                // Attach to processing completed notification
                                subscription.BufferBlockRetransmission += subscription_BufferBlockRetransmission;
                                subscription.ProcessingComplete += subscription_ProcessingComplete;
                            }

                            // Make sure temporal support is initialized
                            OnRequestTemporalSupport();

                            // Manually initialize client subscription
                            // Subscribed signals (i.e., input measurement keys) will be parsed from connection string during
                            // initialization of adapter. This should also gracefully handle "resubscribing" which can add and
                            // remove subscribed points since assignment and use of input measurement keys is synchronized
                            // within the client subscription class
                            subscription.Initialize();
                            subscription.Initialized = true;

                            // Update measurement reporting interval post-initialization
                            subscription.MeasurementReportingInterval = MeasurementReportingInterval;

                            // Send updated signal index cache to client with validated rights of the selected input measurement keys
                            byte[] serializedSignalIndexCache = SerializeSignalIndexCache(clientID, subscription.SignalIndexCache);
                            SendClientResponse(clientID, ServerResponse.UpdateSignalIndexCache, ServerCommand.Subscribe, serializedSignalIndexCache);

                            // Send new or updated cipher keys
                            if (connection.Authenticated && m_encryptPayload)
                                connection.RotateCipherKeys();

                            // The subscription adapter must be started before sending
                            // cached measurements or else they will be ignored
                            subscription.Start();

                            // Spawn routing table recalculation after sending cached measurements--
                            // there is a bit of a race condition that could cause the subscriber to
                            // miss some data points that arrive during the routing table calculation,
                            // but data will not be provided out of order (except maybe on resubscribe)
                            OnInputMeasurementKeysUpdated();
                            m_routingTables.CalculateRoutingTables(null);

                            // Notify any direct publisher consumers about the new client connection
                            try
                            {
                                OnClientConnected(connection.SubscriberID, connection.ConnectionID, connection.SubscriberInfo);
                            }
                            catch (Exception ex)
                            {
                                OnProcessException(MessageLevel.Info, new InvalidOperationException($"ClientConnected event handler exception: {ex.Message}", ex));
                            }

                            // Send success response
                            if (subscription.TemporalConstraintIsDefined())
                            {
                                message = $"Client subscribed as {(useCompactMeasurementFormat ? "" : "non-")}compact with a temporal constraint.";
                            }
                            else
                            {
                                if ((object)subscription.InputMeasurementKeys != null)
                                    message = $"Client subscribed as {(useCompactMeasurementFormat ? "" : "non-")}compact with {subscription.InputMeasurementKeys.Length} signals.";
                                else
                                    message = $"Client subscribed as {(useCompactMeasurementFormat ? "" : "non-")}compact, but no signals were specified. Make sure \"inputMeasurementKeys\" setting is properly defined.";
                            }

                            connection.IsSubscribed = true;
                            SendClientResponse(clientID, ServerResponse.Succeeded, ServerCommand.Subscribe, message);
                            OnStatusMessage(MessageLevel.Info, message);
                        }
                        else
                        {
                            if (byteLength > 0) //-V3022
                                message = "Not enough buffer was provided to parse client data subscription.";
                            else
                                message = "Cannot initialize client data subscription without a connection string.";

                            SendClientResponse(clientID, ServerResponse.Failed, ServerCommand.Subscribe, message);
                            OnProcessException(MessageLevel.Warning, new InvalidOperationException(message));
                        }
                    }
                }
                else
                {
                    message = "Not enough buffer was provided to parse client data subscription.";
                    SendClientResponse(clientID, ServerResponse.Failed, ServerCommand.Subscribe, message);
                    OnProcessException(MessageLevel.Warning, new InvalidOperationException(message));
                }
            }
            catch (Exception ex)
            {
                message = $"Failed to process client data subscription due to exception: {ex.Message}";
                SendClientResponse(clientID, ServerResponse.Failed, ServerCommand.Subscribe, message);
                OnProcessException(MessageLevel.Warning, new InvalidOperationException(message, ex));
            }
        }

        // Handles unsubscribe request
        private void HandleUnsubscribeRequest(SubscriberConnection connection)
        {
            Guid clientID = connection.ClientID;

            RemoveClientSubscription(clientID); // This does not disconnect client command channel - nor should it...

            // Detach from processing completed notification
            if ((object)connection.Subscription != null)
            {
                connection.Subscription.BufferBlockRetransmission -= subscription_BufferBlockRetransmission;
                connection.Subscription.ProcessingComplete -= subscription_ProcessingComplete;
            }

            connection.Subscription = null;
            connection.IsSubscribed = false;

            SendClientResponse(clientID, ServerResponse.Succeeded, ServerCommand.Unsubscribe, "Client unsubscribed.");
            OnStatusMessage(MessageLevel.Info, $"{connection.ConnectionID} unsubscribed.");
        }

        /// <summary>
        /// Gets meta-data to return to <see cref="DataSubscriber"/>.
        /// </summary>
        /// <param name="connection">Client connection requesting meta-data.</param>
        /// <param name="filterExpressions">Any meta-data filter expressions requested by client.</param>
        /// <returns>Meta-data to be returned to client.</returns>
        protected virtual DataSet AquireMetadata(SubscriberConnection connection, Dictionary<string, Tuple<string, string, int>> filterExpressions)
        {
            return null;
        }

        // Handles meta-data refresh request
        private void HandleMetadataRefresh(SubscriberConnection connection, byte[] buffer, int startIndex, int length)
        {
            // Ensure that the subscriber is allowed to request meta-data
            if (!m_allowMetadataRefresh)
                throw new InvalidOperationException("Meta-data refresh has been disallowed by the DataPublisher.");

            OnStatusMessage(MessageLevel.Info, $"Received meta-data refresh request from {connection.ConnectionID}, preparing response...");

            Guid clientID = connection.ClientID;
            Dictionary<string, Tuple<string, string, int>> filterExpressions = new Dictionary<string, Tuple<string, string, int>>(StringComparer.OrdinalIgnoreCase);
            string message;
            Ticks startTime = DateTime.UtcNow.Ticks;

            // Attempt to parse out any subscriber provided meta-data filter expressions
            try
            {
                // Note that these client provided meta-data filter expressions are applied post SQL data retrieval to the limited-capability 
                // DataTable.Select() function against an in-memory DataSet and therefore are not subject to SQL injection attacks
                if (length > 4)
                {
                    int responseLength = BigEndian.ToInt32(buffer, startIndex);
                    startIndex += 4;

                    if (length >= responseLength + 4)
                    {
                        string metadataFilters = GetClientEncoding(clientID).GetString(buffer, startIndex, responseLength);
                        string[] expressions = metadataFilters.Split(';');

                        // Go through each subscriber specified filter expressions
                        foreach (string expression in expressions)
                        {
                            // Attempt to parse filter expression and add it dictionary if successful
                            if (FilterExpressionParser.ParseFilterExpression(expression, out string tableName, out string filterExpression, out string sortField, out int takeCount))
                                filterExpressions.Add(tableName, Tuple.Create(filterExpression, sortField, takeCount));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnProcessException(MessageLevel.Warning, new InvalidOperationException($"Failed to parse subscriber provided meta-data filter expressions: {ex.Message}", ex));
            }

            try
            {
                DataSet metadata = AquireMetadata(connection, filterExpressions);
                byte[] serializedMetadata = SerializeMetadata(clientID, metadata);
                long rowCount = metadata.Tables.Cast<DataTable>().Select(dataTable => (long)dataTable.Rows.Count).Sum();

                if (rowCount > 0)
                {
                    Time elapsedTime = (DateTime.UtcNow.Ticks - startTime).ToSeconds();
                    OnStatusMessage(MessageLevel.Info, $"{rowCount:N0} records spanning {metadata.Tables.Count:N0} tables of meta-data prepared in {elapsedTime.ToString(2)}, sending response to {connection.ConnectionID}...");
                }
                else
                {
                    OnStatusMessage(MessageLevel.Info, $"No meta-data is available, sending an empty response to {connection.ConnectionID}...");
                }

                SendClientResponse(clientID, ServerResponse.Succeeded, ServerCommand.MetaDataRefresh, serializedMetadata);
            }
            catch (Exception ex)
            {
                message = $"Failed to transfer meta-data due to exception: {ex.Message}";
                SendClientResponse(clientID, ServerResponse.Failed, ServerCommand.MetaDataRefresh, message);
                OnProcessException(MessageLevel.Warning, new InvalidOperationException(message, ex));
            }
        }

        // Handles request to update processing interval on client session
        private void HandleUpdateProcessingInterval(SubscriberConnection connection, byte[] buffer, int startIndex, int length)
        {
            Guid clientID = connection.ClientID;
            string message;

            // Make sure there is enough buffer for new processing interval value
            if (length >= 4)
            {
                // Next 4 bytes are an integer representing the new processing interval
                int processingInterval = BigEndian.ToInt32(buffer, startIndex);

                SubscriberAdapter subscription = connection.Subscription;

                if ((object)subscription != null)
                {
                    subscription.ProcessingInterval = processingInterval;
                    SendClientResponse(clientID, ServerResponse.Succeeded, ServerCommand.UpdateProcessingInterval, "New processing interval of {0} assigned.", processingInterval);
                    OnStatusMessage(MessageLevel.Info, $"{connection.ConnectionID} was assigned a new processing interval of {processingInterval}.");
                }
                else
                {
                    message = "Client subscription was not available, could not update processing interval.";
                    SendClientResponse(clientID, ServerResponse.Failed, ServerCommand.UpdateProcessingInterval, message);
                    OnProcessException(MessageLevel.Info, new InvalidOperationException(message));
                }
            }
            else
            {
                message = "Not enough buffer was provided to update client processing interval.";
                SendClientResponse(clientID, ServerResponse.Failed, ServerCommand.UpdateProcessingInterval, message);
                OnProcessException(MessageLevel.Warning, new InvalidOperationException(message));
            }
        }

        // Handle request to define operational modes for client connection
        private void HandleDefineOperationalModes(SubscriberConnection connection, byte[] buffer, int startIndex, int length)
        {
            uint operationalModes;

            if (length >= 4)
            {
                operationalModes = BigEndian.ToUInt32(buffer, startIndex);

                if ((operationalModes & (uint)OperationalModes.VersionMask) != 1u)
                    OnStatusMessage(MessageLevel.Warning, $"Protocol version not supported. Operational modes may not be set correctly for client {connection.ClientID}.");

                connection.OperationalModes = (OperationalModes)operationalModes;
            }
        }

        // Handle confirmation of receipt of notification 
        private void HandleConfirmNotification(SubscriberConnection connection, byte[] buffer, int startIndex, int length)
        {
            int hash = BigEndian.ToInt32(buffer, startIndex);

            if (length >= 4)
            {
                lock (m_clientNotificationsLock)
                {
                    if (m_clientNotifications.TryGetValue(connection.SubscriberID, out Dictionary<int, string> notifications))
                    {
                        if (notifications.TryGetValue(hash, out string notification))
                        {
                            notifications.Remove(hash);
                            OnStatusMessage(MessageLevel.Info, $"Subscriber {connection.ConnectionID} confirmed receipt of notification: {notification}.");
                            SerializeClientNotifications();
                        }
                        else
                        {
                            OnStatusMessage(MessageLevel.Info, $"Confirmation for unknown notification received from client {connection.ConnectionID}.");
                        }
                    }
                    else
                    {
                        OnStatusMessage(MessageLevel.Info, "Unsolicited confirmation of notification received.");
                    }
                }
            }
            else
            {
                OnStatusMessage(MessageLevel.Info, "Malformed notification confirmation received.");
            }
        }

        private void HandleConfirmBufferBlock(SubscriberConnection connection, byte[] buffer, int startIndex, int length)
        {
            uint sequenceNumber;

            if (length >= 4)
            {
                sequenceNumber = BigEndian.ToUInt32(buffer, startIndex);
                connection.Subscription.ConfirmBufferBlock(sequenceNumber);
            }
        }

        /// <summary>
        /// Handles custom commands defined by the user of the publisher API.
        /// </summary>
        /// <param name="connection">Object representing the connection to the data subscriber.</param>
        /// <param name="command">The command issued by the subscriber.</param>
        /// <param name="buffer">The buffer containing the entire message from the subscriber.</param>
        /// <param name="startIndex">The index indicating where to start reading from the buffer to skip past the message header.</param>
        /// <param name="length">The total number of bytes in the message, including the header.</param>
        protected virtual void HandleUserCommand(SubscriberConnection connection, ServerCommand command, byte[] buffer, int startIndex, int length)
        {
            OnStatusMessage(MessageLevel.Info, $"Received command code for user-defined command \"{command}\".");
        }

        private byte[] SerializeSignalIndexCache(Guid clientID, SignalIndexCache signalIndexCache)
        {
            byte[] serializedSignalIndexCache = null;

            if (m_clientConnections.TryGetValue(clientID, out SubscriberConnection connection))
            {
                OperationalModes operationalModes = connection.OperationalModes;
                CompressionModes compressionModes = (CompressionModes)(operationalModes & OperationalModes.CompressionModeMask);
                bool compressSignalIndexCache = (operationalModes & OperationalModes.CompressSignalIndexCache) > 0;
                GZipStream deflater = null;

                signalIndexCache.Encoding = GetClientEncoding(clientID);
                serializedSignalIndexCache = new byte[signalIndexCache.BinaryLength];
                signalIndexCache.GenerateBinaryImage(serializedSignalIndexCache, 0);

                if (compressSignalIndexCache && compressionModes.HasFlag(CompressionModes.GZip))
                {
                    try
                    {
                        // Compress serialized signal index cache into compressed data buffer
                        using (MemoryStream compressedData = new MemoryStream())
                        {
                            deflater = new GZipStream(compressedData, CompressionMode.Compress, true);
                            deflater.Write(serializedSignalIndexCache, 0, serializedSignalIndexCache.Length);
                            deflater.Close();
                            deflater = null;

                            serializedSignalIndexCache = compressedData.ToArray();
                        }
                    }
                    finally
                    {
                        if ((object)deflater != null)
                            deflater.Close();
                    }
                }
            }

            return serializedSignalIndexCache;
        }

        private byte[] SerializeMetadata(Guid clientID, DataSet metadata)
        {
            byte[] serializedMetadata = null;

            if (m_clientConnections.TryGetValue(clientID, out SubscriberConnection connection))
            {
                OperationalModes operationalModes = connection.OperationalModes;
                CompressionModes compressionModes = (CompressionModes)(operationalModes & OperationalModes.CompressionModeMask);
                bool compressMetadata = (operationalModes & OperationalModes.CompressMetadata) > 0;
                GZipStream deflater = null;

                // Encode XML into encoded data buffer
                using (MemoryStream encodedData = new MemoryStream())
                using (XmlTextWriter xmlWriter = new XmlTextWriter(encodedData, GetClientEncoding(clientID)))
                {
                    metadata.WriteXml(xmlWriter, XmlWriteMode.WriteSchema);
                    xmlWriter.Flush();

                    // Return result of encoding
                    serializedMetadata = encodedData.ToArray();
                }

                if (compressMetadata && compressionModes.HasFlag(CompressionModes.GZip))
                {
                    try
                    {
                        // Compress serialized metadata into compressed data buffer
                        using (MemoryStream compressedData = new MemoryStream())
                        {
                            deflater = new GZipStream(compressedData, CompressionMode.Compress, true);
                            deflater.Write(serializedMetadata, 0, serializedMetadata.Length);
                            deflater.Close();
                            deflater = null;

                            // Return result of compression
                            serializedMetadata = compressedData.ToArray();
                        }
                    }
                    finally
                    {
                        if ((object)deflater != null)
                            deflater.Close();
                    }
                }
            }

            return serializedMetadata;
        }

        // Updates the measurements per second counters after receiving another set of measurements.
        private void UpdateMeasurementsPerSecond(int measurementCount)
        {
            long secondsSinceEpoch = DateTime.UtcNow.Ticks / Ticks.PerSecond;

            if (secondsSinceEpoch > m_lastSecondsSinceEpoch)
            {
                if (m_measurementsInSecond < m_minimumMeasurementsPerSecond || m_minimumMeasurementsPerSecond == 0L)
                    m_minimumMeasurementsPerSecond = m_measurementsInSecond;

                if (m_measurementsInSecond > m_maximumMeasurementsPerSecond || m_maximumMeasurementsPerSecond == 0L)
                    m_maximumMeasurementsPerSecond = m_measurementsInSecond;

                m_totalMeasurementsPerSecond += m_measurementsInSecond;
                m_measurementsPerSecondCount++;
                m_measurementsInSecond = 0L;

                m_lastSecondsSinceEpoch = secondsSinceEpoch;
            }

            m_measurementsInSecond += measurementCount;
        }

        // Resets the measurements per second counters after reading the values from the last calculation interval.
        private void ResetMeasurementsPerSecondCounters()
        {
            m_minimumMeasurementsPerSecond = 0L;
            m_maximumMeasurementsPerSecond = 0L;
            m_totalMeasurementsPerSecond = 0L;
            m_measurementsPerSecondCount = 0L;
        }

        private void subscription_BufferBlockRetransmission(object sender, EventArgs eventArgs)
        {
            m_bufferBlockRetransmissions++;
        }

        // Bubble up processing complete notifications from subscriptions
        private void subscription_ProcessingComplete(object sender, EventArgs<SubscriberAdapter, EventArgs> e)
        {
            // Expose notification via data publisher event subscribers
            ProcessingComplete?.Invoke(sender, e.Argument2);

            SubscriberAdapter subscription = e.Argument1;
            string senderType = (object)sender == null ? "N/A" : sender.GetType().Name;

            // Send direct notification to associated client
            if ((object)subscription != null)
                SendClientResponse(subscription.ClientID, ServerResponse.ProcessingComplete, ServerCommand.Subscribe, senderType);
        }

        #endregion

        #region [ Command Channel Handlers ]

        private int ServerCommandLength(byte[] buffer, int length)
        {
            const int HeaderSize = 5;
            int commandLength = HeaderSize;

            if (length >= HeaderSize)
                commandLength += BigEndian.ToInt32(buffer, 1);

            return commandLength;
        }

        private void HandleServerCommand(Guid clientID, byte[] buffer, int length)
        {
            try
            {
                int index = 0;

                if (length > 0 && (object)buffer != null)
                {
                    string message;
                    byte commandByte = buffer[index];
                    index += 5;

                    // Attempt to parse solicited server command
                    bool validServerCommand = Enum.TryParse(commandByte.ToString(), out ServerCommand command);

                    // Look up this client connection
                    if (!m_clientConnections.TryGetValue(clientID, out SubscriberConnection connection))
                    {
                        // Received a request from an unknown client, this request is denied
                        OnStatusMessage(MessageLevel.Warning, $"Ignored {length} byte {(validServerCommand ? command.ToString() : "unidentified")} command request received from an unrecognized client: {clientID}");
                    }
                    else if (validServerCommand)
                    {
                        switch (command)
                        {
                            case ServerCommand.Subscribe:
                                // Handle subscribe
                                HandleSubscribeRequest(connection, buffer, index, length);
                                break;

                            case ServerCommand.Unsubscribe:
                                // Handle unsubscribe
                                HandleUnsubscribeRequest(connection);
                                break;

                            case ServerCommand.MetaDataRefresh:
                                // Handle meta data refresh (per subscriber request)
                                HandleMetadataRefresh(connection, buffer, index, length);
                                break;

                            case ServerCommand.RotateCipherKeys:
                                // Handle rotation of cipher keys (per subscriber request)
                                connection.RotateCipherKeys();
                                break;

                            case ServerCommand.UpdateProcessingInterval:
                                // Handle request to update processing interval
                                HandleUpdateProcessingInterval(connection, buffer, index, length);
                                break;

                            case ServerCommand.DefineOperationalModes:
                                // Handle request to define operational modes
                                HandleDefineOperationalModes(connection, buffer, index, length);
                                break;

                            case ServerCommand.ConfirmNotification:
                                // Handle confirmation of receipt of notification
                                HandleConfirmNotification(connection, buffer, index, length);
                                break;

                            case ServerCommand.ConfirmBufferBlock:
                                // Handle confirmation of receipt of a buffer block
                                HandleConfirmBufferBlock(connection, buffer, index, length);
                                break;

                            case ServerCommand.UserCommand00:
                            case ServerCommand.UserCommand01:
                            case ServerCommand.UserCommand02:
                            case ServerCommand.UserCommand03:
                            case ServerCommand.UserCommand04:
                            case ServerCommand.UserCommand05:
                            case ServerCommand.UserCommand06:
                            case ServerCommand.UserCommand07:
                            case ServerCommand.UserCommand08:
                            case ServerCommand.UserCommand09:
                            case ServerCommand.UserCommand10:
                            case ServerCommand.UserCommand11:
                            case ServerCommand.UserCommand12:
                            case ServerCommand.UserCommand13:
                            case ServerCommand.UserCommand14:
                            case ServerCommand.UserCommand15:
                                // Handle confirmation of receipt of a user-defined command
                                HandleUserCommand(connection, command, buffer, index, length);
                                break;
                        }
                    }
                    else
                    {
                        // Handle unrecognized commands
                        message = $" sent an unrecognized server command: 0x{commandByte.ToString("X").PadLeft(2, '0')}";
                        SendClientResponse(clientID, (byte)ServerResponse.Failed, commandByte, GetClientEncoding(clientID).GetBytes($"Client{message}"));
                        OnProcessException(MessageLevel.Warning, new InvalidOperationException(connection.ConnectionID + message));
                    }
                }
            }
            catch (Exception ex)
            {
                OnProcessException(MessageLevel.Warning, new InvalidOperationException($"Encountered an exception while processing received client data: {ex.Message}", ex));
            }
        }

        private void m_commandChannel_ReceiveClientDataComplete(object sender, EventArgs<Guid, byte[], int> e)
        {
            Guid clientID = e.Argument1;
            byte[] clientBuffer = e.Argument2;
            int bytesReceived = e.Argument3;

            if (!m_clientConnections.TryGetValue(clientID, out SubscriberConnection connection))
                return;

            byte[] buffer = connection.ClientDataBuffer;
            int bufferLength = connection.ClientDataBufferLength;

            using (MemoryStream clientData = new MemoryStream(clientBuffer, 0, bytesReceived))
            {
                int totalBytesRead = 0;
                int commandLength = ServerCommandLength(buffer, bufferLength);

                while (totalBytesRead < bytesReceived)
                {
                    if (buffer == null)
                        buffer = new byte[commandLength];
                    else if (buffer.Length < commandLength)
                        Array.Resize(ref buffer, commandLength);

                    int readLength = commandLength - bufferLength;
                    int bytesRead = clientData.Read(buffer, bufferLength, readLength);
                    totalBytesRead += bytesRead;
                    bufferLength += bytesRead;

                    // Additional data may have provided more
                    // intelligence about the full command length
                    if (bufferLength == commandLength)
                        commandLength = ServerCommandLength(buffer, bufferLength);

                    // If the command length hasn't changed,
                    // it's time to process the command
                    if (bufferLength == commandLength)
                    {
                        HandleServerCommand(clientID, buffer, bufferLength);
                        bufferLength = 0;
                        commandLength = ServerCommandLength(buffer, bufferLength);
                    }
                }
            }


            connection.ClientDataBuffer = buffer;
            connection.ClientDataBufferLength = bufferLength;
        }

        private void m_commandChannel_ClientConnected(object sender, EventArgs<Guid> e)
        {
            Guid clientID = e.Argument;
            SubscriberConnection connection = new SubscriberConnection(this, clientID, m_commandChannel);

            connection.ClientNotFoundExceptionOccurred = false;

            if (m_securityMode == SecurityMode.TLS)
            {
                TryFindClientDetails(connection);
                connection.Authenticated = connection.ValidIPAddresses.Contains(connection.IPAddress);
            }

            m_clientConnections[clientID] = connection;

            OnStatusMessage(MessageLevel.Info, "Client connected to command channel.");

            if (connection.Authenticated)
            {
                lock (m_clientNotificationsLock)
                {
                    // Send any queued notifications to authenticated client
                    SendNotifications(connection);
                }
            }
            else if (m_securityMode == SecurityMode.TLS)
            {
                const string ErrorFormat = "Unable to authenticate client. Client connected using" +
                    " certificate of subscriber \"{0}\", however the IP address used ({1}) was" +
                    " not found among the list of valid IP addresses.";

                string errorMessage = string.Format(ErrorFormat, connection.SubscriberName, connection.IPAddress);

                OnProcessException(MessageLevel.Warning, new InvalidOperationException(errorMessage));
            }
        }

        private void m_commandChannel_ClientDisconnected(object sender, EventArgs<Guid> e)
        {
            try
            {
                ThreadPool.QueueUserWorkItem(DisconnectClient, e.Argument);
            }
            catch (Exception ex)
            {
                // Process exception for logging
                OnProcessException(MessageLevel.Info, new InvalidOperationException($"Failed to queue client disconnect due to exception: {ex.Message}", ex));
            }
        }

        private void m_commandChannel_ClientConnectingException(object sender, EventArgs<Exception> e)
        {
            Exception ex = e.Argument;
            OnProcessException(MessageLevel.Info, new InvalidOperationException($"Data publisher encountered an exception while connecting client to the command channel: {ex.Message}", ex));
        }

        private void m_commandChannel_ServerStarted(object sender, EventArgs e)
        {
            OnStatusMessage(MessageLevel.Info, "Data publisher command channel started.");
        }

        private void m_commandChannel_ServerStopped(object sender, EventArgs e)
        {
            if (Enabled)
            {
                OnStatusMessage(MessageLevel.Info, "Data publisher command channel was unexpectedly terminated, restarting...");

                // We must wait for command channel to completely shutdown before trying to restart...
                if ((object)m_commandChannelRestartTimer != null)
                    m_commandChannelRestartTimer.Start();
            }
            else
            {
                OnStatusMessage(MessageLevel.Info, "Data publisher command channel stopped.");
            }
        }

        private void m_commandChannel_SendClientDataException(object sender, EventArgs<Guid, Exception> e)
        {
            Exception ex = e.Argument2;

            if (!HandleSocketException(e.Argument1, ex) && !(ex is NullReferenceException) && !(ex is ObjectDisposedException))
                OnProcessException(MessageLevel.Info, new InvalidOperationException($"Data publisher encountered an exception while sending command channel data to client connection: {ex.Message}", ex));
        }

        private void m_commandChannel_ReceiveClientDataException(object sender, EventArgs<Guid, Exception> e)
        {
            Exception ex = e.Argument2;

            if (!HandleSocketException(e.Argument1, ex) && !(ex is NullReferenceException) && !(ex is ObjectDisposedException))
                OnProcessException(MessageLevel.Info, new InvalidOperationException($"Data publisher encountered an exception while receiving command channel data from client connection: {ex.Message}", ex));
        }

        #endregion

        #endregion

        #region [ Static ]

        // Static Fields

        // Constant zero length integer byte array
        private static readonly byte[] ZeroLengthBytes = { 0, 0, 0, 0 };

        #endregion
    }
}
