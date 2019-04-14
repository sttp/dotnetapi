//******************************************************************************************************
//  TransportProtocol.cs - Gbtc
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
//  04/14/2019 - J. Ritchie Carroll
//       Imported source code from Grid Solutions Framework.
//
//******************************************************************************************************

namespace sttp.communication
{
    /// <summary>
    /// Indicates the protocol used in server-client communication.
    /// </summary>
    public enum TransportProtocol
    {
        /// <summary>
        /// <see cref="TransportProtocol"/> is Transmission Control Protocol.
        /// </summary>
        Tcp,
        /// <summary>
        /// <see cref="TransportProtocol"/> is User Datagram Protocol.
        /// </summary>
        Udp,
        /// <summary>
        /// <see cref="TransportProtocol"/> is serial interface.
        /// </summary>
        Serial,
        /// <summary>
        /// <see cref="TransportProtocol"/> is file-system based.
        /// </summary>
        File
    }
}
