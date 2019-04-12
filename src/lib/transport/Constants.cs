//******************************************************************************************************
//  Constants.cs - Gbtc
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
//       Generated original version of source code.
//
//******************************************************************************************************

using System;
using System.Data;

namespace sttp.transport
{
    /// <summary>
    /// Measurement state flags.
    /// </summary>
    [Flags]
    public enum MeasurementStateFlags : uint
    {
        /// <summary>
        /// Defines normal state.
        /// </summary>
        Normal = (uint)Bits.Nil,
        /// <summary>
        /// Defines bad data state.
        /// </summary>
        BadData = (uint)Bits.Bit00,
        /// <summary>
        /// Defines suspect data state.
        /// </summary>
        SuspectData = (uint)Bits.Bit01,
        /// <summary>
        /// Defines over range error, i.e., unreasonable high value.
        /// </summary>
        OverRangeError = (uint)Bits.Bit02,
        /// <summary>
        /// Defines under range error, i.e., unreasonable low value.
        /// </summary>
        UnderRangeError = (uint)Bits.Bit03,
        /// <summary>
        /// Defines alarm for high value.
        /// </summary>
        AlarmHigh = (uint)Bits.Bit04,
        /// <summary>
        /// Defines alarm for low value.
        /// </summary>
        AlarmLow = (uint)Bits.Bit05,
        /// <summary>
        /// Defines warning for high value.
        /// </summary>
        WarningHigh = (uint)Bits.Bit06,
        /// <summary>
        /// Defines warning for low value.
        /// </summary>
        WarningLow = (uint)Bits.Bit07,
        /// <summary>
        /// Defines alarm for flat-lined value, i.e., latched value test alarm.
        /// </summary>
        FlatlineAlarm = (uint)Bits.Bit08,
        /// <summary>
        /// Defines comparison alarm, i.e., outside threshold of comparison with a real-time value.
        /// </summary>
        ComparisonAlarm = (uint)Bits.Bit09,
        /// <summary>
        /// Defines rate-of-change alarm.
        /// </summary>
        ROCAlarm = (uint)Bits.Bit10,
        /// <summary>
        /// Defines bad value received.
        /// </summary>
        ReceivedAsBad = (uint)Bits.Bit11,
        /// <summary>
        /// Defines calculated value state.
        /// </summary>
        CalculatedValue = (uint)Bits.Bit12,
        /// <summary>
        /// Defines calculation error with the value.
        /// </summary>
        CalculationError = (uint)Bits.Bit13,
        /// <summary>
        /// Defines calculation warning with the value.
        /// </summary>
        CalculationWarning = (uint)Bits.Bit14,
        /// <summary>
        /// Defines reserved quality flag.
        /// </summary>
        ReservedQualityFlag = (uint)Bits.Bit15,
        /// <summary>
        /// Defines bad time state.
        /// </summary>
        BadTime = (uint)Bits.Bit16,
        /// <summary>
        /// Defines suspect time state.
        /// </summary>
        SuspectTime = (uint)Bits.Bit17,
        /// <summary>
        /// Defines late time alarm.
        /// </summary>
        LateTimeAlarm = (uint)Bits.Bit18,
        /// <summary>
        /// Defines future time alarm.
        /// </summary>
        FutureTimeAlarm = (uint)Bits.Bit19,
        /// <summary>
        /// Defines up-sampled state.
        /// </summary>
        UpSampled = (uint)Bits.Bit20,
        /// <summary>
        /// Defines down-sampled state.
        /// </summary>
        DownSampled = (uint)Bits.Bit21,
        /// <summary>
        /// Defines discarded value state.
        /// </summary>
        DiscardedValue = (uint)Bits.Bit22,
        /// <summary>
        /// Defines reserved time flag.
        /// </summary>
        ReservedTimeFlag = (uint)Bits.Bit23,
        /// <summary>
        /// Defines user defined flag 1.
        /// </summary>
        UserDefinedFlag1 = (uint)Bits.Bit24,
        /// <summary>
        /// Defines user defined flag 2.
        /// </summary>
        UserDefinedFlag2 = (uint)Bits.Bit25,
        /// <summary>
        /// Defines user defined flag 3.
        /// </summary>
        UserDefinedFlag3 = (uint)Bits.Bit26,
        /// <summary>
        /// Defines user defined flag 4.
        /// </summary>
        UserDefinedFlag4 = (uint)Bits.Bit27,
        /// <summary>
        /// Defines user defined flag 5.
        /// </summary>
        UserDefinedFlag5 = (uint)Bits.Bit28,
        /// <summary>
        /// Defines system error state.
        /// </summary>
        SystemError = (uint)Bits.Bit29,
        /// <summary>
        /// Defines system warning state.
        /// </summary>
        SystemWarning = (uint)Bits.Bit30,
        /// <summary>
        /// Defines measurement error flag.
        /// </summary>
        MeasurementError = (uint)Bits.Bit31
    }

    /// <summary>
    /// <see cref="DataPublisher"/> data packet flags.
    /// </summary>
    [Flags]
    public enum DataPacketFlags : byte
    {
        /// <summary>
        /// Determines if data packet is synchronized.
        /// </summary>
        /// <remarks>
        /// Bit set = synchronized, bit clear = unsynchronized.
        /// </remarks>
        Synchronized = (byte)Bits.Bit00,
        /// <summary>
        /// Determines if serialized measurement is compact.
        /// </summary>
        /// <remarks>
        /// Bit set = compact, bit clear = full fidelity.
        /// </remarks>
        Compact = (byte)Bits.Bit01,
        /// <summary>
        /// Determines which cipher index to use when encrypting data packet.
        /// </summary>
        /// <remarks>
        /// Bit set = use odd cipher index (i.e., 1), bit clear = use even cipher index (i.e., 0).
        /// </remarks>
        CipherIndex = (byte)Bits.Bit02,
        /// <summary>
        /// Determines if data packet payload is compressed.
        /// </summary>
        /// <remarks>
        /// Bit set = payload compressed, bit clear = payload normal.
        /// </remarks>
        Compressed = (byte)Bits.Bit03,
        /// <summary>
        /// Determines if the compressed data payload is in little-endian order.
        /// </summary>
        /// <remarks>
        /// Bit set = little-endian order compression, bit clear = big-endian order compression.
        /// </remarks>
        LittleEndianCompression = (byte)Bits.Bit04,
        /// <summary>
        /// No flags set.
        /// </summary>
        /// <remarks>
        /// This would represent unsynchronized, full fidelity measurement data packets.
        /// </remarks>
        NoFlags = (Byte)Bits.Nil
    }

    /// <summary>
    /// Server commands received by <see cref="DataPublisher"/> and sent by <see cref="DataSubscriber"/>.
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
        /// <summary>
        /// Authenticate command.
        /// </summary>
        /// <remarks>
        /// Requests that server authenticate client with the following encrypted authentication packet.
        /// Successful return message type will be string indicating client ID was validated.
        /// Client should not attempt further steps if authentication fails, user will need to take action.
        /// Client should send MetaDataRefresh and Subscribe commands upon successful authentication.
        /// If server is setup to not require authentication, authentication step can be skipped - however
        /// it will be up to the implementation to know if authentication is required - the server cannot be
        /// queried to determine if authentication is required.
        /// It is expected that use of this protocol between gateways over a shared network (i.e., the lower
        /// security zone) will require authentication.
        /// It is expected that use of this protocol between gateway and other systems (e.g., a PDC) in a
        /// private network (i.e., the higher security zone) can be setup to not require authentication.
        /// </remarks>
        Authenticate = 0x00,

        /// <summary>
        /// Meta data refresh command.
        /// </summary>
        /// <remarks>
        /// Requests that server send an updated set of metadata so client can refresh its point list.
        /// Successful return message type will be a <see cref="DataSet"/> containing server device and measurement metadata.
        /// Received device list should be defined as children of the "parent" server device connection similar to the way
        /// PMUs are defined as children of a parent PDC device connection.
        /// Devices and measurements contain unique Guids that should be used to key metadata updates in local repository.
        /// Optional string based message can follow command that should represent client requested meta-data filtering
        /// expressions, e.g.: "FILTER MeasurementDetail WHERE SignalType &lt;&gt; 'STAT'"
        /// </remarks>
        MetaDataRefresh = 0x01,

        /// <summary>
        /// Subscribe command.
        /// </summary>
        /// <remarks>
        /// Requests a subscription of streaming data from server based on connection string that follows.
        /// It will not be necessary to stop an existing subscription before requesting a new one.
        /// Successful return message type will be string indicating total number of allowed points.
        /// Client should wait for UpdateSignalIndexCache and UpdateBaseTime response codes before attempting
        /// to parse data when using the compact measurement format.
        /// </remarks>
        Subscribe = 0x02,

        /// <summary>
        /// Unsubscribe command.
        /// </summary>
        /// <remarks>
        /// Requests that server stop sending streaming data to the client and cancel the current subscription.
        /// </remarks>
        Unsubscribe = 0x03,

        /// <summary>
        /// Rotate cipher keys.
        /// </summary>
        /// <remarks>
        /// Manually requests that server send a new set of cipher keys for data packet encryption.
        /// </remarks>
        RotateCipherKeys = 0x04,

        /// <summary>
        /// Update processing interval.
        /// </summary>
        /// <remarks>
        /// Manually requests server to update the processing interval with the following specified value.
        /// </remarks>
        UpdateProcessingInterval = 0x05,

        /// <summary>
        /// Define operational modes for subscriber connection.
        /// </summary>
        /// <remarks>
        /// As soon as connection is established, requests that server set operational
        /// modes that affect how the subscriber and publisher will communicate.
        /// </remarks>
        DefineOperationalModes = 0x06,

        /// <summary>
        /// Confirm receipt of a notification.
        /// </summary>
        /// <remarks>
        /// This message is sent in response to <see cref="ServerResponse.Notify"/>.
        /// </remarks>
        ConfirmNotification = 0x07,

        /// <summary>
        /// Confirm receipt of a buffer block measurement.
        /// </summary>
        /// <remarks>
        /// This message is sent in response to <see cref="ServerResponse.BufferBlock"/>.
        /// </remarks>
        ConfirmBufferBlock = 0x08,

        /// <summary>
        /// Provides measurements to the publisher over the command channel.
        /// </summary>
        /// <remarks>
        /// Allows for unsolicited publication of measurement data to the server
        /// so that consumers of data can also provide data to other consumers.
        /// </remarks>
        PublishCommandMeasurements = 0x09,

        /// <summary>
        /// Code for handling user-defined commands.
        /// </summary>
        UserCommand00 = 0xD0,

        /// <summary>
        /// Code for handling user-defined commands.
        /// </summary>
        UserCommand01 = 0xD1,

        /// <summary>
        /// Code for handling user-defined commands.
        /// </summary>
        UserCommand02 = 0xD2,

        /// <summary>
        /// Code for handling user-defined commands.
        /// </summary>
        UserCommand03 = 0xD3,

        /// <summary>
        /// Code for handling user-defined commands.
        /// </summary>
        UserCommand04 = 0xD4,

        /// <summary>
        /// Code for handling user-defined commands.
        /// </summary>
        UserCommand05 = 0xD5,

        /// <summary>
        /// Code for handling user-defined commands.
        /// </summary>
        UserCommand06 = 0xD6,

        /// <summary>
        /// Code for handling user-defined commands.
        /// </summary>
        UserCommand07 = 0xD7,

        /// <summary>
        /// Code for handling user-defined commands.
        /// </summary>
        UserCommand08 = 0xD8,

        /// <summary>
        /// Code for handling user-defined commands.
        /// </summary>
        UserCommand09 = 0xD9,

        /// <summary>
        /// Code for handling user-defined commands.
        /// </summary>
        UserCommand10 = 0xDA,

        /// <summary>
        /// Code for handling user-defined commands.
        /// </summary>
        UserCommand11 = 0xDB,

        /// <summary>
        /// Code for handling user-defined commands.
        /// </summary>
        UserCommand12 = 0xDC,

        /// <summary>
        /// Code for handling user-defined commands.
        /// </summary>
        UserCommand13 = 0xDD,

        /// <summary>
        /// Code for handling user-defined commands.
        /// </summary>
        UserCommand14 = 0xDE,

        /// <summary>
        /// Code for handling user-defined commands.
        /// </summary>
        UserCommand15 = 0xDF
    }

    /// <summary>
    /// Server responses sent by <see cref="DataPublisher"/> and received by <see cref="DataSubscriber"/>.
    /// </summary>
    public enum ServerResponse : byte
    {
        // Although the server commands and responses will be on two different paths, the response enumeration values
        // are defined as distinct from the command values to make it easier to identify codes from a wire analysis.

        /// <summary>
        /// Command succeeded response.
        /// </summary>
        /// <remarks>
        /// Informs client that its solicited server command succeeded, original command and success message follow.
        /// </remarks>
        Succeeded = 0x80,

        /// <summary>
        /// Command failed response.
        /// </summary>
        /// <remarks>
        /// Informs client that its solicited server command failed, original command and failure message follow.
        /// </remarks>
        Failed = 0x81,

        /// <summary>
        /// Data packet response.
        /// </summary>
        /// <remarks>
        /// Unsolicited response informs client that a data packet follows.
        /// </remarks>
        DataPacket = 0x82,

        /// <summary>
        /// Update signal index cache response.
        /// </summary>
        /// <remarks>
        /// Unsolicited response requests that client update its runtime signal index cache with the one that follows.
        /// </remarks>
        UpdateSignalIndexCache = 0x83,

        /// <summary>
        /// Update runtime base-timestamp offsets response.
        /// </summary>
        /// <remarks>
        /// Unsolicited response requests that client update its runtime base-timestamp offsets with those that follow.
        /// </remarks>
        UpdateBaseTimes = 0x84,

        /// <summary>
        /// Update runtime cipher keys response.
        /// </summary>
        /// <remarks>
        /// Response, solicited or unsolicited, requests that client update its runtime data cipher keys with those that follow.
        /// </remarks>
        UpdateCipherKeys = 0x85,

        /// <summary>
        /// Data start time response packet.
        /// </summary>
        /// <remarks>
        /// Unsolicited response provides the start time of data being processed from the first measurement.
        /// </remarks>
        DataStartTime = 0x86,

        /// <summary>
        /// Processing complete notification.
        /// </summary>
        /// <remarks>
        /// Unsolicited response provides notification that input processing has completed, typically via temporal constraint.
        /// </remarks>
        ProcessingComplete = 0x87,

        /// <summary>
        /// Buffer block response.
        /// </summary>
        /// <remarks>
        /// Unsolicited response informs client that a raw buffer block follows.
        /// </remarks>
        BufferBlock = 0x88,

        /// <summary>
        /// Notify response.
        /// </summary>
        /// <remarks>
        /// Unsolicited response provides a notification message to the client.
        /// </remarks>
        Notify = 0x89,

        /// <summary>
        /// Configuration changed response.
        /// </summary>
        /// <remarks>
        /// Unsolicited response provides a notification that the publisher's source configuration has changed and that
        /// client may want to request a meta-data refresh.
        /// </remarks>
        ConfigurationChanged = 0x8A,

        /// <summary>
        /// Code for handling user-defined responses.
        /// </summary>
        UserResponse00 = 0xE0,

        /// <summary>
        /// Code for handling user-defined responses.
        /// </summary>
        UserResponse01 = 0xE1,

        /// <summary>
        /// Code for handling user-defined responses.
        /// </summary>
        UserResponse02 = 0xE2,

        /// <summary>
        /// Code for handling user-defined responses.
        /// </summary>
        UserResponse03 = 0xE3,

        /// <summary>
        /// Code for handling user-defined responses.
        /// </summary>
        UserResponse04 = 0xE4,

        /// <summary>
        /// Code for handling user-defined responses.
        /// </summary>
        UserResponse05 = 0xE5,

        /// <summary>
        /// Code for handling user-defined responses.
        /// </summary>
        UserResponse06 = 0xE6,

        /// <summary>
        /// Code for handling user-defined responses.
        /// </summary>
        UserResponse07 = 0xE7,

        /// <summary>
        /// Code for handling user-defined responses.
        /// </summary>
        UserResponse08 = 0xE8,

        /// <summary>
        /// Code for handling user-defined responses.
        /// </summary>
        UserResponse09 = 0xE9,

        /// <summary>
        /// Code for handling user-defined responses.
        /// </summary>
        UserResponse10 = 0xEA,

        /// <summary>
        /// Code for handling user-defined responses.
        /// </summary>
        UserResponse11 = 0xEB,

        /// <summary>
        /// Code for handling user-defined responses.
        /// </summary>
        UserResponse12 = 0xEC,

        /// <summary>
        /// Code for handling user-defined responses.
        /// </summary>
        UserResponse13 = 0xED,

        /// <summary>
        /// Code for handling user-defined responses.
        /// </summary>
        UserResponse14 = 0xEE,

        /// <summary>
        /// Code for handling user-defined responses.
        /// </summary>
        UserResponse15 = 0xEF,

        /// <summary>
        /// No operation keep-alive ping.
        /// </summary>
        /// <remarks>
        /// The command channel can remain quiet for some time, this command allows a period test of client connectivity.
        /// </remarks>
        NoOP = 0xFF
    }

    /// <summary>
    /// Operational modes that affect how <see cref="DataPublisher"/> and <see cref="DataSubscriber"/> communicate.
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
        /// Mask to get version number of protocol.
        /// </summary>
        /// <remarks>
        /// Version number is currently set to 1.
        /// </remarks>
        VersionMask = (uint)(Bits.Bit04 | Bits.Bit03 | Bits.Bit02 | Bits.Bit01 | Bits.Bit00),
        /// <summary>
        /// Mask to get mode of compression.
        /// </summary>
        /// <remarks>
        /// GZip and TSSC compression are the only modes currently supported. Remaining bits are
        /// reserved for future compression modes.
        /// </remarks>
        CompressionModeMask = (uint)(Bits.Bit07 | Bits.Bit06 | Bits.Bit05),
        /// <summary>
        /// Mask to get character encoding used when exchanging messages between publisher and subscriber.
        /// </summary>
        /// <remarks>
        /// 00 = UTF-16, little endian<br/>
        /// 01 = UTF-16, big endian<br/>
        /// 10 = UTF-8<br/>
        /// 11 = ANSI
        /// </remarks>
        EncodingMask = (uint)(Bits.Bit09 | Bits.Bit08),
        /// <summary>
        /// Determines whether external measurements are exchanged during metadata synchronization.
        /// </summary>
        /// <remarks>
        /// Bit set = external measurements are exchanged, bit clear = no external measurements are exchanged
        /// </remarks>
        ReceiveExternalMetadata = (uint)Bits.Bit25,
        /// <summary>
        /// Determines whether internal measurements are exchanged during metadata synchronization.
        /// </summary>
        /// <remarks>
        /// Bit set = internal measurements are exchanged, bit clear = no internal measurements are exchanged
        /// </remarks>
        ReceiveInternalMetadata = (uint)Bits.Bit26,
        /// <summary>
        /// Determines whether payload data is compressed when exchanging between publisher and subscriber.
        /// </summary>
        /// <remarks>
        /// Bit set = compress, bit clear = no compression
        /// </remarks>
        CompressPayloadData = (uint)Bits.Bit29,
        /// <summary>
        /// Determines whether the signal index cache is compressed when exchanging between publisher and subscriber.
        /// </summary>
        /// <remarks>
        /// Bit set = compress, bit clear = no compression
        /// </remarks>
        CompressSignalIndexCache = (uint)Bits.Bit30,
        /// <summary>
        /// Determines whether metadata is compressed when exchanging between publisher and subscriber.
        /// </summary>
        /// <remarks>
        /// Bit set = compress, bit clear = no compression
        /// </remarks>
        CompressMetadata = (uint)Bits.Bit31,
        /// <summary>
        /// No flags set.
        /// </summary>
        /// <remarks>
        /// This would represent protocol version 0,
        /// UTF-16 little endian character encoding,
        /// .NET serialization and no compression.
        /// </remarks>
        NoFlags = (uint)Bits.Nil
    }

    /// <summary>
    /// Enumeration for character encodings supported by the Streaming Telemetry Transport Protocol.
    /// </summary>
    public enum OperationalEncoding : uint
    {
        /// <summary>
        /// UTF-16, little endian
        /// </summary>
        UTF16LE = (uint)Bits.Nil,
        /// <summary>
        /// UTF-16, big endian
        /// </summary>
        UTF16BE = (uint)Bits.Bit08,
        /// <summary>
        /// UTF-8
        /// </summary>
        UTF8 = (uint)Bits.Bit09
    }

    /// <summary>
    /// Enumeration for compression modes supported by the Streaming Telemetry Transport Protocol.
    /// </summary>
    [Flags]
    public enum CompressionModes : uint
    {
        /// <summary>
        /// GZip compression
        /// </summary>
        GZip = (uint)Bits.Bit05,
        /// <summary>
        /// TSSC compression
        /// </summary>
        TSSC = (uint)Bits.Bit06,
        /// <summary>
        /// No compression
        /// </summary>
        None = (uint)Bits.Nil
    }

    /// <summary>
    /// Security modes used by the <see cref="DataPublisher"/> to secure data sent over the command channel.
    /// </summary>
    public enum SecurityMode
    {
        /// <summary>
        /// No security.
        /// </summary>
        None,

        /// <summary>
        /// Transport Layer Security.
        /// </summary>
        TLS
    }
}
