//******************************************************************************************************
//  SubscriberConnector.cs - Gbtc
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
    /// Represents a connector that will establish or automatically reestablish a connection
    /// from a <see cref="DataSubscriber"/> to a <see cref="DataPublisher"/>.
    /// </summary>
    public class SubscriberConnector
    {
        internal bool ConnectionRefused { get; set; }

        /// <summary>
        /// Cleanly shuts down a <see cref="SubscriberConnector"/> that is no longer being used, e.g., during a normal application exit.
        /// </summary>
        public void Dispose()
        {
            //m_disposing = true;
        }

        internal void ResetConnection()
        {

        }
    }
}
