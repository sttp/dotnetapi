//******************************************************************************************************
//  Measurement.cs - Gbtc
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
//  09/20/2022 - Ritchie Carroll
//       Generated original version of source code.
//
//******************************************************************************************************

namespace sttp.transport;

/// <summary>
/// Represents a basic unit of measured data for transmission or reception in the STTP API.
/// </summary>
public class Measurement
{
    /// <summary>
    /// Defines measurement's globally unique identifier.
    /// </summary>
    public Guid SignalID { get; set; }

    /// <summary>
    /// Defines instantaneous value of the measurement.
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    /// Defines the STTP uint64 timestamp, in ticks, that measurement was taken.
    /// </summary>
    public ulong Timestamp { get; set; }

    /// <summary>
    /// Defines flags indicating the state of the measurement as reported by the device that took it.
    /// </summary>
    public StateFlags Flags { get; set; }
}
