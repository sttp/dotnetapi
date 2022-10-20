//******************************************************************************************************
//  DataSubscriber.cs - Gbtc
//
//  Copyright © 2022, Grid Protection Alliance.  All Rights Reserved.
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
//  09/15/2022 - J. Ritchie Carroll
//       Generated original version of source code.
//
//******************************************************************************************************

using Gemstone;
using Gemstone.ArrayExtensions;
using Gemstone.StringExtensions;
using sttp.metadata;
using sttp.metadata.record;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace sttp.transport;

/// <summary>
/// Represents a subscription for an STTP connection.
/// </summary>
public class DataSubscriber
{
    private SubscriptionInfo m_subscriptionInfo;
    private Guid m_subscriberID;
    private OperationalEncoding m_encoding;
    private SubscriberConnector m_connector;
    private bool m_connected;
    private bool m_subscribed;

    private Socket? m_commandChannelSocket;
    private Thread? m_commandChannelResponseThread;
    private Socket? m_dataChannelSocket;
    private Thread? m_dataChannelResponseThread;

    private object m_connectActionMutex;
    private object m_connectionTerminationThreadMutex;
    private Thread? m_connectionTerminationThread;

    private Thread? m_disconnectThread;
    private object m_disconnectThreadMutex;
    private bool m_disconnecting;
    private bool m_disconnected;
    private bool m_disposing;

    // Statistic counters
    private long m_totalCommandChannelBytesReceived;
    private long m_totalDataChannelBytesReceived;
    private long m_totalMeasurementsReceived;

    // Measurement parsing
    private long m_metadataRequested;
    private SignalIndexCache[] m_signalIndexCache;
    private object m_signalIndexCacheMutex;
    private int m_cacheIndex;
    private int m_timeIndex;
    private long[] m_baseTimeOffsets;
    private byte[][][]? m_keyIVs;
    private long m_lastMissingCacheWarning;
    private bool m_tsscResetRequested;
    private long m_tsscOOSReport;
    private object m_tsscOOSReportMutex;
    private uint m_bufferBlockExpectedSequenceNumber;
    private List<BufferBlock> m_bufferBlockCache;

    /// <summary>
    /// Creates a new <see cref="DataSubscriber"/>.
    /// </summary>
    public DataSubscriber()
    {
        m_subscriptionInfo = new();
        m_encoding = OperationalEncoding.UTF8;
        m_connector = new();
        m_connectActionMutex = new();
        m_connectionTerminationThreadMutex = new();
        m_disconnectThreadMutex = new();
        m_signalIndexCache = new[] { new SignalIndexCache(), new SignalIndexCache() };
        m_signalIndexCacheMutex = new();
        m_baseTimeOffsets = new[] { 0L, 0L };
        m_tsscOOSReportMutex = new();
        m_bufferBlockCache = new();

        //m_keyIVs = new[]
        //{
        //    new[] { Array.Empty<byte>(), Array.Empty<byte>() },
        //    new[] { Array.Empty<byte>(), Array.Empty<byte>() }
        //};
    }

    /// <summary>
    /// Gets or sets delegate to be called when a informational message should be logged.
    /// </summary>
    public Action<string>? StatusMessageCallback { get; set; }

    /// <summary>
    /// Gets or sets delegate to be called when an error message should be logged.
    /// </summary>
    public Action<string>? ErrorMessageCallback { get; set; }

    /// <summary>
    /// Gets or sets delegate to be called when <see cref="DataSubscriber"/> terminates its connection.
    /// </summary>
    public Action? ConnectionTerminatedCallback { get; set; }

    /// <summary>
    /// Gets or sets delegate to be called when <see cref="DataSubscriber"/> automatically reconnects.
    /// </summary>
    public Action? AutoReconnectCallback { get; set; }

    /// <summary>
    /// Gets or sets delegate to be called when <see cref="DataSubscriber"/> receives a metadata response.
    /// </summary>
    public Action<byte[]>? MetadataReceivedCallback { get; set; }

    /// <summary>
    /// Gets or sets delegate to be called when <see cref="DataSubscriber"/> receives a new sigbal index cache.
    /// </summary>
    public Action<SignalIndexCache>? SubscriptionUpdatedCallback { get; set; }

    /// <summary>
    /// Gets or sets delegate to be called with timestamp of first received measurement in a subscription.
    /// </summary>
    public Action<ulong>? DataStartTimeCallback { get; set; }

    /// <summary>
    /// Gets or sets delegate to be called when the <see cref="DataPublisher"/> sends a notification that configuration has changed.
    /// </summary>
    public Action? ConfigurationChangedCallback { get; set; }

    /// <summary>
    /// Gets or sets delegate to be called when the <see cref="DataSubscriber"/> receives a set of new measurements from the <see cref="DataPublisher"/>.
    /// </summary>
    public Action<Measurement[]>? NewMeasurementsCallback { get; set; }

    /// <summary>
    /// Gets or sets delegate to be called when the <see cref="DataSubscriber"/> receives a set of new buffer block measurements from the <see cref="DataPublisher"/>.
    /// </summary>
    public Action<BufferBlock[]>? NewBufferBlocksCallback { get; set; }

    /// <summary>
    /// Gets or sets delegate to be called when the <see cref="DataPublisher"/> sends a notification that temporal processing has completed, i.e., the end of a historical playback data stream has been reached.
    /// </summary>
    public Action<string>? ProcessingCompleteCallback { get; set; }

    /// <summary>
    /// Gets or sets delegate to be called when the <see cref="DataPublisher"/> sends a notification that requires receipt.
    /// </summary>
    public Action<string>? NotificationReceivedCallback { get; set; }

    /// <summary>
    /// Gets or sets flag that determines whether payload data is compressed, defaults to TSSC.
    /// </summary>
    public bool CompressPayloadData { get; set; } = Default.CompressPayloadData;

    /// <summary>
    /// Gets or sets flag that determines whether the metadata transfer is compressed, defaults to GZip.
    /// </summary>
    public bool CompressMetadata { get; set; } = Default.CompressMetadata;

    /// <summary>
    /// Gets or sets flag that determines whether the signal index cache is compressed, defaults to GZip.
    /// </summary>
    public bool CompressSignalIndexCache { get; set; } = Default.CompressSignalIndexCache;

    /// <summary>
    /// Gets or sets the STTP protocol version used by this library.
    /// </summary>
    public byte Version { get; set; } = Default.Version;

    /// <summary>
    /// Gets or sets the STTP library API title as identification information of <see cref="DataSubscriber"/> to a <see cref="DataPublisher"/>.
    /// </summary>
    public string STTPSourceInfo { get; set; } = sttp.Version.STTPSource;

    /// <summary>
    /// Gets or sets the STTP library API version as identification information of <see cref="DataSubscriber"/> to a <see cref="DataPublisher"/>.
    /// </summary>
    public string STTPVersionInfo { get; set; } = sttp.Version.STTPVersion;

    /// <summary>
    /// Gets or sets when the STTP library API was last updated as identification information of <see cref="DataSubscriber"/> to a <see cref="DataPublisher"/>.
    /// </summary>
    public string STTPUpdatedOnInfo { get; set; } = sttp.Version.STTPUpdatedOn;

    /// <summary>
    /// Gets the metadata cache associated with this <see cref="DataSubscriber"/>.
    /// </summary>
    public MetadataCache MetadataCache { get; private set; } = new();

    /// <summary>
    /// Cleanly shuts down a <see cref="DataSubscriber"/> that is no longer being used, e.g., during a normal application exit.
    /// </summary>
    public void Dispose()
    {
        m_disposing = true;
        m_connector.Dispose();

        // disconnect(true, false);
    }

    /// <summary>
    /// Gets flag that determines if a <see cref="DataSubscriber"/> is currently connected to a <see cref="DataPublisher"/>.
    /// </summary>
    public bool Connected => m_connected;

    /// <summary>
    /// Gets flag that determines if a <see cref="DataSubscriber"/> is currently subscribed to a data stream.
    /// </summary>
    public bool Subscribed => m_subscribed;

    /// <summary>
    /// Gets flag that determines if <see cref="DataSubscriber"/> is being disposed.
    /// </summary>
    public bool Disposing => m_disposing;

    /// <summary>
    /// Encodes an STTP string according to the defined operational modes.
    /// </summary>
    /// <param name="data">String to encode.</param>
    /// <returns>Byte array containing encoding of the specified string.</returns>
    /// <exception cref="InvalidOperationException">.NET implementation of STTP only supports UTF-8 string encoding.</exception>
    public byte[] EncodeString(string data)
    {
        // Latest version of STTP only supports UTF-8:
        if (m_encoding != OperationalEncoding.UTF8)
            throw new InvalidOperationException(".NET implementation of STTP only supports UTF-8 string encoding");

        return Encoding.UTF8.GetBytes(data);
    }

    /// <summary>
    /// Decodes an STTP string according to the defined operational modes.
    /// </summary>
    /// <param name="data">Byte array to decode.</param>
    /// <returns>String containing decoding of the specified byte array.</returns>
    /// <exception cref="InvalidOperationException">.NET implementation of STTP only supports UTF-8 string encoding.</exception>
    public string DecodeString(byte[] data)
    {
        // Latest version of STTP only supports UTF-8:
        if (m_encoding != OperationalEncoding.UTF8)
            throw new InvalidOperationException(".NET implementation of STTP only supports UTF-8 string encoding");

        return Encoding.UTF8.GetString(data);
    }

    /// <summary>
    /// Gets the <see cref="MeasurementRecord"/> for the specified <paramref name="signalID"/> from the local registry.
    /// If the metadata does not exist, a new record is created and returned.
    /// </summary>
    /// <param name="signalID">Signal ID to lookup.</param>
    /// <returns><see cref="MeasurementRecord"/> for specified <paramref name="signalID"/>.</returns>
    public MeasurementRecord LookupMetadata(Guid signalID)
    {
        MeasurementRecord? record = MetadataCache.FindMeasurement(signalID);

        if (record is not null)
            return record;

        record = new() { SignalID = signalID };
        MetadataCache.AddMeasurementRecord(record);
        return record;
    }

    /// <summary>
    /// Gets the <see cref="Measurement.Value"/> of the specified <paramref name="measurement"/> with any linear adjustments applied from
    /// the measurement's <see cref="MeasurementRecord.Adder"/> and <see cref="MeasurementRecord.Multiplier"/> metadata, if found.
    /// </summary>
    /// <param name="measurement">Measurement for which to get linear adjutsment value.</param>
    /// <returns><see cref="Measurement.Value"/> of the specified <paramref name="measurement"/> with any linear adjustment applied.</returns>
    public double AdjustedValue(Measurement measurement)
    {
        MeasurementRecord? record = MetadataCache.FindMeasurement(measurement.SignalID);

        if (record is not null)
            return measurement.Value * record.Multiplier + record.Adder;

        return measurement.Value;
    }

    /// <summary>
    /// Requests the the <see cref="DataSubscriber"/> initiate a connection to the <see cref="DataPublisher"/>.
    /// </summary>
    /// <param name="hostname">DNS name or IP of data publisher.</param>
    /// <param name="port">Listening port of data publisher TCP command channel.</param>
    /// <returns>Exception details of failed connection attempt; otherwise, <c>null</c> if connection was succesful.</returns>
    public Exception? Connect(string hostname, ushort port) =>
        Connect(hostname, port, false);

    private Exception? Connect(string hostname, ushort port, bool autoReconnecting)
    {
        if (m_connected)
            return new InvalidOperationException("subscriber is already connected; disconnect first");

        Thread? disconnectThread;

        lock (m_disconnectThreadMutex)
            disconnectThread = m_disconnectThread;

        if (disconnectThread is not null && disconnectThread.ThreadState == ThreadState.Running)
            disconnectThread.Join();

        Exception? err = null;

        // Let any pending connect or disconnect operation complete before new connect,
        // this prevents destruction disconnect before connection is completed
        try
        {
            lock (m_connectActionMutex)
            {
                m_disconnected = false;
                m_subscribed = false;

                m_totalCommandChannelBytesReceived = 0L;
                m_totalDataChannelBytesReceived = 0L;
                m_totalMeasurementsReceived = 0L;

                m_keyIVs = null;
                m_bufferBlockExpectedSequenceNumber = 0U;
                MetadataCache = new();

                if (!autoReconnecting)
                    m_connector.ResetConnection();

                m_connector.ConnectionRefused = false;

                // TODO: Add TLS implementation options
                // TODO: Add reverse (server-based) connection options, see:
                // https://sttp.info/reverse-connections/
                m_commandChannelSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                m_commandChannelSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, 1);
                m_commandChannelSocket.Connect(hostname, port);
            }
        }
        catch (Exception ex)
        {
            err = ex;
        }

        if (err is null)
        {
            m_commandChannelResponseThread = new(RunCommandChannelResponseThread) { Name = "CmdChannelThread" };

            m_connected = true;
            m_lastMissingCacheWarning = 0L;

            m_commandChannelResponseThread.Start();
            SendOperationalModes();
        }

        return err;
    }

    /// <summary>
    /// Notifies the <see cref="DataPublisher"/> that a <see cref="DataSubscriber"/> would like to start receiving streaming data.
    /// </summary>
    /// <returns>Exception details of failed subscribe attempt; otherwise, <c>null</c> if subscribe was succesful.</returns>
    public Exception? Subscribe()
    {
        if (!m_connected)
            return new InvalidOperationException("subscriber is not connected; cannot subscribe");

        m_totalDataChannelBytesReceived = 0L;

        SubscriptionInfo subscription = m_subscriptionInfo;
        Dictionary<string, string> parameters = new(StringComparer.OrdinalIgnoreCase)
        {
            ["throttled"] = $"{subscription.Throttled}",
            ["publishInterval"] = $"{subscription.PublishInterval:N6}",
            ["includeTime"] = $"{subscription.IncludeTime}",
            ["processingInterval"] = $"{subscription.ProcessingInterval}",
            ["useMillisecondResolution"] = $"{subscription.UseMillisecondResolution}",
            ["requestNaNValueFilter"] = $"{subscription.RequestNANValueFilter}"
        };

        Dictionary<string, string> assemblyInfo = new(StringComparer.OrdinalIgnoreCase)
        {
            ["source"] = STTPSourceInfo,
            ["version"] = STTPVersionInfo,
            ["updatedOn"] = STTPUpdatedOnInfo
        };

        parameters["assemblyInfo"] = assemblyInfo.JoinKeyValuePairs();

        if (!string.IsNullOrWhiteSpace(subscription.FilterExpression))
            parameters["filterExpression"] = subscription.FilterExpression;

        if (subscription.EnableUDPDataChannel)
        {
            ushort udpPort = subscription.DataChannelLocalPort;

            try
            {
                m_dataChannelSocket = new Socket(SocketType.Dgram, ProtocolType.Udp);
                
                IPAddress dataChannelIP = string.IsNullOrWhiteSpace(subscription.DataChannelInterface) ?
                    IPAddress.Any : 
                    IPAddress.Parse(subscription.DataChannelInterface);

                m_dataChannelSocket.Bind(new IPEndPoint(dataChannelIP, udpPort));
            }
            catch (Exception ex)
            {
                return new InvalidOperationException($"failed to open UDP socket for port {udpPort}: {ex.Message}", ex);
            }

            m_dataChannelResponseThread = new(RunDataChannelResponseThread) { Name = "DataChannelThread" };
            m_dataChannelResponseThread.Start();

            parameters["dataChannel"] = $"dataChannel={{localport={udpPort}}}";
        }

        if (!string.IsNullOrWhiteSpace(subscription.StartTime))
            parameters["startTimeConstraint"] = subscription.StartTime;

        if (!string.IsNullOrWhiteSpace(subscription.StopTime))
            parameters["stopTimeConstraint"] = subscription.StopTime;

        if (!string.IsNullOrWhiteSpace(subscription.ContraintParameters))
            parameters["timeConstraintParameters"] = subscription.ContraintParameters;

        string parameterString = parameters.JoinKeyValuePairs();

        if (!string.IsNullOrWhiteSpace(subscription.ExtraConnectionStringParameters))
            parameterString += $";{subscription.ExtraConnectionStringParameters}";

        byte[] parameterExpression = EncodeString(parameterString);
        int length = parameterExpression.Length;
        byte[] buffer = new byte[5 + length];

        buffer[0] = (byte)DataPacketFlags.Compact;
        Buffer.BlockCopy(BigEndian.GetBytes((uint)length), 0, buffer, 1, 4);
        Buffer.BlockCopy(parameterExpression, 0, buffer, 5, length);

        SendServerCommand(ServerCommand.Subscribe, buffer);

        // Reset TSSC decompressor on successful (re)subscription
        lock (m_tsscOOSReportMutex)
            m_tsscOOSReport = DateTime.UtcNow.Ticks;

        m_tsscResetRequested = true;

        return null;
    }

    /// <summary>
    /// Notifies the <see cref="DataPublisher"/> that a <see cref="DataSubscriber"/> would like to stop receiving streaming data.
    /// </summary>
    public void Unsubscribe()
    {
        if (!m_connected)
            return;

        SendServerCommand(ServerCommand.Unsubscribe);

        m_disconnecting = true;

        if (m_dataChannelSocket is not null)
        {
            try
            {
                m_dataChannelSocket.Shutdown(SocketShutdown.Both);
                m_dataChannelSocket.Close();
            }
            catch (Exception ex)
            {
            }
        }
    }

    /// <summary>
    /// Initiates a <see cref="DataSubscriber"/> disconnect sequence.
    /// </summary>
    public void Disconnect()
    {
        if (m_disconnecting)
            return;

        // Disconnect method executes shutdown on a separate thread without stopping to prevent
        // issues where user may call disconnect method from a dispatched event thread. Also,
        // user requests to disconnect are not an auto-reconnect attempt
        Disconnect(false, false);
    }

    private void Disconnect(bool joinThread, bool autoReconnecting)
    {

    }

    private void RunDisconnectThread()
    {

    }

    private void DispatchConnectionTerminated()
    {

    }

    private void HandleConnectionTerminated()
    {

    }

    private void DispatchStatusMessage(string message)
    {

    }

    private void DispatchErrorMessage(string message)
    {

    }

    private void RunCommandChannelResponseThread()
    {

    }

    private void RunDataChannelResponseThread()
    {

    }

    private void ProcessServerResponse(byte[] buffer)
    {
        if (m_disconnecting)
            return;
    }

    private void HandleSucceeded(ServerCommand commandCode, byte[] data)
    {

    }

    private void HandledFailed(ServerCommand commandCode, byte[] data)
    {

    }

    private void HandleMetadataRefresh(byte[] data)
    {

    }

    private void HandleDataStartTime(byte[] data)
    {

    }

    private void HandleProcessingComplete(byte[] data)
    {

    }

    private void HandleUpdateSignalIndexCache(byte[] data)
    {

    }

    private void HandleUpdateBaseTimes(byte[] data)
    {

    }

    private void HandleUpdateCipherKeys(byte[] data)
    {

    }

    private void HandleConfigurationChanged()
    {

    }

    private void HandleDataPacket(byte[] data)
    {

    }

    private (Measurement[]?, Exception?) ParseTSSCMeasurements(SignalIndexCache signalIndexCache, byte[] data, uint count)
    {
        return (null, null);
    }
    private (Measurement[]?, Exception?) ParseCompactMeasurements(SignalIndexCache signalIndexCache, byte[] data, uint count)
    {
        return (null, null);
    }

    private void HandleBufferBlock(byte[] data)
    {

    }

    private void HandleNotification(byte[] data)
    {

    }

    private void SendServerCommand(ServerCommand commandCode, string message) =>
        SendServerCommand(commandCode, EncodeString(message));

    private void SendServerCommand(ServerCommand commandCode, byte[]? data = null)
    {

    }

    private void SendOperationalModes()
    {
        uint operationalModes = (uint)CompressionModes.GZip;
        operationalModes |= (uint)OperationalModes.VersionMask & Version;
        operationalModes |= (uint)m_encoding;

        // TSSC compression only works with stateful connections
        if (CompressPayloadData && !m_subscriptionInfo.EnableUDPDataChannel)
            operationalModes |= (uint)OperationalModes.CompressPayloadData | (uint)CompressionModes.TSSC;

        if (CompressMetadata)
            operationalModes |= (uint)OperationalModes.CompressMetadata;

        if (CompressSignalIndexCache)
            operationalModes |= (uint)OperationalModes.CompressSignalIndexCache;

        SendServerCommand(ServerCommand.DefineOperationalModes, BigEndian.GetBytes(operationalModes));
    }

    /// <summary>
    /// Gets the <see cref="SubscriptionInfo"/> associated with this <see cref="DataSubscriber"/>.
    /// </summary>
    public SubscriptionInfo Subscription => m_subscriptionInfo;

    /// <summary>
    /// Gets the <see cref="SubscriberConnector"/> associated with this <see cref="DataSubscriber"/>.
    /// </summary>
    public SubscriberConnector Connector => m_connector;

    /// <summary>
    /// Gets the active signal index cache.
    /// </summary>
    public SignalIndexCache ActiveSignalIndexCache
    {
        get
        {
            SignalIndexCache signalIndexCache;

            lock (m_signalIndexCacheMutex)
                signalIndexCache = m_signalIndexCache[m_cacheIndex];

            return signalIndexCache;
        }
    }

    /// <summary>
    /// Gets the subscriber ID as assigned by the <see cref="DataPublisher"/> upon receipt of the <see cref="SignalIndexCache"/>.
    /// </summary>
    public Guid SubscriberID => m_subscriberID;

    /// <summary>
    /// Gets the total number of bytes received via the command channel since last connection.
    /// </summary>
    public long TotalCommandChannelBytesReceived => m_totalCommandChannelBytesReceived;

    /// <summary>
    /// Gets the total number of bytes received via the data channel since last connection.
    /// </summary>
    public long TotalDataChannelBytesReceived => m_totalDataChannelBytesReceived;

    /// <summary>
    /// Gets the total number of measurements received since last subscription.
    /// </summary>
    public long TotalMeasurementsReceived => m_totalMeasurementsReceived;
}
