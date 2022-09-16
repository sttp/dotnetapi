//******************************************************************************************************
//  Config.cs - Gbtc
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

using sttp.transport;

namespace sttp;

/// <summary>
/// Defines the STTP connection related configuration parameters.
/// </summary>
public class Config
{
    /// <summary>
    /// Gets or sets the maximum number of times to retry a connection.
    /// Set value to -1 to retry infinitely.
    /// </summary>
    public int MaxRetries { get; set; } = Default.MaxRetries;

    /// <summary>
    /// Gets or sets the base retry interval, in seconds. Retries will exponentially
    /// back-off starting from this interval.
    /// </summary>
    public double RetryInterval { get; set; } = Default.RetryInterval;

    /// <summary>
    /// Gets or sets the maximum retry interval, in seconds.
    /// </summary>
    public double MaxRetryInterval { get; set; } = Default.MaxRetryInterval;

    /// <summary>
    /// Gets or sets flag that determines if connections should be automatically
    /// reattempted.
    /// </summary>
    public bool AutoReconnect { get; set; } = Default.AutoReconnect;

    /// <summary>
    /// Gets or sets flag that determines if metadata should be automatically requested
    /// upon successful connection.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, metadata will be requested upon connection before subscription;
    /// otherwise, any metadata operations must be handled manually.
    /// </remarks>
    public bool AutoRequestMetadata { get; set; } = Default.AutoRequestMetadata;

    /// <summary>
    /// Gets or sets flag that determines if subscription should be handled automatically
    /// upon successful connection.
    /// </summary>
    /// <remarks>
    /// When <see cref="AutoRequestMetadata"/> is <c>true</c> and <see cref="AutoSubscribe"/>
    /// is <c>true</c>, subscription will occur after reception of metadata. When
    /// <see cref="AutoRequestMetadata"/> is <c>false</c> and <see cref="AutoSubscribe"/> is
    /// <c>true</c>, subscription will occur at successful connection. When
    /// <see cref="AutoSubscribe"/> is <c>false</c>, any subscribe operations must be handled
    /// manually.
    /// </remarks>
    public bool AutoSubscribe { get; set; } = Default.AutoSubscribe;

    /// <summary>
    /// Gets or sets flag that determines whether payload data is compressed.
    /// </summary>
    public bool CompressPayloadData { get; set; } = Default.CompressPayloadData;

    /// <summary>
    /// Gets or sets flag that determines whether the metadata transfer is compressed.
    /// </summary>
    public bool CompressMetadata { get; set; } = Default.CompressMetadata;

    /// <summary>
    /// Gets or sets flag that determines whether the signal index cache is compressed.
    /// </summary>
    public bool CompressSignalIndexCache { get; set; } = Default.CompressSignalIndexCache;

    /// <summary>
    /// Gets or sets any filters to be applied to incoming metadata to reduce total received
    /// metadata. Each filter expression should be separated by semi-colon.
    /// </summary>
    public string MetadataFilters { get; set; } = Default.MetadataFilters;

    /// <summary>
    /// Gets or sets the target STTP protocol version. This currently defaults to 2.
    /// </summary>
    public byte Version { get; set; } = Default.Version;
}

