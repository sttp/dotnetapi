//******************************************************************************************************
//  Common.cs - Gbtc
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

using System.Security.Cryptography;
using Microsoft.Win32;

namespace sttp.transport
{
    /// <summary>
    /// Common static methods and extensions for transport library.
    /// </summary>
    public static class Common
    {
        // Flag that determines if managed encryption wrappers should be used over FIPS-compliant algorithms.
        private static readonly bool s_useManagedEncryption;

        // Static Constructor
        static Common()
        {
#if MONO
            s_useManagedEncryption = true;
#else
            const string fipsKeyOld = "HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Control\\Lsa";
            const string fipsKeyNew = "HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Control\\Lsa\\FipsAlgorithmPolicy";

            // Determine if the operating system configuration to set to use FIPS-compliant algorithms
            s_useManagedEncryption = (Registry.GetValue(fipsKeyNew, "Enabled", 0) ?? Registry.GetValue(fipsKeyOld, "FipsAlgorithmPolicy", 0)).ToString() == "0";
#endif
        }

        /// <summary>
        /// Gets flag that determines if managed encryption should be used.
        /// </summary>
        public static bool UseManagedEncryption => s_useManagedEncryption;

        /// <summary>
        /// Gets an AES symmetric algorithm to use for encryption or decryption.
        /// </summary>
        public static SymmetricAlgorithm SymmetricAlgorithm
        {
            get
            {
                Aes symmetricAlgorithm;

                if (s_useManagedEncryption)
                    symmetricAlgorithm = new AesManaged();
                else
                    symmetricAlgorithm = new AesCryptoServiceProvider();

                symmetricAlgorithm.KeySize = 256;

                return symmetricAlgorithm;
            }
        }
    }
}
