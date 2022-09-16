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

namespace sttp.transport;

/// <summary>
/// Defines defaults for STTP settings.
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
    /// Default to the compres payload data flag.
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