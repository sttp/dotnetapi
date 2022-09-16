//******************************************************************************************************
//  Constants.cs - Gbtc
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

namespace sttp.transport;

/// <summary>
/// Measurement flag for a defaults for STTP settings.
/// </summary>
public static class Default
{
    /// <summary>
    /// Default for maximum number of retries for a connection attempt.
    /// </summary>
    public const int MaxRetries = -1;

    /// <summary>
    /// Default for retry interval, in seconds.
    /// </summary>
    public const double RetryInterval = 1.0D;

    /// <summary>
    /// Default for maximum retry interval, in seconds.
    /// </summary>
    public const double MaxRetryInterval = 30.0D;

    /// <summary>
    /// Default for auto-reconnect flag.
    /// </summary>
    public const bool AutoReconnect = true;

    /// <summary>
    /// Default for auto-request metadata flag.
    /// </summary>
    public const bool AutoRequestMetadata = true;

    /// <summary>
    /// Default for the auto-subscribe flag.
    /// </summary>
    public const bool AutoSubscribe = true;

    /// <summary>
    /// Default to the compress payload data flag.
    /// </summary>
    public const bool CompressPayloadData = true;

    /// <summary>
    /// Default for the compress metadata flag.
    /// </summary>
    public const bool CompressMetadata = true;

    /// <summary>
    /// Default  for the compress signal index cache flag.
    /// </summary>
    public const bool CompressSignalIndexCache = true;

    /// <summary>
    /// Default for metadata filters
    /// </summary>
    public const string MetadataFilters = "";

    /// <summary>
    /// Default for STTP protocol version.
    /// </summary>
    public const byte Version = 2;

    /// <summary>
    /// Default for filter expression.
    /// </summary>
    public const string FilterExpression = "";

    /// <summary>
    /// Default for throttled flag.
    /// </summary>
    public const bool Throttled = false;

    /// <summary>
    /// Default for publish interval, in seconds.
    /// </summary>
    public const double PublishInterval = 1.0D;

    /// <summary>
    /// Default for enable UDP data channel flag.
    /// </summary>
    public const bool EnableUDPDataChannel = false;

    /// <summary>
    /// Default for local UDP port for data channel.
    /// </summary>
    public const ushort UDPDataChannelPort = 0;

    /// <summary>
    /// Default for interface for UPD data channel.
    /// </summary>
    public const string UDPDataChannelInterface = "";

    /// <summary>
    /// Default for include time flag.
    /// </summary>
    public const bool IncludeTime = true;

    /// <summary>
    /// Default for use millisecond resolution flag.
    /// </summary>
    public const bool UseMillisecondResolution = true;

    /// <summary>
    /// Default for request NAN-value filter flag.
    /// </summary>
    public const bool RequestNaNValueFilter = true;

    /// <summary>
    /// Default for start time.
    /// </summary>
    public const string StartTime = "";

    /// <summary>
    /// Default for stop time.
    /// </summary>
    public const string StopTime = "";

    /// <summary>
    /// Default for constraint parameters.
    /// </summary>
    public const string ConstraintParameters = "";

    /// <summary>
    /// Default  for processing interval, in seconds.
    /// </summary>
    public const int ProcessingInterval = -1;

    /// <summary>
    /// Default for extra connection string parameters.
    /// </summary>
    public const string ExtraConnectionStringParameters = "";
}

/// <summary>
/// Enumeration of the possible quality states of a <see cref="Measurement"/> value.
/// </summary>
[Flags]
public enum StateFlags : uint
{
    /// <summary>
    /// Measurement flag for a normal state.
    /// </summary>
    Normal = (uint)Bits.Nil,
    /// <summary>
    /// Measurement flag for a bad data state.
    /// </summary>
    BadData = (uint)Bits.Bit00,
    /// <summary>
    /// Measurement flag for a suspect data state.
    /// </summary>
    SuspectData = (uint)Bits.Bit01,
    /// <summary>
    /// Measurement flag for an over range error, i.e., unreasonable high value.
    /// </summary>
    OverRangeError = (uint)Bits.Bit02,
    /// <summary>
    /// Measurement flag for an under range error, i.e., unreasonable low value.
    /// </summary>
    UnderRangeError = (uint)Bits.Bit03,
    /// <summary>
    /// Measurement flag for a alarm for high value.
    /// </summary>
    AlarmHigh = (uint)Bits.Bit04,
    /// <summary>
    /// Measurement flag for an alarm for low value.
    /// </summary>
    AlarmLow = (uint)Bits.Bit05,
    /// <summary>
    /// Measurement flag for a warning for high value.
    /// </summary>
    WarningHigh = (uint)Bits.Bit06,
    /// <summary>
    /// Measurement flag for a warning for low value.
    /// </summary>
    WarningLow = (uint)Bits.Bit07,
    /// <summary>
    /// Measurement flag for an alarm for flat-lined value, i.e., latched value test alarm.
    /// </summary>
    FlatlineAlarm = (uint)Bits.Bit08,
    /// <summary>
    /// Measurement flag for a comparison alarm, i.e., outside threshold of comparison with a real-time value.
    /// </summary>
    ComparisonAlarm = (uint)Bits.Bit09,
    /// <summary>
    /// Measurement flag for a rate-of-change alarm.
    /// </summary>
    ROCAlarm = (uint)Bits.Bit10,
    /// <summary>
    /// Measurement flag for a bad value received.
    /// </summary>
    ReceivedAsBad = (uint)Bits.Bit11,
    /// <summary>
    /// Measurement flag for a calculated value state.
    /// </summary>
    CalculatedValue = (uint)Bits.Bit12,
    /// <summary>
    /// Measurement flag for a calculation error with the value.
    /// </summary>
    CalculationError = (uint)Bits.Bit13,
    /// <summary>
    /// Measurement flag for a calculation warning with the value.
    /// </summary>
    CalculationWarning = (uint)Bits.Bit14,
    /// <summary>
    /// Measurement flag for a reserved quality flag.
    /// </summary>
    ReservedQualityFlag = (uint)Bits.Bit15,
    /// <summary>
    /// Measurement flag for a bad time state.
    /// </summary>
    BadTime = (uint)Bits.Bit16,
    /// <summary>
    /// Measurement flag for a suspect time state.
    /// </summary>
    SuspectTime = (uint)Bits.Bit17,
    /// <summary>
    /// Measurement flag for a late time alarm.
    /// </summary>
    LateTimeAlarm = (uint)Bits.Bit18,
    /// <summary>
    /// Measurement flag for a future time alarm.
    /// </summary>
    FutureTimeAlarm = (uint)Bits.Bit19,
    /// <summary>
    /// Measurement flag for an up-sampled state.
    /// </summary>
    UpSampled = (uint)Bits.Bit20,
    /// <summary>
    /// Measurement flag for a down-sampled state.
    /// </summary>
    DownSampled = (uint)Bits.Bit21,
    /// <summary>
    /// Measurement flag for a discarded value state.
    /// </summary>
    DiscardedValue = (uint)Bits.Bit22,
    /// <summary>
    /// Measurement flag for a reserved time flag.
    /// </summary>
    ReservedTimeFlag = (uint)Bits.Bit23,
    /// <summary>
    /// Measurement flag for a user defined flag 1.
    /// </summary>
    UserDefinedFlag1 = (uint)Bits.Bit24,
    /// <summary>
    /// Measurement flag for a user defined flag 2.
    /// </summary>
    UserDefinedFlag2 = (uint)Bits.Bit25,
    /// <summary>
    /// Measurement flag for a user defined flag 3.
    /// </summary>
    UserDefinedFlag3 = (uint)Bits.Bit26,
    /// <summary>
    /// Measurement flag for a user defined flag 4.
    /// </summary>
    UserDefinedFlag4 = (uint)Bits.Bit27,
    /// <summary>
    /// Measurement flag for a user defined flag 5.
    /// </summary>
    UserDefinedFlag5 = (uint)Bits.Bit28,
    /// <summary>
    /// Measurement flag for a system error state.
    /// </summary>
    SystemError = (uint)Bits.Bit29,
    /// <summary>
    /// Measurement flag for a system warning state.
    /// </summary>
    SystemWarning = (uint)Bits.Bit30,
    /// <summary>
    /// Measurement flag for an error state.
    /// </summary>
    MeasurementError = (uint)Bits.Bit31
}

/// <summary>
/// Enumeration of the possible flags for a data packet.
/// </summary>
[Flags]
public enum DataPacketFlags : byte
{
    /// <summary>
    /// Determines if serialized measurement is compact. Currently this bit is always set.
    /// </summary>
    [Obsolete("Bit will be removed in future version.")]
    Compact = (byte)Bits.Bit01,
    /// <summary>
    /// Determines which cipher index to use when encrypting data packet.
    /// </summary>
    CipherIndex = (byte)Bits.Bit02,
    /// <summary>
    /// Determines if data packet payload is compressed.
    /// </summary>
    /// <remarks>
    /// Bit set = payload compressed, bit clear = payload normal.
    /// </remarks>
    Compressed = (byte)Bits.Bit03,
    /// <summary>
    /// Determines which signal index cache to use when decoding a data packet. Used by STTP version 2 or greater.
    /// </summary>
    /// <remarks>
    /// Bit set = use odd cache index (i.e., 1), bit clear = use even cache index (i.e., 0).
    /// </remarks>
    CacheIndex = (byte)Bits.Bit04,
    /// <summary>
    /// Defines state where there are no flags set.
    /// </summary>
    NoFlags = (byte)Bits.Nil
}

/// <summary>
/// Enumeration of the possible server commands received by <see cref="DataPublisher"/> and sent by <see cref="DataSubscriber"/>
/// during an STTP session.
/// </summary>
/// <remarks>
/// Solicited server commands will receive a <see cref="ServerResponse.Succeeded"/> or <see cref="ServerResponse.Failed"/>
/// response code along with an associated success or failure message. Message type for successful responses will be based
/// on server command - for example, server response for a successful MetaDataRefresh command will return a serialized
/// <see cref="DataSet"/> of the available server metadata. Message type for failed responses will always be a string of
/// text representing the error message.
/// </remarks>
public enum ServerCommand : byte
{
    // Although the server commands and responses will be on two different paths, the response enumeration values
    // are defined as distinct from the command values to make it easier to identify codes from a wire analysis.

    /// <summary>
    /// Command code for handling connect operations.
    /// </summary>
    /// <remarks>
    /// Only used as part of connection refused response -- value not sent on the wire.
    /// </remarks>
    Connect = 0x00,

    /// <summary>
    /// Command code for requesting an updated set of metadata.
    /// </summary>
    /// <remarks>
    /// Successful return message type will be a <see cref="DataSet"/> containing server device and measurement metadata.
    /// Devices and measurements contain unique Guids that should be used to key metadata updates in local repository.
    /// Optional string based message can follow command that should represent client requested meta-data filtering
    /// expressions, e.g.: "FILTER MeasurementDetail WHERE SignalType &lt;&gt; 'STAT'"
    /// </remarks>
    MetaDataRefresh = 0x01,

    /// <summary>
    /// Command code for requesting a subscription of streaming data from server based on connection string that follows.
    /// </summary>
    /// <remarks>
    /// It will not be necessary to stop an existing subscription before requesting a new one.
    /// Successful return message type will be string indicating total number of allowed points.
    /// Client should wait for UpdateSignalIndexCache and UpdateBaseTime response codes before attempting
    /// to parse data when using the compact measurement format.
    /// </remarks>
    Subscribe = 0x02,

    /// <summary>
    /// Command code for requesting that server stop sending streaming data to the client and cancel the current subscription.
    /// </summary>
    Unsubscribe = 0x03,

    /// <summary>
    /// Command code for manually requesting that server send a new set of cipher keys for data packet encryption (UDP only).
    /// </summary>
    RotateCipherKeys = 0x04,

    /// <summary>
    /// Command code for manually requesting that server to update the processing interval with the following specified value.
    /// </summary>
    UpdateProcessingInterval = 0x05,

    /// <summary>
    /// Command code for establishing operational modes.
    /// </summary>
    /// <remarks>
    /// As soon as connection is established, requests that server set operational modes that affect how the subscriber and
    /// publisher will communicate.
    /// </remarks>
    DefineOperationalModes = 0x06,

    /// <summary>
    /// Command code for receipt of a notification.
    /// </summary>
    /// <remarks>
    /// This message is sent in response to <see cref="ServerResponse.Notify"/>.
    /// </remarks>
    ConfirmNotification = 0x07,

    /// <summary>
    /// Command code for receipt of a buffer block measurement.
    /// </summary>
    /// <remarks>
    /// This message is sent in response to <see cref="ServerResponse.BufferBlock"/>.
    /// </remarks>
    ConfirmBufferBlock = 0x08,

    /// <summary>
    /// Command code for receipt of a base time update.
    /// </summary>
    /// <remarks>
    /// This message is sent in response to <see cref="ServerResponse.UpdateBaseTimes"/>.
    /// </remarks>
    ConfirmUpdateBaseTimes = 0x09,

    /// <summary>
    /// Command code for confirming the receipt of a signal index cache.
    /// </summary>
    /// <remarks>
    /// This allows publisher to safely transition to next signal index cache.
    /// </remarks>
    ConfirmSignalIndexCache = 0x0A,

    /// <summary>
    /// Command code for requesting the primary metadata schema.
    /// </summary>
    GetPrimaryMetadataSchema = 0x0B,

    /// <summary>
    /// Command code for requesting the signal selection schema.
    /// </summary>
    GetSignalSelectionSchema = 0x0C,

    /// <summary>
    /// Command code for handling user-defined commands.
    /// </summary>
    UserCommand00 = 0xD0,

    /// <summary>
    /// Command code for handling user-defined commands.
    /// </summary>
    UserCommand01 = 0xD1,

    /// <summary>
    /// Command code for handling user-defined commands.
    /// </summary>
    UserCommand02 = 0xD2,

    /// <summary>
    /// Command code for handling user-defined commands.
    /// </summary>
    UserCommand03 = 0xD3,

    /// <summary>
    /// Command code for handling user-defined commands.
    /// </summary>
    UserCommand04 = 0xD4,

    /// <summary>
    /// Command code for handling user-defined commands.
    /// </summary>
    UserCommand05 = 0xD5,

    /// <summary>
    /// Command code for handling user-defined commands.
    /// </summary>
    UserCommand06 = 0xD6,

    /// <summary>
    /// Command code for handling user-defined commands.
    /// </summary>
    UserCommand07 = 0xD7,

    /// <summary>
    /// Command code for handling user-defined commands.
    /// </summary>
    UserCommand08 = 0xD8,

    /// <summary>
    /// Command code for handling user-defined commands.
    /// </summary>
    UserCommand09 = 0xD9,

    /// <summary>
    /// Command code for handling user-defined commands.
    /// </summary>
    UserCommand10 = 0xDA,

    /// <summary>
    /// Command code for handling user-defined commands.
    /// </summary>
    UserCommand11 = 0xDB,

    /// <summary>
    /// Command code for handling user-defined commands.
    /// </summary>
    UserCommand12 = 0xDC,

    /// <summary>
    /// Command code for handling user-defined commands.
    /// </summary>
    UserCommand13 = 0xDD,

    /// <summary>
    /// Command code for handling user-defined commands.
    /// </summary>
    UserCommand14 = 0xDE,

    /// <summary>
    /// Command code for handling user-defined commands.
    /// </summary>
    UserCommand15 = 0xDF
}

/// <summary>
/// Enumeration of the possible server responses sent by <see cref="DataPublisher"/> and received by <see cref="DataSubscriber"/>
/// during an STTP session.
/// </summary>
public enum ServerResponse : byte
{
    // Although the server commands and responses will be on two different paths, the response enumeration values
    // are defined as distinct from the command values to make it easier to identify codes from a wire analysis.

    /// <summary>
    /// Response code indicating a succeeded response.
    /// </summary>
    /// <remarks>
    /// Informs client that its solicited server command succeeded, original command and success message follow.
    /// </remarks>
    Succeeded = 0x80,

    /// <summary>
    /// Response code indicating a failed response.
    /// </summary>
    /// <remarks>
    /// Informs client that its solicited server command failed, original command and failure message follow.
    /// </remarks>
    Failed = 0x81,

    /// <summary>
    /// Response code indicating a data packet.
    /// </summary>
    /// <remarks>
    /// Unsolicited response informs client that a data packet follows.
    /// </remarks>
    DataPacket = 0x82,

    /// <summary>
    /// Response code indicating a signal index cache update.
    /// </summary>
    /// <remarks>
    /// Unsolicited response requests that client update its runtime signal index cache with the one that follows.
    /// </remarks>
    UpdateSignalIndexCache = 0x83,

    /// <summary>
    /// Response code indicating a runtime base-timestamp offsets have been updated.
    /// </summary>
    /// <remarks>
    /// Unsolicited response requests that client update its runtime base-timestamp offsets with those that follow.
    /// </remarks>
    UpdateBaseTimes = 0x84,

    /// <summary>
    /// Response code indicating a runtime cipher keys have been updated.
    /// </summary>
    /// <remarks>
    /// Response, solicited or unsolicited, requests that client update its runtime data cipher keys with those that follow.
    /// </remarks>
    UpdateCipherKeys = 0x85,

    /// <summary>
    /// Response code indicating the start time of data being published.
    /// </summary>
    /// <remarks>
    /// Unsolicited response provides the start time of data being processed from the first measurement.
    /// </remarks>
    DataStartTime = 0x86,

    /// <summary>
    /// Response code indicating that processing has completed.
    /// </summary>
    /// <remarks>
    /// Unsolicited response provides notification that input processing has completed, typically via temporal constraint.
    /// </remarks>
    ProcessingComplete = 0x87,

    /// <summary>
    /// Response code indicating a buffer block.
    /// </summary>
    /// <remarks>
    /// Unsolicited response informs client that a raw buffer block follows.
    /// </remarks>
    BufferBlock = 0x88,

    /// <summary>
    /// Response code indicating a notification.
    /// </summary>
    /// <remarks>
    /// Unsolicited response provides a notification message to the client.
    /// </remarks>
    Notify = 0x89,

    /// <summary>
    /// Response code indicating a that the publisher configuration metadata has changed.
    /// </summary>
    /// <remarks>
    /// Unsolicited response provides a notification that the publisher's source configuration has changed and that
    /// client may want to request a meta-data refresh.
    /// </remarks>
    ConfigurationChanged = 0x8A,

    /// <summary>
    /// Response code handling user-defined responses.
    /// </summary>
    UserResponse00 = 0xE0,

    /// <summary>
    /// Response code handling user-defined responses.
    /// </summary>
    UserResponse01 = 0xE1,

    /// <summary>
    /// Response code handling user-defined responses.
    /// </summary>
    UserResponse02 = 0xE2,

    /// <summary>
    /// Response code handling user-defined responses.
    /// </summary>
    UserResponse03 = 0xE3,

    /// <summary>
    /// Response code handling user-defined responses.
    /// </summary>
    UserResponse04 = 0xE4,

    /// <summary>
    /// Response code handling user-defined responses.
    /// </summary>
    UserResponse05 = 0xE5,

    /// <summary>
    /// Response code handling user-defined responses.
    /// </summary>
    UserResponse06 = 0xE6,

    /// <summary>
    /// Response code handling user-defined responses.
    /// </summary>
    UserResponse07 = 0xE7,

    /// <summary>
    /// Response code handling user-defined responses.
    /// </summary>
    UserResponse08 = 0xE8,

    /// <summary>
    /// Response code handling user-defined responses.
    /// </summary>
    UserResponse09 = 0xE9,

    /// <summary>
    /// Response code handling user-defined responses.
    /// </summary>
    UserResponse10 = 0xEA,

    /// <summary>
    /// Response code handling user-defined responses.
    /// </summary>
    UserResponse11 = 0xEB,

    /// <summary>
    /// Response code handling user-defined responses.
    /// </summary>
    UserResponse12 = 0xEC,

    /// <summary>
    /// Response code handling user-defined responses.
    /// </summary>
    UserResponse13 = 0xED,

    /// <summary>
    /// Response code handling user-defined responses.
    /// </summary>
    UserResponse14 = 0xEE,

    /// <summary>
    /// Response code handling user-defined responses.
    /// </summary>
    UserResponse15 = 0xEF,

    /// <summary>
    /// Response code indicating a null-operation keep-alive ping.
    /// </summary>
    /// <remarks>
    /// The command channel can remain quiet for some time, this command allows a period test of client connectivity.
    /// </remarks>
    NoOP = 0xFF
}

/// <summary>
/// Enumeration of the possible modes that affect how <see cref="DataPublisher"/> and <see cref="DataSubscriber"/>
/// communicate during as STTP session.
/// </summary>
/// <remarks>
/// Operational modes are sent from a subscriber to a publisher to request operational behaviors for the
/// connection, as a result the operation modes must be sent before any other command. The publisher may
/// silently refuse some requests (e.g., compression) based on its configuration. Operational modes only
/// apply to fundamental protocol control.
/// </remarks>
[Flags]
public enum OperationalModes : uint
{
    /// <summary>
    /// Bit mask used to get version number of protocol.
    /// </summary>
    /// <remarks>
    /// Version number is currently set to 2.
    /// </remarks>
    VersionMask = 0x000000FF,
    /// <summary>
    ///Bit mask used to get character encoding used when exchanging messages between publisher and subscriber.
    /// </summary>
    /// <remarks>
    /// STTP currently only supports UTF-8 string encoding.
    /// </remarks>
    EncodingMask = 0x00000300,
    /// <summary>
    /// Bit mask used to apply an implementation-specific extension to STTP.
    /// </summary>
    /// <remarks>
    /// If the value is zero, no implementation specific extensions are applied.
    /// If the value is non-zero, an implementation specific extension is applied, and all parties need to coordinate and agree to the extension.
    /// If extended flags are unsupported, returned failure message text should be prefixed with UNSUPPORTED EXTENSION: as the context reference.
    /// </remarks>
    ImplementationSpecificExtensionMask = 0x00FF0000,
    /// <summary>
    /// Bit flag used to determine whether external measurements are exchanged during metadata synchronization.
    /// </summary>
    /// <remarks>
    /// Bit set = external measurements are exchanged, bit clear = no external measurements are exchanged
    /// </remarks>
    ReceiveExternalMetadata = 0x02000000,
    /// <summary>
    /// Bit flag used to determine whether internal measurements are exchanged during metadata synchronization.
    /// </summary>
    /// <remarks>
    /// Bit set = internal measurements are exchanged, bit clear = no internal measurements are exchanged
    /// </remarks>
    ReceiveInternalMetadata = 0x04000000,
    /// <summary>
    /// Bit flag used to determine whether payload data is compressed when exchanging between publisher and subscriber.
    /// </summary>
    /// <remarks>
    /// Bit set = compress, bit clear = no compression
    /// </remarks>
    CompressPayloadData = 0x20000000,
    /// <summary>
    /// Bit flag used to determine whether the signal index cache is compressed when exchanging between publisher and subscriber.
    /// </summary>
    /// <remarks>
    /// Bit set = compress, bit clear = no compression
    /// </remarks>
    CompressSignalIndexCache = 0x40000000,
    /// <summary>
    /// Bit flag used to determine whether metadata is compressed when exchanging between publisher and subscriber.
    /// </summary>
    /// <remarks>
    /// Bit set = compress, bit clear = no compression
    /// </remarks>
    CompressMetadata = 0x80000000,
    /// <summary>
    /// State where there are no flags set.
    /// </summary>
    NoFlags = 0x00000000
}


/// <summary>
/// Enumeration of the possible string encoding options of an STTP session.
/// </summary>
public enum OperationalEncoding : uint
{
    /// <summary>
    /// Targets little-endian 16-bit Unicode character encoding for strings.
    /// </summary>
    [Obsolete("STTP currently only supports UTF-8 string encoding.")]
    UTF16LE = 0x00000000,
    /// <summary>
    /// Targets big-endian 16-bit Unicode character encoding for strings.
    /// </summary>
    [Obsolete("STTP currently only supports UTF-8 string encoding.")]
    UTF16BE = 0x00000100,
    /// <summary>
    /// Targets 8-bit variable-width Unicode character encoding for strings.
    /// </summary>
    UTF8 = 0x00000200
}

/// <summary>
/// Enumeration of the possible compression modes supported by STTP.
/// </summary>
[Flags]
[Obsolete("Only used for backwards compatibility with pre-standard STTP implementations. OperationalModes now supports custom compression types")]
public enum CompressionModes : uint
{
    /// <summary>
    /// Bit flag used determine if GZip compression will be used to metadata exchange.
    /// </summary>
    GZip = (uint)Bits.Bit05,
    /// <summary>
    /// Bit flag used determine if the time-series special compression algorithm will be used for data exchange.
    /// </summary>
    TSSC = (uint)Bits.Bit06,
    /// <summary>
    /// Defines state where no compression will be used.
    /// </summary>
    None = (uint)Bits.Nil
}

/// <summary>
/// Enumeration of the possible security modes used by the <see cref="DataPublisher"/> to secure data
/// sent over the command channel in STTP.
/// </summary>
public enum SecurityMode
{
    /// <summary>
    /// Defines security mode where data will be sent over the wire unencrypted.
    /// </summary>
    Off,

    /// <summary>
    /// Defines security mode where data will be sent over wire using Transport Layer Security (TLS).
    /// </summary>
    TLS
}

/// <summary>
/// Enumeration of the possible connection status results used by the <see cref="SubscriberConnector"/>.
/// </summary>
public enum ConnectStatus
{
    /// <summary>
    /// Connection succeeded status.
    /// </summary>
    Success = 1,

    /// <summary>
    /// Connection failed status.
    /// </summary>
    Failed = 0,

    /// <summary>
    /// Connection cancelled status.
    /// </summary>
    Canceled = -1
}