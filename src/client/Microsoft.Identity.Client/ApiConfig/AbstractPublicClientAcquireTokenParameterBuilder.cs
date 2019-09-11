﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Executors;
using Microsoft.Identity.Client.PoP;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Base class for public client application token request builders
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class AbstractPublicClientAcquireTokenParameterBuilder<T>
        : AbstractAcquireTokenParameterBuilder<T>
        where T : AbstractAcquireTokenParameterBuilder<T>
    {
        internal AbstractPublicClientAcquireTokenParameterBuilder(IPublicClientApplicationExecutor publicClientApplicationExecutor)
        {
            PublicClientApplicationExecutor = publicClientApplicationExecutor;
        }

        /// <summary>
        ///  Modifies the token acquisition request so that the acquired token is a Proof of Possession token (PoP), rather than a Bearer token. 
        ///  A key/pair is generated by MSAL.NET. See https://aka.ms/msal-net-pop
        /// </summary>
        /// <param name="httpRequestMessage">An http request message for which a Proof of Possesion token is acquired</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item> Developers are still responsible for adding an HTTP header with the token to the request. See <seealso cref="AuthenticationResult.CreateAuthorizationHeader"/> for details.</item>
        /// <item> The PoP token is bound to the HTTP request, more specifically to the HTTP method (GET or POST) and to the Uri (path and query). </item>
        /// </list>
        /// </remarks>
        internal AbstractPublicClientAcquireTokenParameterBuilder<T> WithPoPAuthenticationScheme(HttpRequestMessage httpRequestMessage) 
        {
            CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithPoPScheme);
            CommonParameters.AuthenticationScheme = new PoPAuthenticationScheme(httpRequestMessage, null); // TODO add a default crypto provider

            return this;
        }

        // Allows testing the PoP flow with any crypto. Consider making this public.
        internal AbstractPublicClientAcquireTokenParameterBuilder<T> WithPoPAuthenticationScheme(HttpRequestMessage httpRequestMessage, IPoPCryptoProvider popCryptoProvider) 
        {
            CommonParameters.AddApiTelemetryFeature(ApiTelemetryFeature.WithPoPScheme);
            CommonParameters.AuthenticationScheme = new PoPAuthenticationScheme(httpRequestMessage, popCryptoProvider); 

            return this;
        }



        internal abstract Task<AuthenticationResult> ExecuteInternalAsync(CancellationToken cancellationToken);

        /// <inheritdoc />
        public override Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            ValidateAndCalculateApiId();
            return ExecuteInternalAsync(cancellationToken);
        }

        /// <summary>
        /// </summary>
        internal IPublicClientApplicationExecutor PublicClientApplicationExecutor { get; }
    }
}
