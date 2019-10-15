﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommonCache.Test.Common;
using Microsoft.Identity.Client;

namespace CommonCache.Test.MsalV2
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            new MsalV2CacheExecutor().Execute(args);
        }

        private class MsalV2CacheExecutor : AbstractCacheExecutor
        {
            /// <inheritdoc />
            protected override async Task<IEnumerable<CacheExecutorAccountResult>> InternalExecuteAsync(TestInputData testInputData)
            {
                var v1App = PreRegisteredApps.CommonCacheTestV1;
                string resource = PreRegisteredApps.MsGraph;
                string[] scopes = new[]
                {
                    resource + "/user.read"
                };

                Logger.LogCallback = (LogLevel level, string message, bool containsPii) =>
                {
                    Console.WriteLine("{0}: {1}", level, message);
                };


                var tokenCache = new TokenCache();

                FileBasedTokenCacheHelper.ConfigureUserCache(
                    testInputData.StorageType,
                    tokenCache,
                    CommonCacheTestUtils.AdalV3CacheFilePath,
                    CommonCacheTestUtils.MsalV2CacheFilePath);

                var app = new PublicClientApplication(v1App.ClientId, v1App.Authority, tokenCache)
                {
                    ValidateAuthority = true
                };

                IEnumerable<IAccount> accounts = await app.GetAccountsAsync().ConfigureAwait(false);
                var results = new List<CacheExecutorAccountResult>();

                foreach (var labUserData in testInputData.LabUserDatas)
                {
                    IAccount accountToReference = accounts.FirstOrDefault(x => x.Username.Equals(
                        labUserData.User.Upn, StringComparison.OrdinalIgnoreCase));
                    try
                    {
                        var result = await app.AcquireTokenSilentAsync(
                            scopes,
                            accountToReference,
                            app.Authority,
                            false).ConfigureAwait(false);

                        Console.WriteLine($"got token for '{result.Account.Username}' from the cache");
                        results.Add(new CacheExecutorAccountResult(labUserData.User.Upn, result.Account.Username, true));
                    }
                    catch (MsalUiRequiredException)
                    {
                        var result = await app.AcquireTokenByUsernamePasswordAsync(
                            scopes,
                            labUserData.User.Upn,
                            labUserData.Password.ToSecureString()).ConfigureAwait(false);

                        if (string.IsNullOrWhiteSpace(result.AccessToken))
                        {
                            results.Add(new CacheExecutorAccountResult(labUserData.User.Upn, string.Empty, false));
                        }
                        else
                        {
                            Console.WriteLine($"got token for '{result.Account.Username}' without the cache");
                            results.Add(new CacheExecutorAccountResult(labUserData.User.Upn, result.Account.Username, false));
                        }
                    }
                }

                return results;
            }
        }
    }
}
