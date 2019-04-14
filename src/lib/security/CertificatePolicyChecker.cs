﻿//******************************************************************************************************
//  CertificatePolicyChecker.cs - Gbtc
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
    /// Certificate checker that validates remote certificates based
    /// on certificate policies associated with each certificate.
    /// </summary>
    public class CertificatePolicyChecker : ICertificateChecker
    {
        #region [ Members ]

        // Fields
        private readonly Dictionary<X509Certificate, CertificatePolicy> m_trustedCertificates;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Creates a new instance of the <see cref="CertificatePolicyChecker"/> class.
        /// </summary>
        public CertificatePolicyChecker()
        {
            m_trustedCertificates = new Dictionary<X509Certificate, CertificatePolicy>();
            DefaultCertificatePolicy = new CertificatePolicy();
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets the default certificate policy used to validate
        /// certificates that do not have their own certificate policy.
        /// </summary>
        public CertificatePolicy DefaultCertificatePolicy { get; }

        /// <summary>
        /// Gets the reason why the remote certificate validation
        /// failed, or null if certificate validation did not fail.
        /// </summary>
        public string ReasonForFailure { get; private set; }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Trusts the given certificate, using the default policy for validation.
        /// </summary>
        /// <param name="certificate">The certificate to be trusted.</param>
        public void Trust(X509Certificate certificate)
        {
            Trust(certificate, null);
        }

        /// <summary>
        /// Trusts the given certificate, using the given policy for validation.
        /// </summary>
        /// <param name="certificate">The certificate to be trusted.</param>
        /// <param name="policy">The policy by which to evaluate the certificate.</param>
        public void Trust(X509Certificate certificate, CertificatePolicy policy)
        {
            m_trustedCertificates[certificate] = policy;
        }

        /// <summary>
        /// Removes the given certificate from the list of trusted certificates.
        /// </summary>
        /// <param name="certificate">The certificate to be distrusted.</param>
        public void Distrust(X509Certificate certificate)
        {
            m_trustedCertificates.Remove(certificate);
        }

        /// <summary>
        /// Removes all certificates from the list of trusted certificates.
        /// </summary>
        public void DistrustAll()
        {
            m_trustedCertificates.Clear();
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
            X509Certificate trustedCertificate = GetTrustedCertificate(remoteCertificate);
            X509ChainStatusFlags chainFlags;
            CertificatePolicy policy;

            ReasonForFailure = null;

            // If we cannot find a trusted certificate matching the remote
            // certificate, then remote certificate is not trusted
            if ((object)trustedCertificate == null)
            {
                ReasonForFailure = "No matching certificate found in the list of trusted certificates.";
                return false;
            }

            // Get the policy for the remote certificate
            policy = m_trustedCertificates[trustedCertificate] ?? DefaultCertificatePolicy;

            // If there were any errors, excepting the valid
            // policy errors, remote certificate is rejected
            if ((errors & ~policy.ValidPolicyErrors) != SslPolicyErrors.None)
            {
                ReasonForFailure = $"Policy errors encountered during validation: {errors & ~policy.ValidPolicyErrors}";
                return false;
            }

            // If an error is found at any part of the chain, excepting
            // valid chain flags, remote certificate is rejected
            chainFlags = chain.ChainStatus.Aggregate(X509ChainStatusFlags.NoError, (flags, status) => flags | (status.Status & ~policy.ValidChainFlags));

            if (chainFlags != X509ChainStatusFlags.NoError)
            {
                ReasonForFailure = $"Invalid chain flags found during validation: {chainFlags}";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Searches the list of trusted certificates for a certificate that matches the given remote certificate. 
        /// </summary>
        /// <param name="remoteCertificate">Remote certificate to search for.</param>
        /// <returns>Trusted X509 certificate, if found; otherwise, <c>null</c>.</returns>
        public X509Certificate GetTrustedCertificate(X509Certificate remoteCertificate)
        {
            byte[] hash, key;
            bool hashMatch, keyMatch;

            if ((object)remoteCertificate != null)
            {
                hash = remoteCertificate.GetCertHash();
                key = remoteCertificate.GetPublicKey();

                foreach (X509Certificate trustedCertificate in m_trustedCertificates.Keys)
                {
                    hashMatch = hash.SequenceEqual(trustedCertificate.GetCertHash());
                    keyMatch = hashMatch && key.SequenceEqual(trustedCertificate.GetPublicKey());

                    if (keyMatch)
                        return trustedCertificate;
                }
            }

            return null;
        }

        #endregion
    }
}
