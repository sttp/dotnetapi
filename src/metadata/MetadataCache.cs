//******************************************************************************************************
//  MetadataCache.cs - Gbtc
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

using sttp.metadata.record;

namespace sttp.metadata
{
    /// <summary>
    /// Represents a collection of parsed STTP metadata records.
    /// </summary>
    public class MetadataCache
    {
        /// <summary>
        /// Finds the <see cref="MeasurementRecord"/> for the specified <paramref name="signalID"/> in the cache.
        /// </summary>
        /// <param name="signalID">Signal ID to lookup.</param>
        /// <returns><see cref="MeasurementRecord"/> for specified <paramref name="signalID"/>.</returns>
        public MeasurementRecord? FindMeasurement(Guid signalID)
        {
            return default;
        }

        /// <summary>
        /// Adds specified <paramref name="record"/> to the cache.
        /// </summary>
        /// <param name="record">Measurment record to add.</param>
        public void AddMeasurementRecord(MeasurementRecord record)
        {
        }
    }
}
