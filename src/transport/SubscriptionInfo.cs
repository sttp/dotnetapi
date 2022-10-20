//******************************************************************************************************
//  SubscriptionInfo.cs - Gbtc
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
//  10/19/2022 - rcarroll
//       Generated original version of source code.
//
//******************************************************************************************************

namespace sttp.transport
{
    /// <summary>
    /// Defines subscription related settings for a <see cref="DataSubscriber"/> instance.
    /// </summary>
    public class SubscriptionInfo
    {
        /// <summary>
        /// Gets or sets the desired measurements for a subscription. Examples include:
        /// <list type="bullet">
        /// <item>
        /// <description>
        /// Directly specified signal IDs (UUID values in string format):<br/>
        /// <c>  38A47B0-F10B-4143-9A0A-0DBC4FFEF1E8; {E4BBFE6A-35BD-4E5B-92C9-11FF913E7877}</c>
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// Directly specified tag names:
        /// <c>  DOM_GPLAINS-BUS1:VH; TVA_SHELBY-BUS1:VH</c>
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// A filter expression against a selection view::<br/>
        /// <c>  FILTER ActiveMeasurements WHERE Company = 'GPA' AND SignalType = 'FREQ'</c>
        /// </description>
        /// </item>
        /// </list>
        /// </summary>
        public string FilterExpression { get; set; } = Default.FilterExpression;

        /// <summary>
        /// Gets or sets flag that determines if data will be published using down-sampling.
        /// </summary>
        public bool Throttled { get; set; } = Default.Throttled;

        /// <summary>
        /// Gets or sets the down-sampling publish interval to use when <see cref="Throttled"/> is <c>true</c>.
        /// </summary>
        public double PublishInterval { get; set; } = Default.PublishInterval;

        /// <summary>
        /// Gets or sets flag that requests that a UDP channel be used for data publication.
        /// </summary>
        public bool EnableUDPDataChannel { get; set; } = Default.EnableUDPDataChannel;

        /// <summary>
        /// Gets or sets the desired UDP port to use for publication.
        /// </summary>
        public ushort DataChannelLocalPort { get; set; } = Default.UDPDataChannelPort;

        /// <summary>
        /// Gets or sets the desired network interface to use for UDP publication.
        /// </summary>
        public string DataChannelInterface { get; set; } = Default.UDPDataChannelInterface;

        /// <summary>
        /// Gets or sets flag that determines if time should be included in non-compressed, compact measurements.
        /// </summary>
        public bool IncludeTime { get; set; } = Default.IncludeTime;

        /// <summary>
        /// Gets or sets flag that determines if time should be restricted to milliseconds in non-compressed, compact measurements.
        /// </summary>
        public bool UseMillisecondResolution { get; set; } = Default.UseMillisecondResolution;

        /// <summary>
        /// Gets or sets flag that requests that the publisher filter, i.e., does not send, any NaN values. 
        /// </summary>
        public bool RequestNANValueFilter { get; set; } = Default.RequestNaNValueFilter;

        /// <summary>
        /// Gets or sets that defines the start time for a requested temporal data playback, i.e., a historical subscription.
        /// Simply by specifying a <see cref="StartTime"/> and <see cref="StopTime"/>, a subscription is considered a historical subscription.
        /// Note that the publisher may not support historical subscriptions, in which case the subscribe will fail.
        /// </summary>
        public string StartTime { get; set; } = Default.StartTime;

        /// <summary>
        /// Gets or sets that defines the stop time for a requested temporal data playback, i.e., a historical subscription.
        /// Simply by specifying a <see cref="StartTime"/> and <see cref="StopTime"/>, a subscription is considered a historical subscription.
        /// Note that the publisher may not support historical subscriptions, in which case the subscribe will fail.
        /// </summary>
        public string StopTime { get; set; } = Default.StopTime;

        /// <summary>
        /// Gets or sets any custom constraint parameters for a requested temporal data playback. This can include
        /// parameters that may be needed to initiate, filter, or control historical data access.
        /// </summary>
        public string ContraintParameters { get; set; } = Default.ConstraintParameters;

        /// <summary>
        /// Gets or sets the initial playback speed, in milliseconds, for a requested temporal data playback.
        /// With the exception of the values of -1 and 0, this value specifies the desired processing interval for data, i.e.,
        /// basically a delay, or timer interval, over which to process data.A value of -1 means to use the default processing
        /// interval while a value of 0 means to process data as fast as possible.
        /// </summary>
        public int ProcessingInterval { get; set; } = Default.ProcessingInterval;

        /// <summary>
        /// Gets or sets any extra or custom connection string parameters that may be needed for a subscription.
        /// </summary>
        public string ExtraConnectionStringParameters { get; set; } = Default.ExtraConnectionStringParameters;
    }
}
