﻿//******************************************************************************************************
//  SimpleCertificateChecker.cs - Gbtc
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

using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace sttp.security
{
    /// <summary>
    /// Simple implementation of <see cref="ICertificateChecker"/>.
    /// </summary>
    public class SimpleCertificateChecker : ICertificateChecker
    {
        #region [ Members ]

        // Fields
        private readonly List<X509Certificate> m_trustedCertificates;
        private SslPolicyErrors m_validPolicyErrors;
        private X509ChainStatusFlags m_validChainFlags;
        private string m_reasonForFailure;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Creates a new instance of the <see cref="SimpleCertificateChecker"/> class.
        /// </summary>
        public SimpleCertificateChecker()
        {
            m_trustedCertificates = new List<X509Certificate>();
            m_validPolicyErrors = SslPolicyErrors.None;
            m_validChainFlags = X509ChainStatusFlags.NoError;
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets the list of certificates on the system which are
        /// considered trusted when validating remote certificates.
        /// </summary>
        public List<X509Certificate> TrustedCertificates => m_trustedCertificates;

        /// <summary>
        /// Gets or sets the set of invalid policy errors.
        /// </summary>
        public SslPolicyErrors ValidPolicyErrors
        {
            get => m_validPolicyErrors;
            set => m_validPolicyErrors = value;
        }

        /// <summary>
        /// Gets or sets the set of invalid chain flags.
        /// </summary>
        public X509ChainStatusFlags ValidChainFlags
        {
            get => m_validChainFlags;
            set => m_validChainFlags = value;
        }

        /// <summary>
        /// Gets the reason why the remote certificate validation
        /// failed, or null if certificate validation did not fail.
        /// </summary>
        public string ReasonForFailure => m_reasonForFailure;

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Validates the given remote certificate to determine if the host is trusted.
        /// </summary>
        /// <param name="remoteCertificate">Certificate of the remote host.</param>
        /// <returns>True if the remote certificate is trusted; false otherwise.</returns>
        public bool ValidateRemoteCertificate(X509Certificate remoteCertificate)
        {
            byte[] hash = remoteCertificate.GetCertHash();
            byte[] key = remoteCertificate.GetPublicKey();
            bool hashMatch, keyMatch;

            foreach (X509Certificate certificate in TrustedCertificates)
            {
                hashMatch = hash.SequenceEqual(certificate.GetCertHash());
                keyMatch = hashMatch && key.SequenceEqual(certificate.GetPublicKey());

                if (keyMatch)
                    return true;
            }

            m_reasonForFailure = "No matching certificate found in the list of trusted certificates.";

            return false;
        }

        /// <summary>
        /// Verifies the remote certificate used for authentication.
        /// </summary>
        /// <param name="sender">An object that contains state information for this validation.</param>
        /// <param name="remoteCertificate">The certificate used to authenticate the remote party.</param>
        /// <param name="chain">The chain of certificate authorities associated with the remote certificate.</param>
        /// <param name="errors">One or more errors associated with the remote certificate.</param>
        /// <returns>A flag that determines whether the specified certificate is accepted for authentication.</returns>
        public bool ValidateRemoteCertificate(object sender, X509Certificate remoteCertificate, X509Chain chain, SslPolicyErrors errors)
        {
            X509ChainStatusFlags chainFlags;
            m_reasonForFailure = null;

            if ((errors & ~m_validPolicyErrors) != SslPolicyErrors.None)
            {
                m_reasonForFailure = $"Policy errors encountered during validation: {errors & ~m_validPolicyErrors}";
                return false;
            }

            chainFlags = chain.ChainStatus.Aggregate(X509ChainStatusFlags.NoError, (flags, status) => flags | (status.Status & ~m_validChainFlags));

            if (chainFlags != X509ChainStatusFlags.NoError)
            {
                m_reasonForFailure = $"Invalid chain flags found during validation: {chainFlags}";
                return false;
            }

            return ValidateRemoteCertificate(remoteCertificate);
        }

        #endregion
    }
}
