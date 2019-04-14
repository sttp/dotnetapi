//******************************************************************************************************
//  DataSubscriber.cs - Gbtc
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

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Timers;
using System.Xml;
using sttp.communication;
using sttp.security;
using sttp.threading;
using sttp.transport.tssc;
using sttp.units;
using TcpClient = sttp.communication.TcpClient;
using UdpClient = sttp.communication.UdpClient;
using Timer = System.Timers.Timer;

#pragma warning disable 672

namespace sttp.transport
{
    /// <summary>
    /// Represents a data subscribing client that will connect to a data publisher for a data subscription.
    /// </summary>
    public class DataSubscriber
    {
        #region [ Members ]

        /// <summary>
        /// EventArgs implementation for handling user commands.
        /// </summary>
        public class UserCommandArgs : EventArgs
        {
            /// <summary>
            /// Creates a new instance of the <see cref="UserCommandArgs"/> class.
            /// </summary>
            /// <param name="command">The code for the user command.</param>
            /// <param name="response">The code for the server's response.</param>
            /// <param name="buffer">Buffer containing the message from the server.</param>
            /// <param name="startIndex">Index into the buffer used to skip the header.</param>
            /// <param name="length">The length of the message in the buffer, including the header.</param>
            public UserCommandArgs(ServerCommand command, ServerResponse response, byte[] buffer, int startIndex, int length)
            {
                Command = command;
                Response = response;
                Buffer = buffer;
                StartIndex = startIndex;
                Length = length;
            }

            /// <summary>
            /// Gets the code for the user command.
            /// </summary>
            public ServerCommand Command { get; }

            /// <summary>
            /// Gets the code for the server's response.
            /// </summary>
            public ServerResponse Response { get; }

            /// <summary>
            /// Gets the buffer containing the message from the server.
            /// </summary>
            public byte[] Buffer { get; }

            /// <summary>
            /// Gets the index into the buffer used to skip the header.
            /// </summary>
            public int StartIndex { get; }

            /// <summary>
            /// Gets the length of the message in the buffer, including the header.
            /// </summary>
            public int Length { get; }
        }

        // Constants

        /// <summary>
        /// Defines default value for <see cref="DataSubscriber.OperationalModes"/>.
        /// </summary>
        public const OperationalModes DefaultOperationalModes = (OperationalModes)((uint)OperationalModes.VersionMask & 1u) | OperationalModes.CompressMetadata | OperationalModes.CompressSignalIndexCache | OperationalModes.ReceiveInternalMetadata;

        /// <summary>
        /// Defines the default value for the <see cref="MetadataSynchronizationTimeout"/> property.
        /// </summary>
        public const int DefaultMetadataSynchronizationTimeout = 0;

        /// <summary>
        /// Defines the default value for the <see cref="UseTransactionForMetadata"/> property.
        /// </summary>
        public const bool DefaultUseTransactionForMetadata = true;

        /// <summary>
        /// Default value for <see cref="gggingPath"/>.
        /// </summary>
        public const string DefaultLoggingPath = "ConfigurationCache";

        /// <summary>
        /// Specifies the default value for the <see cref="AllowedParsingExceptions"/> property.
        /// </summary>
        public const int DefaultAllowedParsingExceptions = 10;

        /// <summary>
        /// Specifies the default value for the <see cref="ParsingExceptionWindow"/> property.
        /// </summary>
        public const long DefaultParsingExceptionWindow = 50000000L; // 5 seconds

        private const int EvenKey = 0;      // Even key/IV index
        private const int OddKey = 1;       // Odd key/IV index
        private const int KeyIndex = 0;     // Index of cipher key component in keyIV array
        private const int IVIndex = 1;      // Index of initialization vector component in keyIV array

        private const long MissingCacheWarningInterval = 20000000;

        // Events

        /// <summary>
        /// Occurs when client connection to the data publication server is established.
        /// </summary>
        public event EventHandler ConnectionEstablished;

        /// <summary>
        /// Occurs when client connection to the data publication server is terminated.
        /// </summary>
        public event EventHandler ConnectionTerminated;

        /// <summary>
        /// Occurs when client connection to the data publication server has successfully authenticated.
        /// </summary>
        public event EventHandler ConnectionAuthenticated;

        /// <summary>
        /// Occurs when client receives response from the server.
        /// </summary>
        public event EventHandler<EventArgs<ServerResponse, ServerCommand>> ReceivedServerResponse;

        /// <summary>
        /// Occurs when client receives message from the server in response to a user command.
        /// </summary>
        public event EventHandler<UserCommandArgs> ReceivedUserCommandResponse;

        /// <summary>
        /// Occurs when client receives requested meta-data transmitted by data publication server.
        /// </summary>
        public event EventHandler<EventArgs<DataSet>> MetaDataReceived;

        /// <summary>
        /// Occurs when first measurement is transmitted by data publication server.
        /// </summary>
        public event EventHandler<EventArgs<Ticks>> DataStartTime;

        /// <summary>
        /// Indicates that processing for an input adapter (via temporal session) has completed.
        /// </summary>
        /// <remarks>
        /// This event is expected to only be raised when an input adapter has been designed to process
        /// a finite amount of data, e.g., reading a historical range of data during temporal processing.
        /// </remarks>
        public event EventHandler<EventArgs<string>> ProcessingComplete;

        /// <summary>
        /// Occurs when a notification has been received from the <see cref="DataPublisher"/>.
        /// </summary>
        public event EventHandler<EventArgs<string>> NotificationReceived;

        /// <summary>
        /// Occurs when the server has sent a notification that its configuration has changed, this
        /// can allow subscriber to request updated meta-data if desired.
        /// </summary>
        public event EventHandler ServerConfigurationChanged;

        /// <summary>
        /// Occurs when number of parsing exceptions exceed <see cref="AllowedParsingExceptions"/> during <see cref="ParsingExceptionWindow"/>.
        /// </summary>
        public event EventHandler ExceededParsingExceptionThreshold;

        // Fields
        private IClient m_commandChannel;
        private UdpClient m_dataChannel;
        private byte[] m_commandChannelBuffer;
        private int m_commandChannelBufferLength;
        private byte[] m_dataChannelBuffer;
        private int m_dataChannelBufferLength;
        private bool m_tsscResetRequested;
        private TsscDecoder m_tsscDecoder;
        private ushort m_tsscSequenceNumber;
        private Timer m_dataStreamMonitor;
        private long m_commandChannelConnectionAttempts;
        private long m_dataChannelConnectionAttempts;
        private volatile SignalIndexCache m_remoteSignalIndexCache;
        private volatile SignalIndexCache m_signalIndexCache;
        private volatile long[] m_baseTimeOffsets;
        private volatile int m_timeIndex;
        private volatile byte[][][] m_keyIVs;
        private volatile bool m_authenticated;
        private volatile bool m_subscribed;
        private volatile int m_lastBytesReceived;
        private long m_monitoredBytesReceived;
        private long m_totalBytesReceived;
        private long m_lastMissingCacheWarning;
        private SecurityMode m_securityMode;
        private bool m_useMillisecondResolution;
        private bool m_requestNaNValueFilter;
        private bool m_autoConnect;
        private string m_metadataFilters;
        private string m_localCertificate;
        private string m_remoteCertificate;
        private SslPolicyErrors m_validPolicyErrors;
        private X509ChainStatusFlags m_validChainFlags;
        private bool m_checkCertificateRevocation;
        private bool m_internal;
        private bool m_includeTime;
        private bool m_filterOutputMeasurements;
        private bool m_autoSynchronizeMetadata;
        private bool m_useTransactionForMetadata;
        private bool m_useSourcePrefixNames;
        private bool m_useLocalClockAsRealTime;
        private bool m_metadataRefreshPending;
        private int m_metadataSynchronizationTimeout;
        private readonly LongSynchronizedOperation m_synchronizeMetadataOperation;
        private volatile DataSet m_receivedMetadata;
        private DataSet m_synchronizedMetadata;
        private DateTime m_lastMetaDataRefreshTime;
        private OperationalModes m_operationalModes;
        private Encoding m_encoding;
        private string m_loggingPath;
        private int m_parsingExceptionCount;
        private long m_lastParsingExceptionTime;
        private int m_allowedParsingExceptions;
        private Ticks m_parsingExceptionWindow;
        private bool m_supportsRealTimeProcessing;
        private bool m_supportsTemporalProcessing;

        private readonly List<BufferBlockMeasurement> m_bufferBlockCache;
        private uint m_expectedBufferBlockSequenceNumber;

        private Ticks m_realTime;
        private Ticks m_lastStatisticsHelperUpdate;
        private Timer m_subscribedDevicesTimer;

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

        private long m_syncProgressTotalActions;
        private long m_syncProgressActionsCount;
        private long m_syncProgressUpdateInterval;
        private long m_syncProgressLastMessage;

        private bool m_disposed;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Creates a new <see cref="DataSubscriber"/>.
        /// </summary>
        public DataSubscriber()
        {
            m_synchronizeMetadataOperation = new LongSynchronizedOperation(SynchronizeMetadata)
            {
                IsBackground = true
            };

            m_encoding = Encoding.Unicode;
            m_operationalModes = DefaultOperationalModes;
            m_metadataSynchronizationTimeout = DefaultMetadataSynchronizationTimeout;
            m_allowedParsingExceptions = DefaultAllowedParsingExceptions;
            m_parsingExceptionWindow = DefaultParsingExceptionWindow;

            string loggingPath = FilePath.GetDirectoryName(FilePath.GetAbsolutePath(DefaultLoggingPath));

            if (Directory.Exists(loggingPath))
                m_loggingPath = loggingPath;

            DataLossInterval = 10.0D;

            m_bufferBlockCache = new List<BufferBlockMeasurement>();
            m_useLocalClockAsRealTime = true;
            m_useSourcePrefixNames = true;
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets or sets the security mode used for communications over the command channel.
        /// </summary>
        public SecurityMode SecurityMode
        {
            get => m_securityMode;
            set => m_securityMode = value;
        }

        /// <summary>
        /// Gets or sets logging path to be used to be runtime and outage logs of the subscriber which are required for
        /// automated data recovery.
        /// </summary>
        /// <remarks>
        /// Leave value blank for default path, i.e., installation folder. Can be a fully qualified path or a path that
        /// is relative to the installation folder, e.g., a value of "ConfigurationCache" might resolve to
        /// "C:\Program Files\MyTimeSeriespPp\ConfigurationCache\".
        /// </remarks>
        public string LoggingPath
        {
            get => m_loggingPath;
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    string loggingPath = FilePath.GetDirectoryName(FilePath.GetAbsolutePath(value));

                    if (Directory.Exists(loggingPath))
                        value = loggingPath;
                }

                m_loggingPath = value;
            }
        }

        /// <summary>
        /// Gets or sets flag that determines if <see cref="DataSubscriber"/> should attempt to auto-connection to <see cref="DataPublisher"/> using defined connection settings.
        /// </summary>
        public bool AutoConnect
        {
            get => m_autoConnect;
            set => m_autoConnect = value;
        }

        /// <summary>
        /// Gets or sets flag that determines if <see cref="DataSubscriber"/> should
        /// automatically request meta-data synchronization and synchronize publisher
        /// meta-data with its own database configuration.
        /// </summary>
        public bool AutoSynchronizeMetadata
        {
            get => m_autoSynchronizeMetadata;
            set => m_autoSynchronizeMetadata = value;
        }

        /// <summary>
        /// Gets flag that indicates whether the connection will be persisted
        /// even while the adapter is offline in order to synchronize metadata.
        /// </summary>
        public bool PersistConnectionForMetadata =>
            !AutoStart && AutoSynchronizeMetadata && !this.TemporalConstraintIsDefined();

        /// <summary>
        /// Gets or sets flag that determines if child devices associated with a subscription
        /// should be prefixed with the subscription name and an exclamation point to ensure
        /// device name uniqueness - recommended value is <c>true</c>.
        /// </summary>
        public bool UseSourcePrefixNames
        {
            get => m_useSourcePrefixNames;
            set => m_useSourcePrefixNames = value;
        }

        /// <summary>
        /// Gets or sets requested meta-data filter expressions to be applied by <see cref="DataPublisher"/> before meta-data is sent.
        /// </summary>
        /// <remarks>
        /// Multiple meta-data filters, such filters for different data tables, should be separated by a semicolon. Specifying fields in the filter
        /// expression that do not exist in the data publisher's current meta-data set could cause filter expressions to not be applied and possibly
        /// result in no meta-data being received for the specified data table.
        /// </remarks>
        /// <example>
        /// FILTER MeasurementDetail WHERE SignalType &lt;&gt; 'STAT'; FILTER PhasorDetail WHERE Phase = '+'
        /// </example>
        public string MetadataFilters
        {
            get => m_metadataFilters;
            set => m_metadataFilters = value;
        }

        /// <summary>
        /// Gets or sets flag that informs publisher if base time-offsets can use millisecond resolution to conserve bandwidth.
        /// </summary>
        [Obsolete("SubscriptionInfo object defines this parameter.", false)]
        public bool UseMillisecondResolution
        {
            get => m_useMillisecondResolution;
            set => m_useMillisecondResolution = value;
        }

        /// <summary>
        /// Gets flag that determines whether the command channel is connected.
        /// </summary>
        public bool CommandChannelConnected =>
            (object)m_commandChannel != null &&
            m_commandChannel.Enabled;

        /// <summary>
        /// Gets flag that determines if this <see cref="DataSubscriber"/> has successfully authenticated with the <see cref="DataPublisher"/>.
        /// </summary>
        public bool Authenticated => m_authenticated;

        /// <summary>
        /// Gets total data packet bytes received during this session.
        /// </summary>
        public long TotalBytesReceived => m_totalBytesReceived;

        /// <summary>
        /// Gets or sets data loss monitoring interval, in seconds. Set to zero to disable monitoring.
        /// </summary>
        public double DataLossInterval
        {
            get
            {
                if ((object)m_dataStreamMonitor != null)
                    return m_dataStreamMonitor.Interval / 1000.0D;

                return 0.0D;
            }
            set
            {
                if (value > 0.0D)
                {
                    if ((object)m_dataStreamMonitor == null)
                    {
                        // Create data stream monitoring timer
                        m_dataStreamMonitor = new Timer();
                        m_dataStreamMonitor.Elapsed += m_dataStreamMonitor_Elapsed;
                        m_dataStreamMonitor.AutoReset = true;
                        m_dataStreamMonitor.Enabled = false;
                    }

                    // Set user specified interval
                    m_dataStreamMonitor.Interval = (int)(value * 1000.0D);
                }
                else
                {
                    // Disable data monitor
                    if ((object)m_dataStreamMonitor != null)
                    {
                        m_dataStreamMonitor.Elapsed -= m_dataStreamMonitor_Elapsed;
                        m_dataStreamMonitor.Dispose();
                    }

                    m_dataStreamMonitor = null;
                }
            }
        }

        /// <summary>
        /// Gets or sets a set of flags that define ways in
        /// which the subscriber and publisher communicate.
        /// </summary>
        public OperationalModes OperationalModes
        {
            get => m_operationalModes;
            set
            {
                OperationalEncoding operationalEncoding;

                m_operationalModes = value;
                operationalEncoding = (OperationalEncoding)(value & OperationalModes.EncodingMask);
                m_encoding = GetCharacterEncoding(operationalEncoding);
            }
        }

        /// <summary>
        /// Gets or sets the operational mode flag to compress meta-data.
        /// </summary>
        public bool CompressMetadata
        {
            get => m_operationalModes.HasFlag(OperationalModes.CompressMetadata);
            set
            {
                if (value)
                    m_operationalModes |= OperationalModes.CompressMetadata;
                else
                    m_operationalModes &= ~OperationalModes.CompressMetadata;
            }
        }

        /// <summary>
        /// Gets or sets the operational mode flag to compress the signal index cache.
        /// </summary>
        public bool CompressSignalIndexCache
        {
            get => m_operationalModes.HasFlag(OperationalModes.CompressSignalIndexCache);
            set
            {
                if (value)
                    m_operationalModes |= OperationalModes.CompressSignalIndexCache;
                else
                    m_operationalModes &= ~OperationalModes.CompressSignalIndexCache;
            }
        }

        /// <summary>
        /// Gets or sets the operational mode flag to compress data payloads.
        /// </summary>
        public bool CompressPayload
        {
            get => m_operationalModes.HasFlag(OperationalModes.CompressPayloadData);
            set
            {
                if (value)
                    m_operationalModes |= OperationalModes.CompressPayloadData;
                else
                    m_operationalModes &= ~OperationalModes.CompressPayloadData;
            }
        }

        /// <summary>
        /// Gets or sets the operational mode flag to receive internal meta-data.
        /// </summary>
        public bool ReceiveInternalMetadata
        {
            get => m_operationalModes.HasFlag(OperationalModes.ReceiveInternalMetadata);
            set
            {
                if (value)
                    m_operationalModes |= OperationalModes.ReceiveInternalMetadata;
                else
                    m_operationalModes &= ~OperationalModes.ReceiveInternalMetadata;
            }
        }

        /// <summary>
        /// Gets or sets the operational mode flag to receive external meta-data.
        /// </summary>
        public bool ReceiveExternalMetadata
        {
            get => m_operationalModes.HasFlag(OperationalModes.ReceiveExternalMetadata);
            set
            {
                if (value)
                    m_operationalModes |= OperationalModes.ReceiveExternalMetadata;
                else
                    m_operationalModes &= ~OperationalModes.ReceiveExternalMetadata;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="OperationalEncoding"/> used by the subscriber and publisher.
        /// </summary>
        public OperationalEncoding OperationalEncoding
        {
            get => (OperationalEncoding)(m_operationalModes & OperationalModes.EncodingMask);
            set
            {
                m_operationalModes &= ~OperationalModes.EncodingMask;
                m_operationalModes |= (OperationalModes)value;
                m_encoding = GetCharacterEncoding(value);
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="CompressionModes"/> used by the subscriber and publisher.
        /// </summary>
        public CompressionModes CompressionModes
        {
            get => (CompressionModes)(m_operationalModes & OperationalModes.CompressionModeMask);
            set
            {
                m_operationalModes &= ~OperationalModes.CompressionModeMask;
                m_operationalModes |= (OperationalModes)value;

                if (value.HasFlag(CompressionModes.TSSC))
                    CompressPayload = true;
            }
        }

        /// <summary>
        /// Gets the version number of the protocol in use by this subscriber.
        /// </summary>
        public int Version => (int)(m_operationalModes & OperationalModes.VersionMask);

        /// <summary>
        /// Gets the character encoding defined by the
        /// <see cref="OperationalEncoding"/> of the communications stream.
        /// </summary>
        public Encoding Encoding => m_encoding;

        /// <summary>
        /// Gets flag indicating if this adapter supports real-time processing.
        /// </summary>
        /// <remarks>
        /// Setting this value to false indicates that the adapter should not be enabled unless it exists within a temporal session.
        /// As an example, this flag can be used in a gateway system to set up two separate subscribers: one to the PDC for real-time
        /// data streams and one to the historian for historical data streams. In this scenario, the assumption is that the PDC is
        /// the data source for the historian, implying that only local data is destined for archival.
        /// </remarks>
        public bool SupportsRealTimeProcessing => m_supportsRealTimeProcessing;

        /// <summary>
        /// Gets the flag indicating if this adapter supports temporal processing.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Although the data subscriber provisions support for temporal processing by receiving historical data from a remote source,
        /// the adapter opens sockets and does not need to be engaged within an actual temporal <see cref="IaonSession"/>, therefore
        /// this method normally returns <c>false</c> to make sure the adapter doesn't get instantiated within a temporal session.
        /// </para>
        /// <para>
        /// Setting this to <c>true</c> means that a subscriber will be initialized within a temporal session to provide historical
        /// data from a remote source - this should only be enabled in cases where (1) there is no locally defined, e.g., in-process,
        /// historian that can already provide historical data for temporal sessions, and (2) a remote subscriber should be allowed
        /// to proxy temporal requests, e.g., those requested for data gap recovery, to an up-stream subscription. This is useful in
        /// cases where a primary data subscriber that has data gap recovery enabled can also allow a remote subscription to proxy in
        /// data gap recovery requests. It is recommended that remote data gap recovery request parameters be (1) either slightly
        /// looser than those of local system to reduce the possibility of duplicated recovery sessions for the same data loss, or
        /// (2) only enabled in the end-most system that most needs the recovered data, like a historian.
        /// </para>
        /// </remarks>
        public override bool SupportsTemporalProcessing => m_supportsTemporalProcessing;

        /// <summary>
        /// Gets or sets the desired processing interval, in milliseconds, for the adapter.
        /// </summary>
        /// <remarks>
        /// With the exception of the values of -1 and 0, this value specifies the desired processing interval for data, i.e.,
        /// basically a delay, or timer interval, over which to process data. A value of -1 means to use the default processing
        /// interval while a value of 0 means to process data as fast as possible.
        /// </remarks>
        public int ProcessingInterval
        {
            get => base.ProcessingInterval;
            set
            {
                base.ProcessingInterval = value;

                // Request server update the processing interval
                SendServerCommand(ServerCommand.UpdateProcessingInterval, BigEndian.GetBytes(value));
            }
        }

        /// <summary>
        /// Gets or sets the timeout used when executing database queries during meta-data synchronization.
        /// </summary>
        public int MetadataSynchronizationTimeout
        {
            get => m_metadataSynchronizationTimeout;
            set => m_metadataSynchronizationTimeout = value;
        }

        /// <summary>
        /// Gets or sets flag that determines if meta-data synchronization should be performed within a transaction.
        /// </summary>
        public bool UseTransactionForMetadata
        {
            get => m_useTransactionForMetadata;
            set => m_useTransactionForMetadata = value;
        }

        /// <summary>
        /// Gets or sets flag that determines whether to use the local clock when calculating statistics.
        /// </summary>
        public bool UseLocalClockAsRealTime
        {
            get => m_useLocalClockAsRealTime;
            set => m_useLocalClockAsRealTime = value;
        }

        /// <summary>
        /// Gets or sets number of parsing exceptions allowed during <see cref="ParsingExceptionWindow"/> before connection is reset.
        /// </summary>
        public int AllowedParsingExceptions
        {
            get => m_allowedParsingExceptions;
            set => m_allowedParsingExceptions = value;
        }

        /// <summary>
        /// Gets or sets time duration, in <see cref="Ticks"/>, to monitor parsing exceptions.
        /// </summary>
        public Ticks ParsingExceptionWindow
        {
            get => m_parsingExceptionWindow;
            set => m_parsingExceptionWindow = value;
        }

        /// <summary>
        /// Gets or sets <see cref="DataSet"/> based data source available to this <see cref="DataSubscriber"/>.
        /// </summary>
        public override DataSet DataSource
        {
            get => base.DataSource;
            set
            {
                base.DataSource = value;
                m_registerStatisticsOperation.RunOnce();

                bool outputMeasurementsUpdated = AutoConnect && UpdateOutputMeasurements();

                // For automatic connections, when meta-data refresh is complete, update output measurements to see if any
                // points for subscription have changed after re-application of filter expressions and if so, resubscribe
                if (outputMeasurementsUpdated && Enabled && CommandChannelConnected)
                {
                    OnStatusMessage(MessageLevel.Info, "Meta-data received from publisher modified measurement availability, adjusting active subscription...");

                    // Updating subscription will restart data stream monitor upon successful resubscribe
                    if (AutoStart)
                        SubscribeToOutputMeasurements(true);
                }
            }
        }

        /// <summary>
        /// Gets or sets output measurement keys that are requested by other adapters based on what adapter says it can provide.
        /// </summary>
        public override MeasurementKey[] RequestedOutputMeasurementKeys
        {
            get => base.RequestedOutputMeasurementKeys;
            set
            {
                MeasurementKey[] oldKeys = base.RequestedOutputMeasurementKeys ?? new MeasurementKey[0];
                MeasurementKey[] newKeys = value ?? new MeasurementKey[0];
                HashSet<MeasurementKey> oldKeySet = new HashSet<MeasurementKey>(oldKeys);

                base.RequestedOutputMeasurementKeys = value;

                if (!AutoStart && Enabled && CommandChannelConnected && !oldKeySet.SetEquals(newKeys))
                {
                    OnStatusMessage(MessageLevel.Info, "Requested measurements have changed, adjusting active subscription...");
                    SubscribeToOutputMeasurements(true);
                }
            }
        }

        /// <summary>
        /// Gets connection info for adapter, if any.
        /// </summary>
        public string ConnectionInfo
        {
            get
            {
                string commandChannelServerUri = m_commandChannel?.ServerUri;
                string dataChannelServerUri = m_dataChannel?.ServerUri;

                if (string.IsNullOrWhiteSpace(commandChannelServerUri) && string.IsNullOrWhiteSpace(dataChannelServerUri))
                    return null;

                if (string.IsNullOrWhiteSpace(dataChannelServerUri))
                    return commandChannelServerUri;

                if (string.IsNullOrWhiteSpace(commandChannelServerUri))
                    return dataChannelServerUri;

                return $"{commandChannelServerUri} / {dataChannelServerUri}";
            }
        }

        /// <summary>
        /// Gets the status of this <see cref="DataSubscriber"/>.
        /// </summary>
        /// <remarks>
        /// Derived classes should provide current status information about the adapter for display purposes.
        /// </remarks>
        public override string Status
        {
            get
            {
                StringBuilder status = new StringBuilder();

                status.AppendFormat("             Authenticated: {0}", m_authenticated);
                status.AppendLine();
                status.AppendFormat("                Subscribed: {0}", m_subscribed);
                status.AppendLine();
                status.AppendFormat("             Security mode: {0}", SecurityMode);
                status.AppendLine();
                status.AppendFormat("         Compression modes: {0}", CompressionModes);
                status.AppendLine();
                if ((object)m_dataChannel != null)
                {
                    status.AppendFormat("  UDP Data packet security: {0}", (object)m_keyIVs == null ? "Unencrypted" : "Encrypted");
                    status.AppendLine();
                }
                status.AppendFormat("      Data monitor enabled: {0}", (object)m_dataStreamMonitor != null && m_dataStreamMonitor.Enabled);
                status.AppendLine();
                status.AppendFormat("              Logging path: {0}", FilePath.TrimFileName(m_loggingPath.ToNonNullNorWhiteSpace(FilePath.GetAbsolutePath("")), 51));
                status.AppendLine();

                if (DataLossInterval > 0.0D)
                    status.AppendFormat("No data reconnect interval: {0:0.000} seconds", DataLossInterval);
                else
                    status.Append("No data reconnect interval: disabled");

                status.AppendLine();

                if ((object)m_dataChannel != null)
                {
                    status.AppendLine();
                    status.AppendLine("Data Channel Status".CenterText(50));
                    status.AppendLine("-------------------".CenterText(50));
                    status.Append(m_dataChannel.Status);
                }

                if ((object)m_commandChannel != null)
                {
                    status.AppendLine();
                    status.AppendLine("Command Channel Status".CenterText(50));
                    status.AppendLine("----------------------".CenterText(50));
                    status.Append(m_commandChannel.Status);
                }

                status.Append(base.Status);

                return status.ToString();
            }
        }

        /// <summary>
        /// Gets a flag that determines if this <see cref="DataSubscriber"/> uses an asynchronous connection.
        /// </summary>
        protected override bool UseAsyncConnect => true;

        /// <summary>
        /// Gets or sets reference to <see cref="UdpClient"/> data channel, attaching and/or detaching to events as needed.
        /// </summary>
        protected UdpClient DataChannel
        {
            get => m_dataChannel;
            set
            {
                if ((object)m_dataChannel != null)
                {
                    // Detach from events on existing data channel reference
                    m_dataChannel.ConnectionException -= m_dataChannel_ConnectionException;
                    m_dataChannel.ConnectionAttempt -= m_dataChannel_ConnectionAttempt;
                    m_dataChannel.ReceiveData -= m_dataChannel_ReceiveData;
                    m_dataChannel.ReceiveDataException -= m_dataChannel_ReceiveDataException;

                    if ((object)m_dataChannel != value)
                        m_dataChannel.Dispose();
                }

                // Assign new data channel reference
                m_dataChannel = value;

                if ((object)m_dataChannel != null)
                {
                    // Attach to desired events on new data channel reference
                    m_dataChannel.ConnectionException += m_dataChannel_ConnectionException;
                    m_dataChannel.ConnectionAttempt += m_dataChannel_ConnectionAttempt;
                    m_dataChannel.ReceiveData += m_dataChannel_ReceiveData;
                    m_dataChannel.ReceiveDataException += m_dataChannel_ReceiveDataException;
                }
            }
        }

        /// <summary>
        /// Gets or sets reference to <see cref="Communication.TcpClient"/> command channel, attaching and/or detaching to events as needed.
        /// </summary>
        protected IClient CommandChannel
        {
            get => m_commandChannel;
            set
            {
                if ((object)m_commandChannel != null)
                {
                    // Detach from events on existing command channel reference
                    m_commandChannel.ConnectionAttempt -= m_commandChannel_ConnectionAttempt;
                    m_commandChannel.ConnectionEstablished -= m_commandChannel_ConnectionEstablished;
                    m_commandChannel.ConnectionException -= m_commandChannel_ConnectionException;
                    m_commandChannel.ConnectionTerminated -= m_commandChannel_ConnectionTerminated;
                    m_commandChannel.ReceiveData -= m_commandChannel_ReceiveData;
                    m_commandChannel.ReceiveDataException -= m_commandChannel_ReceiveDataException;
                    m_commandChannel.SendDataException -= m_commandChannel_SendDataException;

                    if (m_commandChannel != value)
                        m_commandChannel.Dispose();
                }

                // Assign new command channel reference
                m_commandChannel = value;

                if ((object)m_commandChannel != null)
                {
                    // Attach to desired events on new command channel reference
                    m_commandChannel.ConnectionAttempt += m_commandChannel_ConnectionAttempt;
                    m_commandChannel.ConnectionEstablished += m_commandChannel_ConnectionEstablished;
                    m_commandChannel.ConnectionException += m_commandChannel_ConnectionException;
                    m_commandChannel.ConnectionTerminated += m_commandChannel_ConnectionTerminated;
                    m_commandChannel.ReceiveData += m_commandChannel_ReceiveData;
                    m_commandChannel.ReceiveDataException += m_commandChannel_ReceiveDataException;
                    m_commandChannel.SendDataException += m_commandChannel_SendDataException;
                }
            }
        }

        /// <summary>
        /// Gets the total number of measurements processed through this data publisher over the lifetime of the subscriber.
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
        /// Gets the minimum latency calculated over the full lifetime of the subscriber.
        /// </summary>
        public int LifetimeMinimumLatency => (int)Ticks.ToMilliseconds(m_lifetimeMinimumLatency);

        /// <summary>
        /// Gets the maximum latency calculated over the full lifetime of the subscriber.
        /// </summary>
        public int LifetimeMaximumLatency => (int)Ticks.ToMilliseconds(m_lifetimeMaximumLatency);

        /// <summary>
        /// Gets the average latency calculated over the full lifetime of the subscriber.
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

        /// <summary>
        /// Gets real-time as determined by either the local clock or the latest measurement received.
        /// </summary>
        protected Ticks RealTime => m_useLocalClockAsRealTime ? (Ticks)DateTime.UtcNow.Ticks : m_realTime;

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="DataSubscriber"/> object and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (!m_disposed)
            {
                try
                {
                    if (disposing)
                    {
                        DataLossInterval = 0.0D;
                        CommandChannel = null;
                        DataChannel = null;

                        if ((object)m_subscribedDevicesTimer != null)
                        {
                            m_subscribedDevicesTimer.Elapsed -= SubscribedDevicesTimer_Elapsed;
                            m_subscribedDevicesTimer.Dispose();
                            m_subscribedDevicesTimer = null;
                        }
                    }
                }
                finally
                {
                    m_disposed = true;          // Prevent duplicate dispose.
                    base.Dispose(disposing);    // Call base class Dispose().
                }
            }
        }

        /// <summary>
        /// Initializes <see cref="DataSubscriber"/>.
        /// </summary>
        public override void Initialize()
        {
            //base.Initialize();

            Dictionary<string, string> settings = Settings;

            // See if user has opted for different operational modes
            if (settings.TryGetValue("operationalModes", out string setting) && Enum.TryParse(setting, true, out OperationalModes operationalModes))
                OperationalModes = operationalModes;

            // Set the security mode if explicitly defined
            if (!settings.TryGetValue("securityMode", out setting) || !Enum.TryParse(setting, true, out m_securityMode))
                m_securityMode = SecurityMode.None;

            // Apply gateway compression mode to operational mode flags
            if (settings.TryGetValue("compressionModes", out setting) && Enum.TryParse(setting, true, out CompressionModes compressionModes))
                CompressionModes = compressionModes;

            // Check if output measurements should be filtered to only those belonging to the subscriber
            m_filterOutputMeasurements = !settings.TryGetValue("filterOutputMeasurements", out setting) || setting.ParseBoolean();

            // Check if the subscriber supports real-time and historical processing
            m_supportsRealTimeProcessing = !settings.TryGetValue("supportsRealTimeProcessing", out setting) || setting.ParseBoolean();
            m_supportsTemporalProcessing = settings.TryGetValue("supportsTemporalProcessing", out setting) && setting.ParseBoolean();

            // Settings specific to Transport Layer Security
            if (m_securityMode == SecurityMode.TLS)
            {
                if (!settings.TryGetValue("localCertificate", out m_localCertificate) || !File.Exists(m_localCertificate))
                    m_localCertificate = GetLocalCertificate();

                if (!settings.TryGetValue("remoteCertificate", out m_remoteCertificate) || !RemoteCertificateExists())
                    throw new ArgumentException("The \"remoteCertificate\" setting must be defined and certificate file must exist when using TLS security mode.");

                if (!settings.TryGetValue("validPolicyErrors", out setting) || !Enum.TryParse(setting, out m_validPolicyErrors))
                    m_validPolicyErrors = SslPolicyErrors.None;

                if (!settings.TryGetValue("validChainFlags", out setting) || !Enum.TryParse(setting, out m_validChainFlags))
                    m_validChainFlags = X509ChainStatusFlags.NoError;

                if (settings.TryGetValue("checkCertificateRevocation", out setting) && !string.IsNullOrWhiteSpace(setting))
                    m_checkCertificateRevocation = setting.ParseBoolean();
                else
                    m_checkCertificateRevocation = true;
            }

            // Check if measurements for this connection should be marked as "internal" - i.e., owned and allowed for proxy
            if (settings.TryGetValue("internal", out setting))
                m_internal = setting.ParseBoolean();

            // Check if user has explicitly defined the ReceiveInternalMetadata flag
            if (settings.TryGetValue("receiveInternalMetadata", out setting))
                ReceiveInternalMetadata = setting.ParseBoolean();

            // Check if user has explicitly defined the ReceiveExternalMetadata flag
            if (settings.TryGetValue("receiveExternalMetadata", out setting))
                ReceiveExternalMetadata = setting.ParseBoolean();

            // Check if user has defined a meta-data synchronization timeout
            if (settings.TryGetValue("metadataSynchronizationTimeout", out setting) && int.TryParse(setting, out int metadataSynchronizationTimeout))
                m_metadataSynchronizationTimeout = metadataSynchronizationTimeout;

            // Check if user has defined a flag for using a transaction during meta-data synchronization
            if (settings.TryGetValue("useTransactionForMetadata", out setting))
                m_useTransactionForMetadata = setting.ParseBoolean();

            // Check if user wants to request that publisher use millisecond resolution to conserve bandwidth
            if (settings.TryGetValue("useMillisecondResolution", out setting))
                m_useMillisecondResolution = setting.ParseBoolean();

            // Check if user wants to request that publisher remove NaN from the data stream to conserve bandwidth
            if (settings.TryGetValue("requestNaNValueFilter", out setting))
                m_requestNaNValueFilter = setting.ParseBoolean();

            // Check if user has defined any meta-data filter expressions
            if (settings.TryGetValue("metadataFilters", out setting))
                m_metadataFilters = setting;

            // Define auto connect setting
            if (settings.TryGetValue("autoConnect", out setting))
            {
                m_autoConnect = setting.ParseBoolean();

                if (m_autoConnect)
                    m_autoSynchronizeMetadata = true;
            }

            // Define the maximum allowed exceptions before resetting the connection
            if (settings.TryGetValue("allowedParsingExceptions", out setting))
                m_allowedParsingExceptions = int.Parse(setting);

            // Define the window of time over which parsing exceptions are tolerated
            if (settings.TryGetValue("parsingExceptionWindow", out setting))
                m_parsingExceptionWindow = Ticks.FromSeconds(double.Parse(setting));

            // Check if synchronize meta-data is explicitly enabled or disabled
            if (settings.TryGetValue("synchronizeMetadata", out setting))
                m_autoSynchronizeMetadata = setting.ParseBoolean();

            // Determine if source name prefixes should be applied during metadata synchronization
            if (settings.TryGetValue("useSourcePrefixNames", out setting))
                m_useSourcePrefixNames = setting.ParseBoolean();

            // Define data loss interval
            if (settings.TryGetValue("dataLossInterval", out setting) && double.TryParse(setting, out double interval))
                DataLossInterval = interval;

            // Define buffer size
            if (!settings.TryGetValue("bufferSize", out setting) || !int.TryParse(setting, out int bufferSize))
                bufferSize = ClientBase.DefaultReceiveBufferSize;

            if (settings.TryGetValue("useLocalClockAsRealTime", out setting))
                m_useLocalClockAsRealTime = setting.ParseBoolean();

            if (m_autoConnect)
            {
                // Connect to local events when automatically engaging connection cycle
                ConnectionAuthenticated += DataSubscriber_ConnectionAuthenticated;
                MetaDataReceived += DataSubscriber_MetaDataReceived;

                // Update output measurements to include "subscribed" points
                UpdateOutputMeasurements(true);
            }
            else if (m_autoSynchronizeMetadata)
            {
                // Output measurements do not include "subscribed" points,
                // but should still be filtered if applicable
                TryFilterOutputMeasurements();
            }

            if (m_securityMode == SecurityMode.TLS)
            {
                // Create a new TLS client and certificate checker
                TlsClient commandChannel = new TlsClient();
                SimpleCertificateChecker certificateChecker = new SimpleCertificateChecker();

                // Set up certificate checker
                certificateChecker.TrustedCertificates.Add(new X509Certificate2(FilePath.GetAbsolutePath(m_remoteCertificate)));
                certificateChecker.ValidPolicyErrors = m_validPolicyErrors;
                certificateChecker.ValidChainFlags = m_validChainFlags;

                // Initialize default settings
                commandChannel.PayloadAware = false;
                commandChannel.PersistSettings = false;
                commandChannel.MaxConnectionAttempts = 1;
                commandChannel.CertificateFile = FilePath.GetAbsolutePath(m_localCertificate);
                commandChannel.CheckCertificateRevocation = m_checkCertificateRevocation;
                commandChannel.CertificateChecker = certificateChecker;
                commandChannel.ReceiveBufferSize = bufferSize;
                commandChannel.SendBufferSize = bufferSize;
                commandChannel.NoDelay = true;

                // Assign command channel client reference and attach to needed events
                CommandChannel = commandChannel;
            }
            else
            {
                // Create a new TCP client
                TcpClient commandChannel = new TcpClient();

                // Initialize default settings
                commandChannel.PayloadAware = false;
                commandChannel.PersistSettings = false;
                commandChannel.MaxConnectionAttempts = 1;
                commandChannel.ReceiveBufferSize = bufferSize;
                commandChannel.SendBufferSize = bufferSize;
                commandChannel.NoDelay = true;

                // Assign command channel client reference and attach to needed events
                CommandChannel = commandChannel;
            }

            // Get proper connection string - either from specified command channel or from base connection string
            if (settings.TryGetValue("commandChannel", out setting))
                m_commandChannel.ConnectionString = setting;
            else
                m_commandChannel.ConnectionString = ConnectionString;

            // Check for simplified compression setup flag
            if (settings.TryGetValue("compression", out setting) && setting.ParseBoolean())
            {
                CompressionModes |= CompressionModes.TSSC | CompressionModes.GZip;
                OperationalModes |= OperationalModes.CompressPayloadData | OperationalModes.CompressMetadata | OperationalModes.CompressSignalIndexCache;
            }

            // Get logging path, if any has been defined
            if (settings.TryGetValue("loggingPath", out setting))
            {
                setting = FilePath.GetDirectoryName(FilePath.GetAbsolutePath(setting));

                if (Directory.Exists(setting))
                    m_loggingPath = setting;
                else
                    OnStatusMessage(MessageLevel.Info, $"Logging path \"{setting}\" not found, defaulting to \"{FilePath.GetAbsolutePath("")}\"...", flags: MessageFlags.UsageIssue);
            }

            if (PersistConnectionForMetadata)
                m_commandChannel.ConnectAsync();

            Initialized = true;
        }

        // Gets the path to the local certificate from the configuration file
        private string GetLocalCertificate()
        {
            CategorizedSettingsElement localCertificateElement = ConfigurationFile.Current.Settings["systemSettings"]["LocalCertificate"];
            string localCertificate = null;

            if ((object)localCertificateElement != null)
                localCertificate = localCertificateElement.Value;

            if ((object)localCertificate == null || !File.Exists(FilePath.GetAbsolutePath(localCertificate)))
                throw new InvalidOperationException("Unable to find local certificate. Local certificate file must exist when using TLS security mode.");

            return localCertificate;
        }

        // Checks if the specified certificate exists
        private bool RemoteCertificateExists()
        {
            string fullPath = FilePath.GetAbsolutePath(m_remoteCertificate);
            CategorizedSettingsElement remoteCertificateElement;

            if (!File.Exists(fullPath))
            {
                remoteCertificateElement = ConfigurationFile.Current.Settings["systemSettings"]["RemoteCertificatesPath"];

                if ((object)remoteCertificateElement != null)
                {
                    m_remoteCertificate = Path.Combine(remoteCertificateElement.Value, m_remoteCertificate);
                    fullPath = FilePath.GetAbsolutePath(m_remoteCertificate);
                }
            }

            return File.Exists(fullPath);
        }

        // Initialize (or reinitialize) the output measurements associated with the data subscriber.
        // Returns true if output measurements were updated, otherwise false if they remain the same.
        private bool UpdateOutputMeasurements(bool initialCall = false)
        {
            IMeasurement[] originalOutputMeasurements = OutputMeasurements;

            // Reapply output measurements if reinitializing - this way filter expressions and/or sourceIDs
            // will be reapplied. This can be important after a meta-data refresh which may have added new
            // measurements that could now be applicable as desired output measurements.
            if (!initialCall)
            {
                if (Settings.TryGetValue("outputMeasurements", out string setting))
                    OutputMeasurements = ParseOutputMeasurements(DataSource, true, setting);

                OutputSourceIDs = OutputSourceIDs;
            }

            // If active measurements are defined, attempt to defined desired subscription points from there
            if (m_filterOutputMeasurements && (object)DataSource != null && DataSource.Tables.Contains("ActiveMeasurements"))
            {
                try
                {
                    // Filter to points associated with this subscriber that have been requested for subscription, are enabled and not owned locally
                    DataRow[] filteredRows = DataSource.Tables["ActiveMeasurements"].Select("Subscribed <> 0");
                    List<IMeasurement> subscribedMeasurements = new List<IMeasurement>();
                    Guid signalID;

                    foreach (DataRow row in filteredRows)
                    {
                        // Create a new measurement for the provided field level information
                        Measurement measurement = new Measurement();

                        // Parse primary measurement identifier
                        signalID = row["SignalID"].ToNonNullString(Guid.Empty.ToString()).ConvertToType<Guid>();

                        // Set measurement key if defined
                        MeasurementKey key = MeasurementKey.LookUpOrCreate(signalID, row["ID"].ToString());
                        measurement.Metadata = key.Metadata;
                        subscribedMeasurements.Add(measurement);
                    }

                    if (subscribedMeasurements.Count > 0)
                    {
                        // Combine subscribed output measurement with any existing output measurement and return unique set
                        if ((object)OutputMeasurements == null)
                            OutputMeasurements = subscribedMeasurements.ToArray();
                        else
                            OutputMeasurements = subscribedMeasurements.Concat(OutputMeasurements).Distinct().ToArray();
                    }
                }
                catch (Exception ex)
                {
                    // Errors here may not be catastrophic, this simply limits the auto-assignment of input measurement keys desired for subscription
                    OnProcessException(MessageLevel.Warning, new InvalidOperationException($"Failed to apply subscribed measurements to subscription filter: {ex.Message}", ex));
                }
            }

            // Ensure that we are not attempting to subscribe to
            // measurements that we know cannot be published
            TryFilterOutputMeasurements();

            // Determine if output measurements have changed
            return originalOutputMeasurements.CompareTo(OutputMeasurements, false) != 0;
        }

        // When synchronizing meta-data, the publisher sends meta-data for all possible signals we can subscribe to.
        // Here we check each signal defined in OutputMeasurements to determine whether that signal was defined in
        // the published meta-data rather than blindly attempting to subscribe to all signals.
        private void TryFilterOutputMeasurements()
        {
            if (!m_filterOutputMeasurements)
                return;

            IEnumerable<Guid> measurementIDs;
            ISet<Guid> measurementIDSet;
            Guid signalID = Guid.Empty;

            try
            {
                if ((object)OutputMeasurements != null && (object)DataSource != null && DataSource.Tables.Contains("ActiveMeasurements"))
                {
                    // Have to use a Convert expression for DeviceID column in Select function
                    // here since SQLite doesn't report data types for COALESCE based columns
                    measurementIDs = DataSource.Tables["ActiveMeasurements"]
                        .Select($"Convert(DeviceID, 'System.String') = '{ID}'")
                        .Where(row => Guid.TryParse(row["SignalID"].ToNonNullString(), out signalID))
                        .Select(row => signalID);

                    measurementIDSet = new HashSet<Guid>(measurementIDs);

                    OutputMeasurements = OutputMeasurements.Where(measurement => measurementIDSet.Contains(measurement.ID)).ToArray();
                }
            }
            catch (Exception ex)
            {
                OnProcessException(MessageLevel.Warning, new InvalidOperationException($"Error when filtering output measurements by device ID: {ex.Message}", ex));
            }
        }

        /// <summary>
        /// Subscribes (or re-subscribes) to a data publisher for a set of data points.
        /// </summary>
        /// <param name="info">Configuration object that defines the subscription.</param>
        /// <returns><c>true</c> if subscribe transmission was successful; otherwise <c>false</c>.</returns>
        public bool Subscribe(SubscriptionInfo info)
        {
            StringBuilder connectionString = new StringBuilder();
            AssemblyInfo assemblyInfo = AssemblyInfo.ExecutingAssembly;

            connectionString.AppendFormat("trackLatestMeasurements={0};", info.Throttled);
            connectionString.AppendFormat("publishInterval={0};", info.PublishInterval);
            connectionString.AppendFormat("includeTime={0};", info.IncludeTime);
            connectionString.AppendFormat("lagTime={0};", info.LagTime);
            connectionString.AppendFormat("leadTime={0};", info.LeadTime);
            connectionString.AppendFormat("useLocalClockAsRealTime={0};", info.UseLocalClockAsRealTime);
            connectionString.AppendFormat("processingInterval={0};", info.ProcessingInterval);
            connectionString.AppendFormat("useMillisecondResolution={0};", info.UseMillisecondResolution);
            connectionString.AppendFormat("requestNaNValueFilter={0};", info.RequestNaNValueFilter);
            connectionString.AppendFormat("assemblyInfo={{source={0};version={1}.{2}.{3};buildDate={4}}};", assemblyInfo.Name, assemblyInfo.Version.Major, assemblyInfo.Version.Minor, assemblyInfo.Version.Build, assemblyInfo.BuildDate.ToString("yyyy-MM-dd HH:mm:ss"));

            if (!string.IsNullOrWhiteSpace(info.FilterExpression))
                connectionString.AppendFormat("inputMeasurementKeys={{{0}}};", info.FilterExpression);

            if (info.UdpDataChannel)
                connectionString.AppendFormat("dataChannel={{localport={0}}};", info.DataChannelLocalPort);

            if (!string.IsNullOrWhiteSpace(info.StartTime))
                connectionString.AppendFormat("startTimeConstraint={0};", info.StartTime);

            if (!string.IsNullOrWhiteSpace(info.StopTime))
                connectionString.AppendFormat("stopTimeConstraint={0};", info.StopTime);

            if (!string.IsNullOrWhiteSpace(info.ConstraintParameters))
                connectionString.AppendFormat("timeConstraintParameters={0};", info.ConstraintParameters);

            if (!string.IsNullOrWhiteSpace(info.ExtraConnectionStringParameters))
                connectionString.AppendFormat("{0};", info.ExtraConnectionStringParameters);

            // Make sure not to monitor for data loss any faster than down-sample time on throttled connections - additionally
            // you will want to make sure data stream monitor is twice lag-time to allow time for initial points to arrive.
            if (info.Throttled && (object)m_dataStreamMonitor != null && m_dataStreamMonitor.Interval / 1000.0D < info.LagTime)
                m_dataStreamMonitor.Interval = (int)(2.0D * info.LagTime * 1000.0D);

            // Set millisecond resolution member variable for compact measurement parsing
            m_useMillisecondResolution = info.UseMillisecondResolution;

            return Subscribe(info.UseCompactMeasurementFormat, connectionString.ToString());
        }

        /// <summary>
        /// Subscribes (or re-subscribes) to a data publisher for an unsynchronized set of data points.
        /// </summary>
        /// <param name="compactFormat">Boolean value that determines if the compact measurement format should be used. Set to <c>false</c> for full fidelity measurement serialization; otherwise set to <c>true</c> for bandwidth conservation.</param>
        /// <param name="throttled">Boolean value that determines if data should be throttled at a set transmission interval or sent on change.</param>
        /// <param name="filterExpression">Filtering expression that defines the measurements that are being subscribed.</param>
        /// <param name="dataChannel">Desired UDP return data channel connection string to use for data packet transmission. Set to <c>null</c> to use TCP channel for data transmission.</param>
        /// <param name="includeTime">Boolean value that determines if time is a necessary component in streaming data.</param>
        /// <param name="lagTime">When <paramref name="throttled"/> is <c>true</c>, defines the data transmission speed in seconds (can be sub-second).</param>
        /// <param name="leadTime">When <paramref name="throttled"/> is <c>true</c>, defines the allowed time deviation tolerance to real-time in seconds (can be sub-second).</param>
        /// <param name="useLocalClockAsRealTime">When <paramref name="throttled"/> is <c>true</c>, defines boolean value that determines whether or not to use the local clock time as real-time. Set to <c>false</c> to use latest received measurement timestamp as real-time.</param>
        /// <param name="startTime">Defines a relative or exact start time for the temporal constraint to use for historical playback.</param>
        /// <param name="stopTime">Defines a relative or exact stop time for the temporal constraint to use for historical playback.</param>
        /// <param name="constraintParameters">Defines any temporal parameters related to the constraint to use for historical playback.</param>
        /// <param name="processingInterval">Defines the desired processing interval milliseconds, i.e., historical play back speed, to use when temporal constraints are defined.</param>
        /// <param name="waitHandleNames">Comma separated list of wait handle names used to establish external event wait handles needed for inter-adapter synchronization.</param>
        /// <param name="waitHandleTimeout">Maximum wait time for external events, in milliseconds, before proceeding.</param>
        /// <returns><c>true</c> if subscribe transmission was successful; otherwise <c>false</c>.</returns>
        /// <remarks>
        /// <para>
        /// When the <paramref name="startTime"/> or <paramref name="stopTime"/> temporal processing constraints are defined (i.e., not <c>null</c>), this
        /// specifies the start and stop time over which the subscriber session will process data. Passing in <c>null</c> for the <paramref name="startTime"/>
        /// and <paramref name="stopTime"/> specifies the subscriber session will process data in standard, i.e., real-time, operation.
        /// </para>
        /// <para>
        /// With the exception of the values of -1 and 0, the <paramref name="processingInterval"/> value specifies the desired historical playback data
        /// processing interval in milliseconds. This is basically a delay, or timer interval, over which to process data. Setting this value to -1 means
        /// to use the default processing interval while setting the value to 0 means to process data as fast as possible.
        /// </para>
        /// <para>
        /// The <paramref name="startTime"/> and <paramref name="stopTime"/> parameters can be specified in one of the
        /// following formats:
        /// <list type="table">
        ///     <listheader>
        ///         <term>Time Format</term>
        ///         <description>Format Description</description>
        ///     </listheader>
        ///     <item>
        ///         <term>12-30-2000 23:59:59.033</term>
        ///         <description>Absolute date and time.</description>
        ///     </item>
        ///     <item>
        ///         <term>*</term>
        ///         <description>Evaluates to <see cref="DateTime.UtcNow"/>.</description>
        ///     </item>
        ///     <item>
        ///         <term>*-20s</term>
        ///         <description>Evaluates to 20 seconds before <see cref="DateTime.UtcNow"/>.</description>
        ///     </item>
        ///     <item>
        ///         <term>*-10m</term>
        ///         <description>Evaluates to 10 minutes before <see cref="DateTime.UtcNow"/>.</description>
        ///     </item>
        ///     <item>
        ///         <term>*-1h</term>
        ///         <description>Evaluates to 1 hour before <see cref="DateTime.UtcNow"/>.</description>
        ///     </item>
        ///     <item>
        ///         <term>*-1d</term>
        ///         <description>Evaluates to 1 day before <see cref="DateTime.UtcNow"/>.</description>
        ///     </item>
        /// </list>
        /// </para>
        /// </remarks>
        [Obsolete("Preferred method uses SubscriptionInfo object to subscribe.", false)]
        public virtual bool Subscribe(bool compactFormat, bool throttled, string filterExpression, string dataChannel = null, bool includeTime = true, double lagTime = 10.0D, double leadTime = 5.0D, bool useLocalClockAsRealTime = false, string startTime = null, string stopTime = null, string constraintParameters = null, int processingInterval = -1, string waitHandleNames = null, int waitHandleTimeout = 0)
        {
            StringBuilder connectionString = new StringBuilder();
            AssemblyInfo assemblyInfo = AssemblyInfo.ExecutingAssembly;

            connectionString.AppendFormat("trackLatestMeasurements={0}; ", throttled);
            connectionString.AppendFormat("inputMeasurementKeys={{{0}}}; ", filterExpression.ToNonNullString());
            connectionString.AppendFormat("dataChannel={{{0}}}; ", dataChannel.ToNonNullString());
            connectionString.AppendFormat("includeTime={0}; ", includeTime);
            connectionString.AppendFormat("lagTime={0}; ", lagTime);
            connectionString.AppendFormat("leadTime={0}; ", leadTime);
            connectionString.AppendFormat("useLocalClockAsRealTime={0}; ", useLocalClockAsRealTime);
            connectionString.AppendFormat("startTimeConstraint={0}; ", startTime.ToNonNullString());
            connectionString.AppendFormat("stopTimeConstraint={0}; ", stopTime.ToNonNullString());
            connectionString.AppendFormat("timeConstraintParameters={0}; ", constraintParameters.ToNonNullString());
            connectionString.AppendFormat("processingInterval={0}; ", processingInterval);
            connectionString.AppendFormat("useMillisecondResolution={0}; ", m_useMillisecondResolution);
            connectionString.AppendFormat("requestNaNValueFilter={0}; ", m_requestNaNValueFilter);
            connectionString.AppendFormat("assemblyInfo={{source={0}; version={1}.{2}.{3}; buildDate={4}}}", assemblyInfo.Name, assemblyInfo.Version.Major, assemblyInfo.Version.Minor, assemblyInfo.Version.Build, assemblyInfo.BuildDate.ToString("yyyy-MM-dd HH:mm:ss"));

            if (!string.IsNullOrWhiteSpace(waitHandleNames))
            {
                connectionString.AppendFormat("; waitHandleNames={0}", waitHandleNames);
                connectionString.AppendFormat("; waitHandleTimeout={0}", waitHandleTimeout);
            }

            // Make sure not to monitor for data loss any faster than down-sample time on throttled connections - additionally
            // you will want to make sure data stream monitor is twice lag-time to allow time for initial points to arrive.
            if (throttled && (object)m_dataStreamMonitor != null && m_dataStreamMonitor.Interval / 1000.0D < lagTime)
                m_dataStreamMonitor.Interval = (int)(2.0D * lagTime * 1000.0D);

            return Subscribe(compactFormat, connectionString.ToString());
        }

        /// <summary>
        /// Subscribes (or re-subscribes) to a data publisher for a set of data points.
        /// </summary>
        /// <param name="compactFormat">Boolean value that determines if the compact measurement format should be used. Set to <c>false</c> for full fidelity measurement serialization; otherwise set to <c>true</c> for bandwidth conservation.</param>
        /// <param name="connectionString">Connection string that defines required and optional parameters for the subscription.</param>
        /// <returns><c>true</c> if subscribe transmission was successful; otherwise <c>false</c>.</returns>
        public virtual bool Subscribe(bool compactFormat, string connectionString)
        {
            bool success = false;

            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                try
                {
                    // Parse connection string to see if it contains a data channel definition
                    Dictionary<string, string> settings = connectionString.ParseKeyValuePairs();
                    UdpClient dataChannel = null;

                    // Track specified time inclusion for later deserialization
                    if (settings.TryGetValue("includeTime", out string setting))
                        m_includeTime = setting.ParseBoolean();
                    else
                        m_includeTime = true;

                    settings.TryGetValue("dataChannel", out setting);

                    if (!string.IsNullOrWhiteSpace(setting))
                    {
                        if ((CompressionModes & CompressionModes.TSSC) > 0)
                        {
                            // TSSC is a stateful compression algorithm which will not reliably support UDP
                            OnStatusMessage(MessageLevel.Warning, "Cannot use TSSC compression mode with UDP - special compression mode disabled");

                            // Disable TSSC compression processing
                            CompressionModes &= ~CompressionModes.TSSC;
                        }

                        dataChannel = new UdpClient(setting);

                        dataChannel.ReceiveBufferSize = ushort.MaxValue;
                        dataChannel.MaxConnectionAttempts = -1;
                        dataChannel.ConnectAsync();
                    }

                    // Assign data channel client reference and attach to needed events
                    DataChannel = dataChannel;

                    // Setup subscription packet
                    using (MemoryStream buffer = new MemoryStream())
                    {
                        DataPacketFlags flags = DataPacketFlags.NoFlags;
                        byte[] bytes;

                        if (compactFormat)
                            flags |= DataPacketFlags.Compact;

                        // Write data packet flags into buffer
                        buffer.WriteByte((byte)flags);

                        // Get encoded bytes of connection string
                        bytes = m_encoding.GetBytes(connectionString);

                        // Write encoded connection string length into buffer
                        buffer.Write(BigEndian.GetBytes(bytes.Length), 0, 4);

                        // Encode connection string into buffer
                        buffer.Write(bytes, 0, bytes.Length);

                        // Send subscribe server command with associated command buffer
                        success = SendServerCommand(ServerCommand.Subscribe, buffer.ToArray());
                    }
                }
                catch (Exception ex)
                {
                    OnProcessException(MessageLevel.Error, new InvalidOperationException("Exception occurred while trying to make publisher subscription: " + ex.Message, ex));
                }
            }
            else
            {
                OnProcessException(MessageLevel.Error, new InvalidOperationException("Cannot make publisher subscription without a connection string."));
            }

            // Reset decompressor on successful resubscription
            if (success)
            {
                m_tsscResetRequested = true;
            }

            return success;
        }

        /// <summary>
        /// Unsubscribes from a data publisher.
        /// </summary>
        /// <returns><c>true</c> if unsubscribe transmission was successful; otherwise <c>false</c>.</returns>
        public virtual bool Unsubscribe()
        {
            // Send unsubscribe server command
            return SendServerCommand(ServerCommand.Unsubscribe);
        }

        /// <summary>
        /// Returns the measurements signal IDs that were authorized after the last successful subscription request.
        /// </summary>
        public virtual Guid[] GetAuthorizedSignalIDs()
        {
            if ((object)m_signalIndexCache != null)
                return m_signalIndexCache.AuthorizedSignalIDs;

            return new Guid[0];
        }

        /// <summary>
        /// Returns the measurements signal IDs that were unauthorized after the last successful subscription request.
        /// </summary>
        public virtual Guid[] GetUnauthorizedSignalIDs()
        {
            if ((object)m_signalIndexCache != null)
                return m_signalIndexCache.UnauthorizedSignalIDs;

            return new Guid[0];
        }

        /// <summary>
        /// Resets the counters for the lifetime statistics without interrupting the adapter's operations.
        /// </summary>
        public virtual void ResetLifetimeCounters()
        {
            m_lifetimeMeasurements = 0L;
            m_totalBytesReceived = 0L;
            m_lifetimeTotalLatency = 0L;
            m_lifetimeMinimumLatency = 0L;
            m_lifetimeMaximumLatency = 0L;
            m_lifetimeLatencyMeasurements = 0L;
        }

        /// <summary>
        /// Initiate a meta-data refresh.
        /// </summary>
        public virtual void RefreshMetadata()
        {
            SendServerCommand(ServerCommand.MetaDataRefresh, m_metadataFilters);
        }

        /// <summary>
        /// Spawn meta-data synchronization.
        /// </summary>
        /// <param name="metadata"><see cref="DataSet"/> to use for synchronization.</param>
        /// <remarks>
        /// This method makes sure only one meta-data synchronization happens at a time.
        /// </remarks>
        public void SynchronizeMetadata(DataSet metadata)
        {
            try
            {
                m_receivedMetadata = metadata;
                m_synchronizeMetadataOperation.RunOnceAsync();
            }
            catch (Exception ex)
            {
                // Process exception for logging
                OnProcessException(MessageLevel.Warning, new InvalidOperationException("Failed to queue meta-data synchronization: " + ex.Message, ex));
            }
        }

        /// <summary>
        /// Sends a server command to the publisher connection with associated <paramref name="message"/> data.
        /// </summary>
        /// <param name="commandCode"><see cref="ServerCommand"/> to send.</param>
        /// <param name="message">String based command data to send to server.</param>
        /// <returns><c>true</c> if <paramref name="commandCode"/> transmission was successful; otherwise <c>false</c>.</returns>
        public virtual bool SendServerCommand(ServerCommand commandCode, string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                using (MemoryStream buffer = new MemoryStream())
                {
                    byte[] bytes = m_encoding.GetBytes(message);

                    buffer.Write(BigEndian.GetBytes(bytes.Length), 0, 4);
                    buffer.Write(bytes, 0, bytes.Length);

                    return SendServerCommand(commandCode, buffer.ToArray());
                }
            }

            return SendServerCommand(commandCode);
        }

        /// <summary>
        /// Sends a server command to the publisher connection.
        /// </summary>
        /// <param name="commandCode"><see cref="ServerCommand"/> to send.</param>
        /// <param name="data">Optional command data to send.</param>
        /// <returns><c>true</c> if <paramref name="commandCode"/> transmission was successful; otherwise <c>false</c>.</returns>
        public virtual bool SendServerCommand(ServerCommand commandCode, byte[] data = null)
        {
            if ((object)m_commandChannel != null && m_commandChannel.CurrentState == ClientState.Connected)
            {
                try
                {
                    using (MemoryStream commandPacket = new MemoryStream())
                    {
                        // Write command code into command packet
                        commandPacket.WriteByte((byte)commandCode);

                        // Write length of command buffer into command packet
                        int dataLength = data?.Length ?? 0;
                        byte[] lengthBytes = BigEndian.GetBytes(dataLength);
                        commandPacket.Write(lengthBytes);

                        // Write command buffer into command packet
                        if (dataLength > 0)
                            commandPacket.Write(data, 0, data.Length);

                        // Send command packet to publisher
                        m_commandChannel.SendAsync(commandPacket.ToArray(), 0, (int)commandPacket.Length);
                        m_metadataRefreshPending = commandCode == ServerCommand.MetaDataRefresh;
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    OnProcessException(MessageLevel.Error, new InvalidOperationException($"Exception occurred while trying to send server command \"{commandCode}\" to publisher: {ex.Message}", ex));
                }
            }
            else
                OnProcessException(MessageLevel.Error, new InvalidOperationException($"Subscriber is currently unconnected. Cannot send server command \"{commandCode}\" to publisher."));

            return false;
        }

        /// <summary>
        /// Attempts to connect to this <see cref="DataSubscriber"/>.
        /// </summary>
        protected override void AttemptConnection()
        {
            if (!this.TemporalConstraintIsDefined() && !m_supportsRealTimeProcessing)
                return;

            long now = m_useLocalClockAsRealTime ? DateTime.UtcNow.Ticks : 0L;
            List<DeviceStatisticsHelper<SubscribedDevice>> statisticsHelpers = m_statisticsHelpers;

            m_registerStatisticsOperation.RunOnceAsync();
            m_expectedBufferBlockSequenceNumber = 0u;
            m_commandChannelConnectionAttempts = 0;
            m_dataChannelConnectionAttempts = 0;

            m_authenticated = m_securityMode == SecurityMode.TLS;
            m_subscribed = false;
            m_keyIVs = null;
            m_totalBytesReceived = 0L;
            m_monitoredBytesReceived = 0L;
            m_lastBytesReceived = 0;

            m_commandChannelBuffer = null;
            m_commandChannelBufferLength = 0;

            if (!PersistConnectionForMetadata)
                m_commandChannel.ConnectAsync();
            else
                OnConnected();

            if (PersistConnectionForMetadata && CommandChannelConnected)
                SubscribeToOutputMeasurements(true);

            if (m_useLocalClockAsRealTime && (object)m_subscribedDevicesTimer == null)
            {
                m_subscribedDevicesTimer = new Timer(1000);
                m_subscribedDevicesTimer.Elapsed += SubscribedDevicesTimer_Elapsed;
            }

            if ((object)statisticsHelpers != null)
            {
                m_realTime = 0L;
                m_lastStatisticsHelperUpdate = 0L;

                foreach (DeviceStatisticsHelper<SubscribedDevice> statisticsHelper in statisticsHelpers)
                    statisticsHelper.Reset(now);
            }

            if (m_useLocalClockAsRealTime)
                m_subscribedDevicesTimer.Start();
        }

        /// <summary>
        /// Attempts to disconnect from this <see cref="DataSubscriber"/>.
        /// </summary>
        protected override void AttemptDisconnection()
        {
            // Unregister device statistics
            m_registerStatisticsOperation.RunOnceAsync();

            // Stop data stream monitor
            if ((object)m_dataStreamMonitor != null)
                m_dataStreamMonitor.Enabled = false;

            // Disconnect command channel
            if (!PersistConnectionForMetadata && (object)m_commandChannel != null)
                m_commandChannel.Disconnect();

            if ((object)m_subscribedDevicesTimer != null)
                m_subscribedDevicesTimer.Stop();

            m_metadataRefreshPending = false;
        }

        /// <summary>
        /// Gets a short one-line status of this <see cref="DataSubscriber"/>.
        /// </summary>
        /// <param name="maxLength">Maximum length of the status message.</param>
        /// <returns>Text of the status message.</returns>
        public override string GetShortStatus(int maxLength)
        {
            if ((object)m_commandChannel != null && m_commandChannel.CurrentState == ClientState.Connected)
                return $"Subscriber is connected and receiving data points".CenterText(maxLength);

            return "Subscriber is not connected.".CenterText(maxLength);
        }

        /// <summary>
        /// Get message from string based response.
        /// </summary>
        /// <param name="buffer">Response buffer.</param>
        /// <param name="startIndex">Start index of response message.</param>
        /// <param name="length">Length of response message.</param>
        /// <returns>Decoded response string.</returns>
        protected string InterpretResponseMessage(byte[] buffer, int startIndex, int length)
        {
            return m_encoding.GetString(buffer, startIndex, length);
        }

        // Restarts the subscriber.
        private void Restart()
        {
            try
            {
                base.Start();
            }
            catch (Exception ex)
            {
                OnProcessException(MessageLevel.Warning, ex);
            }
        }

        private int ServerResponseLength(byte[] buffer, int length)
        {
            int responseLength = DataPublisher.ClientResponseHeaderSize;

            if (buffer != null && length >= DataPublisher.ClientResponseHeaderSize)
                responseLength += BigEndian.ToInt32(buffer, 2);

            return responseLength;
        }

        private void ProcessServerResponse(byte[] buffer, int length)
        {
            // Currently this work is done on the async socket completion thread, make sure work to be done is timely and if the response processing
            // is coming in via the command channel and needs to send a command back to the server, it should be done on a separate thread...
            if (buffer != null && length > 0)
            {
                try
                {
                    Dictionary<Guid, DeviceStatisticsHelper<SubscribedDevice>> subscribedDevicesLookup;
                    DeviceStatisticsHelper<SubscribedDevice> statisticsHelper;

                    ServerResponse responseCode = (ServerResponse)buffer[0];
                    ServerCommand commandCode = (ServerCommand)buffer[1];
                    int responseLength = BigEndian.ToInt32(buffer, 2);
                    int responseIndex = DataPublisher.ClientResponseHeaderSize;
                    byte[][][] keyIVs;

                    // Disconnect any established UDP data channel upon successful unsubscribe
                    if (commandCode == ServerCommand.Unsubscribe && responseCode == ServerResponse.Succeeded)
                        DataChannel = null;

                    if (!IsUserCommand(commandCode))
                        OnReceivedServerResponse(responseCode, commandCode);
                    else
                        OnReceivedUserCommandResponse(commandCode, responseCode, buffer, responseIndex, length);

                    switch (responseCode)
                    {
                        case ServerResponse.Succeeded:
                            switch (commandCode)
                            {
                                case ServerCommand.Subscribe:
                                    OnStatusMessage(MessageLevel.Info, $"Success code received in response to server command \"{commandCode}\": {InterpretResponseMessage(buffer, responseIndex, responseLength)}");
                                    m_subscribed = true;
                                    break;
                                case ServerCommand.Unsubscribe:
                                    OnStatusMessage(MessageLevel.Info, $"Success code received in response to server command \"{commandCode}\": {InterpretResponseMessage(buffer, responseIndex, responseLength)}");
                                    m_subscribed = false;
                                    if ((object)m_dataStreamMonitor != null)
                                        m_dataStreamMonitor.Enabled = false;
                                    break;
                                case ServerCommand.RotateCipherKeys:
                                    OnStatusMessage(MessageLevel.Info, $"Success code received in response to server command \"{commandCode}\": {InterpretResponseMessage(buffer, responseIndex, responseLength)}");
                                    break;
                                case ServerCommand.MetaDataRefresh:
                                    OnStatusMessage(MessageLevel.Info, $"Success code received in response to server command \"{commandCode}\": latest meta-data received.");
                                    OnMetaDataReceived(DeserializeMetadata(buffer.BlockCopy(responseIndex, responseLength)));
                                    m_metadataRefreshPending = false;
                                    break;
                            }
                            break;
                        case ServerResponse.Failed:
                            OnStatusMessage(MessageLevel.Info, $"Failure code received in response to server command \"{commandCode}\": {InterpretResponseMessage(buffer, responseIndex, responseLength)}");

                            if (commandCode == ServerCommand.MetaDataRefresh)
                                m_metadataRefreshPending = false;
                            break;
                        case ServerResponse.DataPacket:
                            long now = DateTime.UtcNow.Ticks;

                            // Deserialize data packet
                            List<IMeasurement> measurements = new List<IMeasurement>();
                            DataPacketFlags flags;
                            Ticks timestamp = 0;
                            int count;

                            if (m_totalBytesReceived == 0)
                            {
                                // At the point when data is being received, data monitor should be enabled
                                if ((object)m_dataStreamMonitor != null && !m_dataStreamMonitor.Enabled)
                                    m_dataStreamMonitor.Enabled = true;
                            }

                            // Track total data packet bytes received from any channel
                            m_totalBytesReceived += m_lastBytesReceived;
                            m_monitoredBytesReceived += m_lastBytesReceived;

                            // Get data packet flags
                            flags = (DataPacketFlags)buffer[responseIndex];
                            responseIndex++;

                            bool compactMeasurementFormat = (byte)(flags & DataPacketFlags.Compact) > 0;
                            bool compressedPayload = (byte)(flags & DataPacketFlags.Compressed) > 0;
                            int cipherIndex = (flags & DataPacketFlags.CipherIndex) > 0 ? 1 : 0;

                            // Decrypt data packet payload if keys are available
                            if ((object)m_keyIVs != null)
                            {
                                // Get a local copy of volatile keyIVs reference since this can change at any time
                                keyIVs = m_keyIVs;

                                // Decrypt payload portion of data packet
                                buffer = Common.SymmetricAlgorithm.Decrypt(buffer, responseIndex, responseLength - 1, keyIVs[cipherIndex][0], keyIVs[cipherIndex][1]);
                                responseIndex = 0;
                                responseLength = buffer.Length;
                            }

                            // Deserialize number of measurements that follow
                            count = BigEndian.ToInt32(buffer, responseIndex);
                            responseIndex += 4;

                            if (compressedPayload)
                            {
                                if (CompressionModes.HasFlag(CompressionModes.TSSC))
                                {
                                    try
                                    {
                                        // Decompress TSSC serialized measurements from payload
                                        ParseTSSCMeasurements(buffer, responseLength, ref responseIndex, measurements);
                                    }
                                    catch (Exception ex)
                                    {
                                        OnProcessException(MessageLevel.Error, new InvalidOperationException($"Decompression failure: (Decoded {measurements.Count} of {count} measurements)" + ex.Message, ex));
                                    }
                                }
                                else
                                {
                                    OnProcessException(MessageLevel.Error, new InvalidOperationException("Decompression failure: Unexpected compression type in use - STTP currently only supports TSSC payload compression"));
                                }
                            }
                            else
                            {
                                // Deserialize measurements
                                for (int i = 0; i < count; i++)
                                {
                                    if (!compactMeasurementFormat)
                                    {
                                        // Deserialize full measurement format
                                        SerializableMeasurement measurement = new SerializableMeasurement(m_encoding);
                                        responseIndex += measurement.ParseBinaryImage(buffer, responseIndex, responseLength - responseIndex);
                                        measurements.Add(measurement);
                                    }
                                    else if ((object)m_signalIndexCache != null)
                                    {
                                        // Deserialize compact measurement format
                                        CompactMeasurement measurement = new CompactMeasurement(m_signalIndexCache, m_includeTime, m_baseTimeOffsets, m_timeIndex, m_useMillisecondResolution);
                                        responseIndex += measurement.ParseBinaryImage(buffer, responseIndex, responseLength - responseIndex);

                                        // Apply timestamp from frame if not included in transmission
                                        if (!measurement.IncludeTime)
                                            measurement.Timestamp = timestamp;

                                        measurements.Add(measurement);
                                    }
                                    else if (m_lastMissingCacheWarning + MissingCacheWarningInterval < now)
                                    {
                                        // Warning message for missing signal index cache
                                        if (m_lastMissingCacheWarning != 0L)
                                            OnStatusMessage(MessageLevel.Error, "Signal index cache has not arrived. No compact measurements can be parsed.");

                                        m_lastMissingCacheWarning = now;
                                    }
                                }
                            }

                            // Calculate statistics
                            subscribedDevicesLookup = m_subscribedDevicesLookup;
                            statisticsHelper = null;

                            if ((object)subscribedDevicesLookup != null)
                            {
                                IEnumerable<IGrouping<DeviceStatisticsHelper<SubscribedDevice>, IMeasurement>> deviceGroups = measurements
                                    .Where(measurement => subscribedDevicesLookup.TryGetValue(measurement.ID, out statisticsHelper))
                                    .Select(measurement => Tuple.Create(statisticsHelper, measurement))
                                    .ToList()
                                    .GroupBy(tuple => tuple.Item1, tuple => tuple.Item2);

                                foreach (IGrouping<DeviceStatisticsHelper<SubscribedDevice>, IMeasurement> deviceGroup in deviceGroups)
                                {
                                    statisticsHelper = deviceGroup.Key;

                                    foreach (IGrouping<Ticks, IMeasurement> frame in deviceGroup.GroupBy(measurement => measurement.Timestamp))
                                    {
                                        // Determine the number of measurements received with valid values
                                        const MeasurementStateFlags ErrorFlags = MeasurementStateFlags.BadData | MeasurementStateFlags.BadTime | MeasurementStateFlags.SystemError;
                                        Func<MeasurementStateFlags, bool> hasError = stateFlags => (stateFlags & ErrorFlags) != MeasurementStateFlags.Normal;
                                        int measurementsReceived = frame.Count(measurement => !double.IsNaN(measurement.Value));
                                        int measurementsWithError = frame.Count(measurement => !double.IsNaN(measurement.Value) && hasError(measurement.StateFlags));

                                        IMeasurement statusFlags = null;
                                        IMeasurement frequency = null;
                                        IMeasurement deltaFrequency = null;

                                        // Attempt to update real-time
                                        if (!m_useLocalClockAsRealTime && frame.Key > m_realTime)
                                            m_realTime = frame.Key;

                                        // Search the frame for status flags, frequency, and delta frequency
                                        foreach (IMeasurement measurement in frame)
                                        {
                                            if (measurement.ID == statisticsHelper.Device.StatusFlagsID)
                                                statusFlags = measurement;
                                            else if (measurement.ID == statisticsHelper.Device.FrequencyID)
                                                frequency = measurement;
                                            else if (measurement.ID == statisticsHelper.Device.DeltaFrequencyID)
                                                deltaFrequency = measurement;
                                        }

                                        // If we are receiving status flags for this device,
                                        // count the data quality, time quality, and device errors
                                        if ((object)statusFlags != null)
                                        {
                                            uint commonStatusFlags = (uint)statusFlags.Value;

                                            if ((commonStatusFlags & (uint)Bits.Bit19) > 0)
                                                statisticsHelper.Device.DataQualityErrors++;

                                            if ((commonStatusFlags & (uint)Bits.Bit18) > 0)
                                                statisticsHelper.Device.TimeQualityErrors++;

                                            if ((commonStatusFlags & (uint)Bits.Bit16) > 0)
                                                statisticsHelper.Device.DeviceErrors++;

                                            measurementsReceived--;

                                            if (hasError(statusFlags.StateFlags))
                                                measurementsWithError--;
                                        }

                                        // Zero is not a valid value for frequency.
                                        // If frequency is zero, invalidate both frequency and delta frequency
                                        if ((object)frequency != null && frequency.Value == 0.0D)
                                        {
                                            if ((object)deltaFrequency != null && !double.IsNaN(deltaFrequency.Value))
                                                measurementsReceived -= 2;
                                            else
                                                measurementsReceived--;

                                            if (hasError(frequency.StateFlags))
                                            {
                                                if ((object)deltaFrequency != null && !double.IsNaN(deltaFrequency.Value))
                                                    measurementsWithError -= 2;
                                                else
                                                    measurementsWithError--;
                                            }
                                        }

                                        // Track the number of measurements received
                                        statisticsHelper.AddToMeasurementsReceived(measurementsReceived);
                                        statisticsHelper.AddToMeasurementsWithError(measurementsWithError);
                                    }
                                }
                            }

                            OnNewMeasurements(measurements);

                            // Gather statistics on received data
                            DateTime timeReceived = RealTime;

                            if (!m_useLocalClockAsRealTime && timeReceived.Ticks - m_lastStatisticsHelperUpdate > Ticks.PerSecond)
                            {
                                UpdateStatisticsHelpers();
                                m_lastStatisticsHelperUpdate = m_realTime;
                            }

                            m_lifetimeMeasurements += measurements.Count;
                            UpdateMeasurementsPerSecond(timeReceived, measurements.Count);

                            for (int x = 0; x < measurements.Count; x++)
                            {
                                long latency = timeReceived.Ticks - (long)measurements[x].Timestamp;

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
                            break;
                        case ServerResponse.BufferBlock:
                            // Buffer block received - wrap as a buffer block measurement and expose back to consumer
                            uint sequenceNumber = BigEndian.ToUInt32(buffer, responseIndex);
                            int cacheIndex = (int)(sequenceNumber - m_expectedBufferBlockSequenceNumber);
                            BufferBlockMeasurement bufferBlockMeasurement;
                            int signalIndex;

                            // Check if this buffer block has already been processed (e.g., mistaken retransmission due to timeout)
                            if (cacheIndex >= 0 && (cacheIndex >= m_bufferBlockCache.Count || (object)m_bufferBlockCache[cacheIndex] == null))
                            {
                                // Send confirmation that buffer block is received
                                SendServerCommand(ServerCommand.ConfirmBufferBlock, buffer.BlockCopy(responseIndex, 4));

                                // Get measurement key from signal index cache
                                signalIndex = BigEndian.ToUInt16(buffer, responseIndex + 4);

                                if (!m_signalIndexCache.Reference.TryGetValue(signalIndex, out MeasurementKey measurementKey))
                                    throw new InvalidOperationException("Failed to find associated signal identification for runtime ID " + signalIndex);

                                // Skip the sequence number and signal index when creating the buffer block measurement
                                bufferBlockMeasurement = new BufferBlockMeasurement(buffer, responseIndex + 6, responseLength - 6)
                                {
                                    Metadata = measurementKey.Metadata
                                };

                                // Determine if this is the next buffer block in the sequence
                                if (sequenceNumber == m_expectedBufferBlockSequenceNumber)
                                {
                                    List<IMeasurement> bufferBlockMeasurements = new List<IMeasurement>();
                                    int i;

                                    // Add the buffer block measurement to the list of measurements to be published
                                    bufferBlockMeasurements.Add(bufferBlockMeasurement);
                                    m_expectedBufferBlockSequenceNumber++;

                                    // Add cached buffer block measurements to the list of measurements to be published
                                    for (i = 1; i < m_bufferBlockCache.Count; i++)
                                    {
                                        if ((object)m_bufferBlockCache[i] == null)
                                            break;

                                        bufferBlockMeasurements.Add(m_bufferBlockCache[i]);
                                        m_expectedBufferBlockSequenceNumber++;
                                    }

                                    // Remove published measurements from the buffer block queue
                                    if (m_bufferBlockCache.Count > 0)
                                        m_bufferBlockCache.RemoveRange(0, i);

                                    // Publish measurements
                                    OnNewMeasurements(bufferBlockMeasurements);
                                }
                                else
                                {
                                    // Ensure that the list has at least as many
                                    // elements as it needs to cache this measurement
                                    for (int i = m_bufferBlockCache.Count; i <= cacheIndex; i++)
                                        m_bufferBlockCache.Add(null);

                                    // Insert this buffer block into the proper location in the list
                                    m_bufferBlockCache[cacheIndex] = bufferBlockMeasurement;
                                }
                            }

                            m_lifetimeMeasurements += 1;
                            UpdateMeasurementsPerSecond(DateTime.UtcNow, 1);
                            break;
                        case ServerResponse.DataStartTime:
                            // Raise data start time event
                            OnDataStartTime(BigEndian.ToInt64(buffer, responseIndex));
                            break;
                        case ServerResponse.ProcessingComplete:
                            // Raise input processing completed event
                            OnProcessingComplete(InterpretResponseMessage(buffer, responseIndex, responseLength));
                            break;
                        case ServerResponse.UpdateSignalIndexCache:
                            // Deserialize new signal index cache
                            m_remoteSignalIndexCache = DeserializeSignalIndexCache(buffer.BlockCopy(responseIndex, responseLength));
                            m_signalIndexCache = new SignalIndexCache(DataSource, m_remoteSignalIndexCache);
                            FixExpectedMeasurementCounts();
                            break;
                        case ServerResponse.UpdateBaseTimes:
                            // Get active time index
                            m_timeIndex = BigEndian.ToInt32(buffer, responseIndex);
                            responseIndex += 4;

                            // Deserialize new base time offsets
                            m_baseTimeOffsets = new[] { BigEndian.ToInt64(buffer, responseIndex), BigEndian.ToInt64(buffer, responseIndex + 8) };
                            break;
                        case ServerResponse.UpdateCipherKeys:
                            // Move past active cipher index (not currently used anywhere else)
                            responseIndex++;

                            // Extract remaining response
                            byte[] bytes = buffer.BlockCopy(responseIndex, responseLength - 1);

                            // Deserialize new cipher keys
                            keyIVs = new byte[2][][];
                            keyIVs[EvenKey] = new byte[2][];
                            keyIVs[OddKey] = new byte[2][];

                            int index = 0;
                            int bufferLen;

                            // Read even key size
                            bufferLen = BigEndian.ToInt32(bytes, index);
                            index += 4;

                            // Read even key
                            keyIVs[EvenKey][KeyIndex] = new byte[bufferLen];
                            Buffer.BlockCopy(bytes, index, keyIVs[EvenKey][KeyIndex], 0, bufferLen);
                            index += bufferLen;

                            // Read even initialization vector size
                            bufferLen = BigEndian.ToInt32(bytes, index);
                            index += 4;

                            // Read even initialization vector
                            keyIVs[EvenKey][IVIndex] = new byte[bufferLen];
                            Buffer.BlockCopy(bytes, index, keyIVs[EvenKey][IVIndex], 0, bufferLen);
                            index += bufferLen;

                            // Read odd key size
                            bufferLen = BigEndian.ToInt32(bytes, index);
                            index += 4;

                            // Read odd key
                            keyIVs[OddKey][KeyIndex] = new byte[bufferLen];
                            Buffer.BlockCopy(bytes, index, keyIVs[OddKey][KeyIndex], 0, bufferLen);
                            index += bufferLen;

                            // Read odd initialization vector size
                            bufferLen = BigEndian.ToInt32(bytes, index);
                            index += 4;

                            // Read odd initialization vector
                            keyIVs[OddKey][IVIndex] = new byte[bufferLen];
                            Buffer.BlockCopy(bytes, index, keyIVs[OddKey][IVIndex], 0, bufferLen);
                            //index += bufferLen;

                            // Exchange keys
                            m_keyIVs = keyIVs;

                            OnStatusMessage(MessageLevel.Info, "Successfully established new cipher keys for data packet transmissions.");
                            break;
                        case ServerResponse.Notify:
                            // Skip the 4-byte hash
                            string message = m_encoding.GetString(buffer, responseIndex + 4, responseLength - 4);

                            // Display notification
                            OnStatusMessage(MessageLevel.Info, $"NOTIFICATION: {message}");
                            OnNotificationReceived(message);

                            // Send confirmation of receipt of the notification
                            SendServerCommand(ServerCommand.ConfirmNotification, buffer.BlockCopy(responseIndex, 4));
                            break;
                        case ServerResponse.ConfigurationChanged:
                            OnStatusMessage(MessageLevel.Info, "Received notification from publisher that configuration has changed.");
                            OnServerConfigurationChanged();

                            // Initiate meta-data refresh when publisher configuration has changed - we only do this
                            // for automatic connections since API style connections have to manually initiate a
                            // meta-data refresh. API style connection should attach to server configuration changed
                            // event and request meta-data refresh to complete automated cycle.
                            if (m_autoConnect && m_autoSynchronizeMetadata)
                                SendServerCommand(ServerCommand.MetaDataRefresh, m_metadataFilters);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    OnProcessException(MessageLevel.Error, new InvalidOperationException("Failed to process publisher response packet due to exception: " + ex.Message, ex));
                }
            }
        }

        private void ParseTSSCMeasurements(byte[] buffer, int responseLength, ref int responseIndex, List<IMeasurement> measurements)
        {
            // Use TSSC compression to decompress measurements                                            
            if ((object)m_tsscDecoder == null)
            {
                m_tsscDecoder = new TsscDecoder();
                m_tsscSequenceNumber = 0;
            }

            if (buffer[responseIndex] != 85)
                throw new Exception($"TSSC version not recognized: {buffer[responseIndex]}");

            responseIndex++;

            int sequenceNumber = BigEndian.ToUInt16(buffer, responseIndex);
            responseIndex += 2;

            if (sequenceNumber == 0)
            {
                OnStatusMessage(MessageLevel.Info, $"TSSC algorithm reset before sequence number: {m_tsscSequenceNumber}", "TSSC");
                m_tsscDecoder = new TsscDecoder();
                m_tsscSequenceNumber = 0;
                m_tsscResetRequested = false;
            }

            if (m_tsscSequenceNumber != sequenceNumber)
            {
                if (!m_tsscResetRequested)
                    throw new Exception($"TSSC is out of sequence. Expecting: {m_tsscSequenceNumber}, Received: {sequenceNumber}");

                // Ignore packets until the reset has occurred.
                LogEventPublisher publisher = Log.RegisterEvent(MessageLevel.Debug, "TSSC", 0, MessageRate.EveryFewSeconds(1), 5);
                publisher.ShouldRaiseMessageSupressionNotifications = false;
                publisher.Publish($"TSSC is out of sequence. Expecting: {m_tsscSequenceNumber}, Received: {sequenceNumber}");
                return;
            }

            m_tsscDecoder.SetBuffer(buffer, responseIndex, responseLength + DataPublisher.ClientResponseHeaderSize - responseIndex);

            Measurement measurement;
            MeasurementKey key = null;

            while (m_tsscDecoder.TryGetMeasurement(out int id, out long time, out uint quality, out float value))
            {
                if (m_signalIndexCache?.Reference.TryGetValue(id, out key) ?? false)
                {
                    measurement = new Measurement();
                    measurement.Metadata = key?.Metadata;
                    measurement.Timestamp = time;
                    measurement.StateFlags = (MeasurementStateFlags)quality;
                    measurement.Value = value;
                    measurements.Add(measurement);
                }
            }

            m_tsscSequenceNumber++;

            // Do not increment to 0 on roll-over
            if (m_tsscSequenceNumber == 0)
                m_tsscSequenceNumber = 1;
        }

        private bool IsUserCommand(ServerCommand command)
        {
            ServerCommand[] userCommands =
            {
                ServerCommand.UserCommand00,
                ServerCommand.UserCommand01,
                ServerCommand.UserCommand02,
                ServerCommand.UserCommand03,
                ServerCommand.UserCommand04,
                ServerCommand.UserCommand05,
                ServerCommand.UserCommand06,
                ServerCommand.UserCommand07,
                ServerCommand.UserCommand08,
                ServerCommand.UserCommand09,
                ServerCommand.UserCommand10,
                ServerCommand.UserCommand11,
                ServerCommand.UserCommand12,
                ServerCommand.UserCommand13,
                ServerCommand.UserCommand14,
                ServerCommand.UserCommand15
            };

            return userCommands.Contains(command);
        }

        // Handles auto-connection subscription initialization
        private void StartSubscription()
        {
            SubscribeToOutputMeasurements(!m_autoSynchronizeMetadata);

            // Initiate meta-data refresh
            if (m_autoSynchronizeMetadata && !this.TemporalConstraintIsDefined())
                SendServerCommand(ServerCommand.MetaDataRefresh, m_metadataFilters);
        }

        private void SubscribeToOutputMeasurements(bool metaDataRefreshCompleted)
        {
            StringBuilder filterExpression = new StringBuilder();
            string dataChannel = null;
            string startTimeConstraint = null;
            string stopTimeConstraint = null;
            int processingInterval = -1;

            // If TCP command channel is defined separately, then base connection string defines data channel
            if (Settings.ContainsKey("commandChannel"))
                dataChannel = ConnectionString;

            if (this.TemporalConstraintIsDefined())
            {
                startTimeConstraint = StartTimeConstraint.ToString("yyyy-MM-dd HH:mm:ss.fffffff");
                stopTimeConstraint = StopTimeConstraint.ToString("yyyy-MM-dd HH:mm:ss.fffffff");
                processingInterval = ProcessingInterval;
            }

            MeasurementKey[] outputMeasurementKeys = AutoStart
                ? this.OutputMeasurementKeys()
                : RequestedOutputMeasurementKeys;

            if ((object)outputMeasurementKeys != null && outputMeasurementKeys.Length > 0)
            {
                foreach (MeasurementKey measurementKey in outputMeasurementKeys)
                {
                    if (filterExpression.Length > 0)
                        filterExpression.Append(';');

                    // Subscribe by associated Guid...
                    filterExpression.Append(measurementKey.SignalID);
                }

                // Start unsynchronized subscription
                #pragma warning disable 0618
                Subscribe(true, false, filterExpression.ToString(), dataChannel, startTime: startTimeConstraint, stopTime: stopTimeConstraint, processingInterval: processingInterval);
            }
            else
            {
                Unsubscribe();

                if (AutoStart && metaDataRefreshCompleted)
                    OnStatusMessage(MessageLevel.Error, "No measurements are currently defined for subscription.");
            }
        }

        /// <summary>
        /// Handles meta-data synchronization to local system.
        /// </summary>
        /// <remarks>
        /// This function should only be initiated from call to <see cref="SynchronizeMetadata(DataSet)"/> to make
        /// sure only one meta-data synchronization happens at once. Users can override this method to customize
        /// process of meta-data synchronization.
        /// </remarks>
        protected virtual void SynchronizeMetadata()
        {
            bool dataMonitoringEnabled = false;

            // TODO: This function is complex and very closely tied to the current time-series data schema - perhaps it should be moved outside this class and referenced
            // TODO: as a delegate that can be assigned and called to allow other schemas as well. DataPublisher is already very flexible in what data it can deliver.
            try
            {
                DataSet metadata = m_receivedMetadata;

                // Only perform database synchronization if meta-data has changed since last update
                if (!SynchronizedMetadataChanged(metadata))
                    return;

                if ((object)metadata == null)
                {
                    OnStatusMessage(MessageLevel.Error, "Meta-data synchronization was not performed, deserialized dataset was empty.");
                    return;
                }

                // Reset data stream monitor while meta-data synchronization is in progress
                if ((object)m_dataStreamMonitor != null && m_dataStreamMonitor.Enabled)
                {
                    m_dataStreamMonitor.Enabled = false;
                    dataMonitoringEnabled = true;
                }

                // Track total meta-data synchronization process time
                Ticks startTime = DateTime.UtcNow.Ticks;
                DateTime latestUpdateTime = DateTime.MinValue;

                // TODO: Handle synchronization - callback / virtual method

                // New signals may have been defined, take original remote signal index cache and apply changes
                if (m_remoteSignalIndexCache != null)
                    m_signalIndexCache = new SignalIndexCache(DataSource, m_remoteSignalIndexCache);

                m_lastMetaDataRefreshTime = latestUpdateTime > DateTime.MinValue ? latestUpdateTime : DateTime.UtcNow;

                OnStatusMessage(MessageLevel.Info, $"Meta-data synchronization completed successfully in {(DateTime.UtcNow.Ticks - startTime).ToElapsedTimeString(2)}");

                // Send notification that system configuration has changed
                OnConfigurationChanged();
            }
            catch (Exception ex)
            {
                OnProcessException(MessageLevel.Error, new InvalidOperationException("Failed to synchronize meta-data to local cache: " + ex.Message, ex));
            }
            finally
            {
                // Restart data stream monitor after meta-data synchronization if it was originally enabled
                if (dataMonitoringEnabled && (object)m_dataStreamMonitor != null)
                    m_dataStreamMonitor.Enabled = true;
            }
        }

        private void InitSyncProgress(long totalActions)
        {
            m_syncProgressTotalActions = totalActions;
            m_syncProgressActionsCount = 0;

            // We update user on progress with 5 messages or every 15 seconds
            m_syncProgressUpdateInterval = (long)(totalActions * 0.2D);
            m_syncProgressLastMessage = DateTime.UtcNow.Ticks;
        }

        private void UpdateSyncProgress()
        {
            m_syncProgressActionsCount++;

            if (m_syncProgressActionsCount % m_syncProgressUpdateInterval == 0 || DateTime.UtcNow.Ticks - m_syncProgressLastMessage > 150000000)
            {
                OnStatusMessage(MessageLevel.Info, $"Meta-data synchronization is {m_syncProgressActionsCount / (double)m_syncProgressTotalActions:0.0%} complete...");
                m_syncProgressLastMessage = DateTime.UtcNow.Ticks;
            }
        }

        private SignalIndexCache DeserializeSignalIndexCache(byte[] buffer)
        {
            CompressionModes compressionModes = (CompressionModes)(m_operationalModes & OperationalModes.CompressionModeMask);
            bool compressSignalIndexCache = (m_operationalModes & OperationalModes.CompressSignalIndexCache) > 0;
            SignalIndexCache deserializedCache;
            GZipStream inflater = null;

            if (compressSignalIndexCache && compressionModes.HasFlag(CompressionModes.GZip))
            {
                try
                {
                    using (MemoryStream compressedData = new MemoryStream(buffer))
                    {
                        inflater = new GZipStream(compressedData, CompressionMode.Decompress, true);
                        buffer = inflater.ReadStream();
                    }
                }
                finally
                {
                    if ((object)inflater != null)
                        inflater.Close();
                }
            }

            deserializedCache = new SignalIndexCache();
            deserializedCache.Encoding = m_encoding;
            deserializedCache.ParseBinaryImage(buffer, 0, buffer.Length);

            return deserializedCache;
        }

        private DataSet DeserializeMetadata(byte[] buffer)
        {
            CompressionModes compressionModes = (CompressionModes)(m_operationalModes & OperationalModes.CompressionModeMask);
            bool compressMetadata = (m_operationalModes & OperationalModes.CompressMetadata) > 0;
            Ticks startTime = DateTime.UtcNow.Ticks;
            DataSet deserializedMetadata;
            GZipStream inflater = null;

            if (compressMetadata && compressionModes.HasFlag(CompressionModes.GZip))
            {
                try
                {
                    // Insert compressed data into compressed buffer
                    using (MemoryStream compressedData = new MemoryStream(buffer))
                    {
                        inflater = new GZipStream(compressedData, CompressionMode.Decompress, true);
                        buffer = inflater.ReadStream();
                    }
                }
                finally
                {
                    if ((object)inflater != null)
                        inflater.Close();
                }
            }

            // Copy decompressed data into encoded buffer
            using (MemoryStream encodedData = new MemoryStream(buffer))
            using (XmlTextReader xmlReader = new XmlTextReader(encodedData))
            {
                // Read encoded data into data set as XML
                deserializedMetadata = new DataSet();
                deserializedMetadata.ReadXml(xmlReader, XmlReadMode.ReadSchema);
            }

            long rowCount = deserializedMetadata.Tables.Cast<DataTable>().Select(dataTable => (long)dataTable.Rows.Count).Sum();

            if (rowCount > 0)
            {
                Time elapsedTime = (DateTime.UtcNow.Ticks - startTime).ToSeconds();
                OnStatusMessage(MessageLevel.Info, $"Received a total of {rowCount:N0} records spanning {deserializedMetadata.Tables.Count:N0} tables of meta-data that was {(compressMetadata ? "uncompressed and " : "")}deserialized in {elapsedTime.ToString(2)}...");
            }

            return deserializedMetadata;
        }

        private Encoding GetCharacterEncoding(OperationalEncoding operationalEncoding)
        {
            Encoding encoding;

            switch (operationalEncoding)
            {
                case OperationalEncoding.UTF16LE:
                    encoding = Encoding.Unicode;
                    break;
                case OperationalEncoding.UTF16BE:
                    encoding = Encoding.BigEndianUnicode;
                    break;
                case OperationalEncoding.UTF8:
                    encoding = Encoding.UTF8;
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported encoding detected: {operationalEncoding}");
            }

            return encoding;
        }

        // Socket exception handler
        private bool HandleSocketException(Exception ex)
        {
            SocketException socketException = ex as SocketException;

            if ((object)socketException != null)
            {
                // WSAECONNABORTED and WSAECONNRESET are common errors after a client disconnect,
                // if they happen for other reasons, make sure disconnect procedure is handled
                if (socketException.ErrorCode == 10053 || socketException.ErrorCode == 10054)
                {
                    DisconnectClient();
                    return true;
                }
            }

            if ((object)ex != null)
                HandleSocketException(ex.InnerException);

            return false;
        }

        // Disconnect client, restarting if disconnect was not intentional
        private void DisconnectClient()
        {
            DataChannel = null;
            m_metadataRefreshPending = false;

            // If user didn't initiate disconnect, restart the connection
            if (Enabled)
                Start();
        }

        private int GetFramesPerSecond(DataTable measurementTable, Guid signalID)
        {
            DataRow row = measurementTable.Select($"SignalID = '{signalID}'").FirstOrDefault();

            if ((object)row != null)
            {
                switch (row.Field<string>("SignalType").ToUpperInvariant())
                {
                    case "FLAG":
                    case "STAT":
                        return 0;

                    default:
                        return row.ConvertField<int>("FramesPerSecond");
                }
            }

            return 0;
        }

        // This method is called when connection has been authenticated
        private void DataSubscriber_ConnectionAuthenticated(object sender, EventArgs e)
        {
            if (m_autoConnect && Enabled)
                StartSubscription();
        }

        // This method is called then new meta-data has been received
        private void DataSubscriber_MetaDataReceived(object sender, EventArgs<DataSet> e)
        {
            try
            {
                // We handle synchronization on a separate thread since this process may be lengthy
                if (m_autoSynchronizeMetadata)
                    SynchronizeMetadata(e.Argument);
            }
            catch (Exception ex)
            {
                // Process exception for logging
                OnProcessException(MessageLevel.Error, new InvalidOperationException("Failed to queue meta-data synchronization due to exception: " + ex.Message, ex));
            }
        }

        /// <summary>
        /// Raises the <see cref="ConnectionEstablished"/> event.
        /// </summary>
        protected void OnConnectionEstablished()
        {
            try
            {
                ConnectionEstablished?.Invoke(this, EventArgs.Empty);
                m_lastMissingCacheWarning = 0L;
            }
            catch (Exception ex)
            {
                // We protect our code from consumer thrown exceptions
                OnProcessException(MessageLevel.Info, new InvalidOperationException($"Exception in consumer handler for ConnectionEstablished event: {ex.Message}", ex), "ConsumerEventException");
            }
        }

        /// <summary>
        /// Raises the <see cref="ConnectionTerminated"/> event.
        /// </summary>
        protected void OnConnectionTerminated()
        {
            try
            {
                ConnectionTerminated?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                // We protect our code from consumer thrown exceptions
                OnProcessException(MessageLevel.Info, new InvalidOperationException($"Exception in consumer handler for ConnectionTerminated event: {ex.Message}", ex), "ConsumerEventException");
            }
        }

        /// <summary>
        /// Raises the <see cref="ConnectionAuthenticated"/> event.
        /// </summary>
        protected void OnConnectionAuthenticated()
        {
            try
            {
                ConnectionAuthenticated?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                // We protect our code from consumer thrown exceptions
                OnProcessException(MessageLevel.Info, new InvalidOperationException($"Exception in consumer handler for ConnectionAuthenticated event: {ex.Message}", ex), "ConsumerEventException");
            }
        }

        /// <summary>
        /// Raises the <see cref="ReceivedServerResponse"/> event.
        /// </summary>
        /// <param name="responseCode">Response received from the server.</param>
        /// <param name="commandCode">Command that the server responded to.</param>
        protected void OnReceivedServerResponse(ServerResponse responseCode, ServerCommand commandCode)
        {
            try
            {
                ReceivedServerResponse?.Invoke(this, new EventArgs<ServerResponse, ServerCommand>(responseCode, commandCode));
            }
            catch (Exception ex)
            {
                // We protect our code from consumer thrown exceptions
                OnProcessException(MessageLevel.Info, new InvalidOperationException($"Exception in consumer handler for ReceivedServerResponse event: {ex.Message}", ex), "ConsumerEventException");
            }
        }

        /// <summary>
        /// Raises the <see cref="ReceivedUserCommandResponse"/> event.
        /// </summary>
        /// <param name="command">The code for the user command.</param>
        /// <param name="response">The code for the server's response.</param>
        /// <param name="buffer">Buffer containing the message from the server.</param>
        /// <param name="startIndex">Index into the buffer used to skip the header.</param>
        /// <param name="length">The length of the message in the buffer, including the header.</param>
        protected void OnReceivedUserCommandResponse(ServerCommand command, ServerResponse response, byte[] buffer, int startIndex, int length)
        {
            try
            {
                UserCommandArgs args = new UserCommandArgs(command, response, buffer, startIndex, length);
                ReceivedUserCommandResponse?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                // We protect our code from consumer thrown exceptions
                OnProcessException(MessageLevel.Info, new InvalidOperationException($"Exception in consumer handler for UserCommandResponse event: {ex.Message}", ex), "ConsumerEventException");
            }
        }

        /// <summary>
        /// Raises the <see cref="MetaDataReceived"/> event.
        /// </summary>
        /// <param name="metadata">Meta-data <see cref="DataSet"/> instance to send to client subscription.</param>
        protected void OnMetaDataReceived(DataSet metadata)
        {
            try
            {
                MetaDataReceived?.Invoke(this, new EventArgs<DataSet>(metadata));
            }
            catch (Exception ex)
            {
                // We protect our code from consumer thrown exceptions
                OnProcessException(MessageLevel.Info, new InvalidOperationException($"Exception in consumer handler for MetaDataReceived event: {ex.Message}", ex), "ConsumerEventException");
            }
        }

        /// <summary>
        /// Raises the <see cref="DataStartTime"/> event.
        /// </summary>
        /// <param name="startTime">Start time, in <see cref="Ticks"/>, of first measurement transmitted.</param>
        protected void OnDataStartTime(Ticks startTime)
        {
            try
            {
                DataStartTime?.Invoke(this, new EventArgs<Ticks>(startTime));
            }
            catch (Exception ex)
            {
                // We protect our code from consumer thrown exceptions
                OnProcessException(MessageLevel.Info, new InvalidOperationException($"Exception in consumer handler for DataStartTime event: {ex.Message}", ex), "ConsumerEventException");
            }
        }

        /// <summary>
        /// Raises the <see cref="ProcessingComplete"/> event.
        /// </summary>
        /// <param name="source">Type name of adapter that sent the processing completed notification.</param>
        protected void OnProcessingComplete(string source)
        {
            try
            {
                ProcessingComplete?.Invoke(this, new EventArgs<string>(source));

                // Also raise base class event in case this event has been subscribed
                OnProcessingComplete();
            }
            catch (Exception ex)
            {
                // We protect our code from consumer thrown exceptions
                OnProcessException(MessageLevel.Info, new InvalidOperationException($"Exception in consumer handler for ProcessingComplete event: {ex.Message}", ex), "ConsumerEventException");
            }
        }

        /// <summary>
        /// Raises the <see cref="NotificationReceived"/> event.
        /// </summary>
        /// <param name="message">Message for the notification.</param>
        protected void OnNotificationReceived(string message)
        {
            try
            {
                NotificationReceived?.Invoke(this, new EventArgs<string>(message));
            }
            catch (Exception ex)
            {
                // We protect our code from consumer thrown exceptions
                OnProcessException(MessageLevel.Info, new InvalidOperationException($"Exception in consumer handler for NotificationReceived event: {ex.Message}", ex), "ConsumerEventException");
            }
        }

        /// <summary>
        /// Raises the <see cref="ServerConfigurationChanged"/> event.
        /// </summary>
        protected void OnServerConfigurationChanged()
        {
            try
            {
                ServerConfigurationChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                // We protect our code from consumer thrown exceptions
                OnProcessException(MessageLevel.Info, new InvalidOperationException($"Exception in consumer handler for ServerConfigurationChanged event: {ex.Message}", ex), "ConsumerEventException");
            }
        }

        /// <summary>
        /// Raises the <see cref="ExceededParsingExceptionThreshold"/> event.
        /// </summary>
        private void OnExceededParsingExceptionThreshold()
        {
            ExceededParsingExceptionThreshold?.Invoke(this, EventArgs.Empty);
        }

        // Updates the measurements per second counters after receiving another set of measurements.
        private void UpdateMeasurementsPerSecond(DateTime now, int measurementCount)
        {
            long secondsSinceEpoch = now.Ticks / Ticks.PerSecond;

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

        private bool SynchronizedMetadataChanged(DataSet newSynchronizedMetadata)
        {
            try
            {
                return !DataSetEqualityComparer.Default.Equals(m_synchronizedMetadata, newSynchronizedMetadata);
            }
            catch
            {
                return true;
            }
        }

        /// <summary>
        /// Gets file path for any defined logging path.
        /// </summary>
        /// <param name="filePath">Path to acquire within logging path.</param>
        /// <returns>File path within any defined logging path.</returns>
        protected string GetLoggingPath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(m_loggingPath))
                return FilePath.GetAbsolutePath(filePath);

            return Path.Combine(m_loggingPath, filePath);
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

            if (DateTime.UtcNow.Ticks - m_lastParsingExceptionTime > m_parsingExceptionWindow)
            {
                // Exception window has passed since last exception, so we reset counters
                m_lastParsingExceptionTime = DateTime.UtcNow.Ticks;
                m_parsingExceptionCount = 0;
            }

            m_parsingExceptionCount++;

            if (m_parsingExceptionCount > m_allowedParsingExceptions)
            {
                try
                {
                    // When the parsing exception threshold has been exceeded, connection is restarted
                    Start();
                }
                catch (Exception ex)
                {
                    base.OnProcessException(MessageLevel.Warning, new InvalidOperationException($"Error while restarting subscriber connection due to excessive exceptions: {ex.Message}", ex), "DataSubscriber", MessageFlags.UsageIssue);
                }
                finally
                {
                    // Notify consumer of parsing exception threshold deviation
                    OnExceededParsingExceptionThreshold();
                    m_lastParsingExceptionTime = 0;
                    m_parsingExceptionCount = 0;
                }
            }
        }


        /// <summary>
        /// Returns <c>true</c> if <see cref="DataSubscriber"/> has a temporal constraint defined, i.e., either
        /// <see cref="StartTimeConstraint"/> or <see cref="StopTimeConstraint"/> is not
        /// set to its default value.
        /// </summary>
        /// <returns><c>true</c> if <see cref="DataSubscriber"/> has a temporal constraint defined.</returns>
        public bool TemporalConstraintIsDefined()
        {
            return StartTimeConstraint != DateTime.MinValue || StopTimeConstraint != DateTime.MaxValue;
        }

        private void m_localConcentrator_ProcessException(object sender, EventArgs<Exception> e)
        {
            // Make sure any exceptions reported by local concentrator get exposed as needed
            OnProcessException(MessageLevel.Warning, e.Argument);
        }

        private void m_dataStreamMonitor_Elapsed(object sender, ElapsedEventArgs e)
        {
            bool dataReceived = m_monitoredBytesReceived > 0;

            if ((object)m_dataChannel == null && m_metadataRefreshPending)
                dataReceived = (DateTime.UtcNow - m_commandChannel.Statistics.LastReceive).Seconds < DataLossInterval;

            if (!dataReceived)
            {
                // If we've received no data in the last time-span, we restart connect cycle...
                m_dataStreamMonitor.Enabled = false;
                OnStatusMessage(MessageLevel.Info, $"\r\nNo data received in {m_dataStreamMonitor.Interval / 1000.0D:0.0} seconds, restarting connect cycle...\r\n", "Connection Issues");
                ThreadPool.QueueUserWorkItem(state => Restart());
            }

            // Reset bytes received bytes being monitored
            m_monitoredBytesReceived = 0L;
        }

        private void ReceiveChannelData(ref byte[] buffer, ref int bufferLength, IClient channelClient, int bytesReceived)
        {
            int totalBytesRead = 0;
            int responseLength = ServerResponseLength(buffer, bufferLength);
            m_lastBytesReceived = bytesReceived;

            while (totalBytesRead < bytesReceived)
            {
                if (buffer == null)
                    buffer = new byte[responseLength];
                else if (buffer.Length < responseLength)
                    Array.Resize(ref buffer, responseLength);

                int readLength = responseLength - bufferLength;
                int bytesRead = channelClient.Read(buffer, bufferLength, readLength);
                totalBytesRead += bytesRead;
                bufferLength += bytesRead;

                // Additional data may have provided more
                // intelligence about the full response length
                if (bufferLength == responseLength)
                    responseLength = ServerResponseLength(buffer, bufferLength);

                // If the response length hasn't changed,
                // it's time to process the response
                if (bufferLength == responseLength)
                {
                    ProcessServerResponse(buffer, bufferLength);
                    bufferLength = 0;
                    responseLength = ServerResponseLength(buffer, bufferLength);
                }
            }
        }

        #region [ Command Channel Event Handlers ]

        private void m_commandChannel_ConnectionEstablished(object sender, EventArgs e)
        {
            // Define operational modes as soon as possible
            SendServerCommand(ServerCommand.DefineOperationalModes, BigEndian.GetBytes((uint)m_operationalModes));

            // Notify input adapter base that asynchronous connection succeeded
            if (!PersistConnectionForMetadata)
                OnConnected();
            else
                SendServerCommand(ServerCommand.MetaDataRefresh, m_metadataFilters);

            // Notify consumer that connection was successfully established
            OnConnectionEstablished();

            OnStatusMessage(MessageLevel.Info, "Data subscriber command channel connection to publisher was established.");

            if (m_autoConnect && Enabled)
                StartSubscription();
        }

        private void m_commandChannel_ConnectionTerminated(object sender, EventArgs e)
        {
            OnConnectionTerminated();
            OnStatusMessage(MessageLevel.Info, "Data subscriber command channel connection to publisher was terminated.");
            DisconnectClient();
        }

        private void m_commandChannel_ConnectionException(object sender, EventArgs<Exception> e)
        {
            Exception ex = e.Argument;
            OnProcessException(MessageLevel.Info, new InvalidOperationException("Data subscriber encountered an exception while attempting command channel publisher connection: " + ex.Message, ex));
        }

        private void m_commandChannel_ConnectionAttempt(object sender, EventArgs e)
        {
            // Inject a short delay between multiple connection attempts
            if (m_commandChannelConnectionAttempts > 0)
                Thread.Sleep(2000);

            OnStatusMessage(MessageLevel.Info, "Attempting command channel connection to publisher...");
            m_commandChannelConnectionAttempts++;
        }

        private void m_commandChannel_SendDataException(object sender, EventArgs<Exception> e)
        {
            Exception ex = e.Argument;

            if (!HandleSocketException(ex) && !(ex is ObjectDisposedException))
                OnProcessException(MessageLevel.Info, new InvalidOperationException("Data subscriber encountered an exception while sending command channel data to publisher connection: " + ex.Message, ex));
        }

        private void m_commandChannel_ReceiveData(object sender, EventArgs<int> e)
        {
            try
            {
                ReceiveChannelData(ref m_commandChannelBuffer, ref m_commandChannelBufferLength, m_commandChannel, e.Argument);
            }
            catch (Exception ex)
            {
                OnProcessException(MessageLevel.Info, ex);
            }
        }

        private void m_commandChannel_ReceiveDataException(object sender, EventArgs<Exception> e)
        {
            Exception ex = e.Argument;

            if (!HandleSocketException(ex) && !(ex is ObjectDisposedException))
                OnProcessException(MessageLevel.Info, new InvalidOperationException("Data subscriber encountered an exception while receiving command channel data from publisher connection: " + ex.Message, ex));
        }

        #endregion

        #region [ Data Channel Event Handlers ]

        private void m_dataChannel_ConnectionException(object sender, EventArgs<Exception> e)
        {
            Exception ex = e.Argument;
            OnProcessException(MessageLevel.Info, new InvalidOperationException("Data subscriber encountered an exception while attempting to establish UDP data channel connection: " + ex.Message, ex));
        }

        private void m_dataChannel_ConnectionAttempt(object sender, EventArgs e)
        {
            // Inject a short delay between multiple connection attempts
            if (m_dataChannelConnectionAttempts > 0)
                Thread.Sleep(2000);

            OnStatusMessage(MessageLevel.Info, "Attempting to establish data channel connection to publisher...");
            m_dataChannelConnectionAttempts++;
        }

        private void m_dataChannel_ReceiveData(object sender, EventArgs<int> e)
        {
            try
            {
                ReceiveChannelData(ref m_dataChannelBuffer, ref m_dataChannelBufferLength, m_dataChannel, e.Argument);
            }
            catch (Exception ex)
            {
                OnProcessException(MessageLevel.Info, ex);
            }
        }

        private void m_dataChannel_ReceiveDataException(object sender, EventArgs<Exception> e)
        {
            Exception ex = e.Argument;

            if (!HandleSocketException(ex) && !(ex is ObjectDisposedException))
                OnProcessException(MessageLevel.Info, new InvalidOperationException("Data subscriber encountered an exception while receiving UDP data from publisher connection: " + ex.Message, ex));
        }

        #endregion

        #endregion
    }
}
;