﻿//******************************************************************************************************
//  IProvideStatus.cs - Gbtc
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

namespace sttp
{
    /// <summary>
    /// Defines an interface for any object to allow it to provide a name and status
    /// that can be displayed for informational purposes.
    /// </summary>
    public interface IProvideStatus
    {
        /// <summary>
        /// Gets the name of the object providing status information.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the current status details about object providing status information.
        /// </summary>
        string Status { get; }
    }
 }
