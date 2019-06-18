﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Instance
{
    internal class AadOpenIdConfigurationEndpointManager : IOpenIdConfigurationEndpointManager
    {
        private readonly IServiceBundle _serviceBundle;

        public AadOpenIdConfigurationEndpointManager(IServiceBundle serviceBundle)
        {
            _serviceBundle = serviceBundle;
        }

        /// <inheritdoc />
        public async Task<string> ValidateAuthorityAndGetOpenIdDiscoveryEndpointAsync(
            AuthorityInfo authorityInfo,
            string userPrincipalName,
            RequestContext requestContext)
        {
            var authorityUri = new Uri(authorityInfo.CanonicalAuthority);
            if (authorityInfo.ValidateAuthority && !AadAuthority.IsInTrustedHostList(authorityUri.Host))
            {
                // MSAL will throw if the instance discovery URI does not respond with a valid json
                await _serviceBundle.InstanceDiscoveryManager.GetMetadataEntryAsync(
                                             authorityUri,
                                             requestContext).ConfigureAwait(false);
            }

            return authorityInfo.CanonicalAuthority + Constants.OpenIdConfigurationEndpoint;
        }
    }
}
