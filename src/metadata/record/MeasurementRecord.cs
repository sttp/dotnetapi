//******************************************************************************************************
//  MeasurementRecord.cs - Gbtc
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

using Gemstone.Numeric.EE;
using sttp.transport;

namespace sttp.metadata.record
{
    /// <summary>
    /// Represents a record of measurement metadata in the STTP.
    /// </summary>
    /// <remarks>
    /// The <see cref="MeasurementRecord"/> defines  ancillary information associated with a <see cref="Measurement"/>.
    /// Metadata gets cached in a registry associated with a <see cref="DataSubscriber"/>.
    /// </remarks>
    public class MeasurementRecord
    {
        private string? m_signalTypeName;

        /// <summary>
        /// Gets the unique guid-based signal identifier for this <see cref="MeasurementRecord"/>.
        /// </summary>
        public Guid SignalID { get; init; }

        /// <summary>
        /// Gets the additive value modifier. Allows for linear value adjustment. Defaults to zero.
        /// </summary>
        public double Adder { get; init; } = 0.0D;

        /// <summary>
        /// Gets the multiplicative value modifier. Allows for linear value adjustment. Defaults to one.
        /// </summary>
        public double Multiplier { get; init; } = 1.0D;

        /// <summary>
        /// Gets the STTP numeric ID number (from measurement key) for this <see cref="MeasurementRecord"/>.
        /// </summary>
        public ulong ID { get; init; }

        /// <summary>
        /// Gets the STTP source instance (from measurement key) for this <see cref="MeasurementRecord"/>.
        /// </summary>
        public string Source { get; init; } = string.Empty;

        /// <summary>
        /// Gets the signal type name for this `MeasurementRecord`, e.g., "FREQ".
        /// </summary>
        public string SignalTypeName
        {
            get => m_signalTypeName ??= (SignalType == SignalType.NONE ? "UNKN" : SignalType.ToString());
            init
            {
                m_signalTypeName = value;

                if (Enum.TryParse(m_signalTypeName, out SignalType signalType))
                    SignalType = signalType;
                else
                    SignalType = SignalType.NONE;
            }
        }

        /// <summary>
        /// Gets the <see cref="Gemstone.Numeric.EE.SignalType"/> enumeration for this <see cref="MeasurementRecord"/>,
        /// if it can be parsed from <see cref="SignalTypeName"/>; otherwise, returns <see cref="SignalType.NONE"/>.
        /// </summary>
        public SignalType SignalType { get; private set; } = SignalType.NONE;

        /// <summary>
        /// Gets the unique signal reference for this <see cref="MeasurementRecord"/>.
        /// </summary>
        public string SignalReference { get; init; } = string.Empty;

        /// <summary>
        /// Gets the unique point tag for this <see cref="MeasurementRecord"/>.
        /// </summary>
        public string PointTag { get; init; } = string.Empty;

        /// <summary>
        /// Gets the alpha-numeric identifier of the associated device for this <see cref="MeasurementRecord"/>.
        /// </summary>
        public string DeviceAcronym { get; init; } = string.Empty;

        /// <summary>
        /// Gets the description for this <see cref="MeasurementRecord"/>.
        /// </summary>
        public string Description { get; init; } = string.Empty;

        /// <summary>
        /// Gets the <see cref="DateTime"/> of when this <see cref="MeasurementRecord"/> was last updated.
        /// </summary>
        public DateTime UpdatedOn { get; init; }
    }
}
