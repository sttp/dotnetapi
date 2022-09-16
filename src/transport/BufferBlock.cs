//******************************************************************************************************
//  BufferBlock.cs - Gbtc
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
//  09/16/2022 - Ritchie Carroll
//       Generated original version of source code.
//
//******************************************************************************************************


namespace sttp.transport;

/// <summary>
/// BufferBlock defines an atomic unit of data, i.e., a binary buffer, for transport in STTP.
/// </summary>
public class BufferBlock
{
    /// <summary>
    /// Defines measurement's globally unique identifier.
    /// </summary>
    public Guid SignalID { get; set; }

    /// <summary>
    /// Gets measurement buffer as an atomic unit of data, i.e., a binary buffer.
    /// </summary>
    /// <remarks>
    /// This buffer typically represents a partial image of a larger whole.
    /// </remarks>
    public byte[]? Buffer { get; set; }
}
