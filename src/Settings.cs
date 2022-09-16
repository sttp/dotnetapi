//******************************************************************************************************
//  Settings.cs - Gbtc
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
/// Defines the STTP subscription related settings.
/// </summary>
/// <remarks>
/// The <see cref="Settings"/> class exists as a simplified implementation of the
/// <see cref="SubscriptionInfo"/> class found in the <see cref="transport"/> namespace.
/// Internally, the <see cref="Subscriber"/> class maps <see cref="Settings"/> values to
/// a <see cref="SubscriptionInfo"/> instance for use with a <see cref="DataSubscriber"/>.
/// </remarks>
public class Settings
{
    /// <summary>
    /// Gets or sets flag that setermines if data will be published using down-sampling.
    /// </summary>
    public bool Throttled { get; set; } = Default.Throttled;

    /// <summary>
    /// Gets or sets flag that defines the down-sampling publish interval, in seconds,
    /// to use when <see cref="Throttled"/> is <c>true</c>. 
    /// </summary>
    public double PublishInterval { get; set; } = Default.PublishInterval;

    /// <summary>
    /// Gets or sets the desired UDP port to use for publication. Zero value means do not
    /// receive data on UDP, i.e., data will be delivered to the STTP client via TCP.
    /// </summary>
    public ushort UDPDataChannelPort { get; set; } = Default.UDPDataChannelPort;

    /// <summary>
    /// Gets or sets the desired UDP binding interface to use for publication. Empty string
    /// means to bind to same interface as TCP command channel.
    /// </summary>
    public string UDPDataChannelInterface { get; set; } = Default.UDPDataChannelInterface;

    /// <summary>
    /// Gets or sets flag that determines if time should be included in non-compressed,
    /// compact measurements.
    /// </summary>
    public bool IncludeTime { get; set; } = Default.IncludeTime;

    /// <summary>
    /// Gets or sets flag that determines if time should be restricted to milliseconds
    /// in non-compressed, compact measurements.
    /// </summary>
    public bool UseMillisecondResolution { get; set; } = Default.UseMillisecondResolution;

    /// <summary>
    /// Gets or sets flag that requests that the publisher filter, i.e., does not send,
    /// any <c>NaN</c> values.
    /// </summary>
    public bool RequestNaNValueFilter { get; set; } = Default.RequestNaNValueFilter;

    /// <summary>
    /// Gets or sets the start time for a requested temporal data playback, i.e., a historical
    /// subscription. Simply by specifying a <see cref="StartTime"/> and <see cref="StopTime"/>,
    /// a subscription is considered a historical subscription.
    /// </summary>
    public string StartTime { get; set; } = Default.StartTime;

    /// <summary>
    /// Gets or sets the stop time for a requested temporal data playback, i.e., a historical
    /// subscription. Simply by specifying a <see cref="StartTime"/> and <see cref="StopTime"/>,
    /// a subscription is considered a historical subscription.
    /// </summary>
    public string StopTime { get; set; } = Default.StopTime;

    /// <summary>
    /// Gets or sets any custom constraint parameters for a requested temporal data playback.
    /// This can include parameters that may be needed to initiate, filter, or control
    /// historical data access.
    /// </summary>
    public string ConstraintParameters { get; set; } = Default.ConstraintParameters;

    /// <summary>
    /// Gets or sets the initial playback speed, in milliseconds, for a requested temporal data
    /// playback.
    /// <remarks>
    /// With the exception of the values of -1 and 0, this value specifies the desired processing
    /// interval for data, i.e., basically a delay, or timer interval, over which to process data.
    /// A value of -1 means to use the default processing interval while a value of 0 means to
    /// process data as fast as possible.
    /// </remarks>
    /// </summary>
    public int ProcessingInterval { get; set; } = Default.ProcessingInterval;

    /// <summary>
    /// Gets or sets any extra custom connection string parameters that may be needed for a
    /// subscription.
    /// </summary>
    public string ExtraConnectionStringParameters { get; set; } = Default.ExtraConnectionStringParameters;
}
