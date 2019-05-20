﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Test.Unit;
using System.Globalization;
using Microsoft.Identity.Test.UIAutomation.Infrastructure;
using System.IO;

namespace Microsoft.Identity.Test.Integration.SeleniumTests
{
    [TestClass]
    public class InteractiveFlowTests
    {
        private readonly TimeSpan _interactiveAuthTimeout = TimeSpan.FromMinutes(1);
        private static readonly string[] s_scopes = new[] { "user.read" };

        #region MSTest Hooks
        /// <summary>
        /// Initialized by MSTest (do not make private or readonly)
        /// </summary>
        public TestContext TestContext { get; set; }

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
        }

        #endregion

        [TestMethod]
        public async Task Interactive_AADAsync()
        {
            // Arrange
            LabResponse labResponse = LabUserHelper.GetDefaultUser();
            await RunTestForUserAsync(labResponse).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Interactive_AdfsV3_NotFederatedAsync()
        {
            // Arrange
            UserQuery query = new UserQuery
            {
                FederationProvider = FederationProvider.AdfsV4,
                IsMamUser = false,
                IsMfaUser = false,
                IsFederatedUser = false
            };

            LabResponse labResponse = LabUserHelper.GetLabUserData(query);
            await RunTestForUserAsync(labResponse).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Interactive_AdfsV3_FederatedAsync()
        {
            // Arrange
            UserQuery query = new UserQuery
            {
                FederationProvider = FederationProvider.AdfsV4,
                IsMamUser = false,
                IsMfaUser = false,
                IsFederatedUser = true
            };

            LabResponse labResponse = LabUserHelper.GetLabUserData(query);
            await RunTestForUserAsync(labResponse).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Interactive_AdfsV2_FederatedAsync()
        {
            // Arrange
            UserQuery query = new UserQuery
            {
                FederationProvider = FederationProvider.AdfsV2,
                IsMamUser = false,
                IsMfaUser = false,
                IsFederatedUser = true
            };


            LabResponse labResponse = LabUserHelper.GetLabUserData(query);
            await RunTestForUserAsync(labResponse).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Interactive_AdfsV4_NotFederatedAsync()
        {
            // Arrange
            UserQuery query = new UserQuery
            {
                FederationProvider = FederationProvider.AdfsV4,
                IsMamUser = false,
                IsMfaUser = false,
                IsFederatedUser = false
            };

            LabResponse labResponse = LabUserHelper.GetLabUserData(query);
            await RunTestForUserAsync(labResponse).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Interactive_AdfsV4_FederatedAsync()
        {
            // Arrange
            UserQuery query = new UserQuery
            {
                FederationProvider = FederationProvider.AdfsV4,
                IsMamUser = false,
                IsMfaUser = false,
                IsFederatedUser = true
            };

            LabResponse labResponse = LabUserHelper.GetLabUserData(query);
            await RunTestForUserAsync(labResponse).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task InteractiveConsentPromptAsync()
        {
            var labResponse = LabUserHelper.GetDefaultUser();

            await RunPromptTestForUserAsync(labResponse, Prompt.Consent, true).ConfigureAwait(false);
            await RunPromptTestForUserAsync(labResponse, Prompt.Consent, false).ConfigureAwait(false);
        }

        private async Task RunPromptTestForUserAsync(LabResponse labResponse, Prompt prompt, bool useLoginHint)
        {
            var pca = PublicClientApplicationBuilder
               .Create(labResponse.AppId)
               .WithRedirectUri(SeleniumWebUI.FindFreeLocalhostRedirectUri())
               .Build();

            AcquireTokenInteractiveParameterBuilder builder = pca
               .AcquireTokenInteractive(s_scopes)
               .WithPrompt(prompt)
               .WithCustomWebUi(CreateSeleniumCustomWebUI(labResponse.User, prompt, TestContext, useLoginHint));

            if (useLoginHint)
            {
                builder = builder.WithLoginHint(labResponse.User.Upn);
            }

            AuthenticationResult result = await builder
               .ExecuteAsync(new CancellationTokenSource(_interactiveAuthTimeout).Token)
               .ConfigureAwait(false);

            await MsalAssert.AssertSingleAccountAsync(labResponse, pca, result).ConfigureAwait(false);

        }

		[TestMethod]
        public async Task Interactive_AdfsV2019_NotFederatedAsync()
        {
            // Arrange
            UserQuery query = new UserQuery
            {
                FederationProvider = FederationProvider.ADFSv2019,
                IsMamUser = false,
                IsMfaUser = false,
                IsFederatedUser = false
            };

            LabResponse labResponse = LabUserHelper.GetLabUserData(query);
            await RunTestForUserAsync(labResponse).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Interactive_AdfsV2019_FederatedAsync()
        {
            // Arrange
            UserQuery query = new UserQuery
            {
                FederationProvider = FederationProvider.ADFSv2019,
                IsMamUser = false,
                IsMfaUser = false,
                IsFederatedUser = true
            };

            LabResponse labResponse = LabUserHelper.GetLabUserData(query);
            await RunTestForUserAsync(labResponse).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Interactive_AdfsV2019_DirectAsync()
        {
            // Arrange
            UserQuery query = new UserQuery
            {
                FederationProvider = FederationProvider.ADFSv2019,
                IsMamUser = false,
                IsMfaUser = false,
                IsFederatedUser = true
            };

            LabResponse labResponse = LabUserHelper.GetLabUserData(query);
            await RunTestForUserAsync(labResponse, true).ConfigureAwait(false);
        }

        [TestMethod]
        [Ignore("Lab needs a way to provide multiple account types(AAD, ADFS, MSA) that can sign into the same client id")]
        public async Task MultiUserCacheCompatabilityTestAsync()
        {
            // Arrange
            //cache = new TokenCache();

            //Acquire AT for default lab account
            LabResponse labResponseDefault = LabUserHelper.GetDefaultUser();
            AuthenticationResult defaultAccountResult = await RunTestForUserAsync(labResponseDefault).ConfigureAwait(false);

            //Acquire AT for ADFS 2019 account
            UserQuery federatedUserquery = new UserQuery
            {
                FederationProvider = FederationProvider.ADFSv2019,
                IsMamUser = false,
                IsMfaUser = false,
                IsFederatedUser = true
            };

            LabResponse labResponseFederated = LabUserHelper.GetLabUserData(federatedUserquery);
            var federatedAccountResult = await RunTestForUserAsync(labResponseFederated, false).ConfigureAwait(false);

            //Acquire AT for MSA account
            LabResponse labResponseMsa = LabUserHelper.GetB2CMSAAccount();
            labResponseMsa.AppId = LabApiConstants.MSAOutlookAccountClientID;
            var msaAccountResult = await RunTestForUserAsync(labResponseMsa).ConfigureAwait(false);

            PublicClientApplication pca = PublicClientApplicationBuilder.Create(labResponseDefault.AppId).BuildConcrete();

            AuthenticationResult authResult = await pca.AcquireTokenSilent(new[] { CoreUiTestConstants.DefaultScope }, defaultAccountResult.Account)
                                                       .ExecuteAsync()
                                                       .ConfigureAwait(false);
            Assert.IsNotNull(authResult);
            Assert.IsNotNull(authResult.AccessToken);
            Assert.IsNotNull(authResult.IdToken);

            pca = PublicClientApplicationBuilder.Create(labResponseFederated.AppId).BuildConcrete();

            authResult = await pca.AcquireTokenSilent(new[] { CoreUiTestConstants.DefaultScope }, federatedAccountResult.Account)
                                  .ExecuteAsync()
                                  .ConfigureAwait(false);
            Assert.IsNotNull(authResult);
            Assert.IsNotNull(authResult.AccessToken);
            Assert.IsNull(authResult.IdToken);

            pca = PublicClientApplicationBuilder.Create(LabApiConstants.MSAOutlookAccountClientID).BuildConcrete();

            authResult = await pca.AcquireTokenSilent(new[] { CoreUiTestConstants.DefaultScope }, msaAccountResult.Account)
                                  .ExecuteAsync()
                                  .ConfigureAwait(false);
            Assert.IsNotNull(authResult);
            Assert.IsNotNull(authResult.AccessToken);
            Assert.IsNull(authResult.IdToken);
        }

        private async Task<AuthenticationResult> RunTestForUserAsync(LabResponse labResponse, bool directToAdfs = false)
        {
            IPublicClientApplication pca;
            if(directToAdfs)
            {
                pca = PublicClientApplicationBuilder
					.Create(Adfs2019LabConstants.PublicClientId)
                    .WithRedirectUri(Adfs2019LabConstants.ClientRedirectUri)
                    .WithAdfsAuthority(Adfs2019LabConstants.Authority)
                    .BuildConcrete();
            }
            else
            {
            	pca = PublicClientApplicationBuilder
                	.Create(labResponse.AppId)
                	.WithRedirectUri(SeleniumWebUI.FindFreeLocalhostRedirectUri())
                	.Build();
			}
            Trace.WriteLine("Part 1 - Acquire a token interactively, no login hint");
            AuthenticationResult result = await pca
                .AcquireTokenInteractive(s_scopes)
                .WithCustomWebUi(CreateSeleniumCustomWebUI(labResponse.User, Prompt.SelectAccount, TestContext, false, directToAdfs))
                .ExecuteAsync(new CancellationTokenSource(_interactiveAuthTimeout).Token)
                .ConfigureAwait(false);

            IAccount account = await MsalAssert.AssertSingleAccountAsync(labResponse, pca, result).ConfigureAwait(false);

            Trace.WriteLine("Part 2 - Clear the cache");
            await pca.RemoveAsync(account).ConfigureAwait(false);
            Assert.IsFalse((await pca.GetAccountsAsync().ConfigureAwait(false)).Any());

            Trace.WriteLine("Part 3 - Acquire a token interactively again, with login hint");
            result = await pca
                .AcquireTokenInteractive(s_scopes)
                .WithCustomWebUi(CreateSeleniumCustomWebUI(labResponse.User, Prompt.ForceLogin, TestContext, true, directToAdfs))
                .WithPrompt(Prompt.ForceLogin)
                .WithLoginHint(labResponse.User.HomeUPN)
                .ExecuteAsync(new CancellationTokenSource(_interactiveAuthTimeout).Token)
                .ConfigureAwait(false);

            account = await MsalAssert.AssertSingleAccountAsync(labResponse, pca, result).ConfigureAwait(false);

            Trace.WriteLine("Part 4 - Acquire a token silently");
            result = await pca
                .AcquireTokenSilent(s_scopes, account)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            await MsalAssert.AssertSingleAccountAsync(labResponse, pca, result).ConfigureAwait(false);
        	return result;
		}

        private static SeleniumWebUI CreateSeleniumCustomWebUI(LabUser user, Prompt prompt, TestContext context, bool withLoginHint, bool adfsOnly = false)
        {
            return new SeleniumWebUI((driver) =>
            {
                Trace.WriteLine("Starting Selenium automation");
                try
                {
                    driver.PerformLogin(user, prompt, withLoginHint, adfsOnly);
                }
                catch (Exception ex)
                {
                    //TODO: There is a bug on .net core that prevents the TestContext.AddResultFile() method from being used. It will be availabe
                    //in the next version of the MSTest.TestAdapter & MSTest.TestFramework nuget packages. Simply updating to 2.x when it is
                    //released should resolve this. bug tracking here (https://github.com/Microsoft/testfx/issues/394)
#if DESKTOP
                    var htmlDump = driver.PageSource;
                    string htmlDumpPath = Directory.GetCurrentDirectory() + context.TestName + "-HTMLDUMP.txt";
                    File.Create(htmlDumpPath);

                    using (StreamWriter output = new StreamWriter(htmlDumpPath))
                    {
                        output.Write(htmlDump);
                    }

                    context.AddResultFile(htmlDumpPath);

                    driver.SaveScreenshot(context);
#endif
                    throw ex;
                }
            });
        }
    }
}
