﻿namespace App.Security.Owin.UserNamePwd
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Owin.Security;
    using Microsoft.Owin.Security.Infrastructure;
    using System.Collections.Generic;
    using System.Security.Claims;
    using Microsoft.Owin;
    using Common;
    using System.Linq;
    using Common.Command;
    using Aggregate;
    using Command.UserNameAndPwd;

    internal class OwinAuthenticationHandler : AuthenticationHandler<UserNamePwd.UserNamePwdAuthOptions>
    {
        protected async override Task<AuthenticationTicket> AuthenticateCoreAsync()
        {
            UserNameAndPwdAuthenticationResult authorise = this.Authorise(this.Request.Headers);
            if (authorise == null || !authorise.IsValid)
            {
                return null;
            }
            AuthenticationProperties authProperties = new AuthenticationProperties();
            authProperties.IssuedUtc = DateTime.UtcNow;
            authProperties.ExpiresUtc = authorise.ExpiredAfter;
            authProperties.AllowRefresh = true;
            authProperties.IsPersistent = true;
            IList<Claim> claimCollection = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, authorise.FullName),
                    new Claim(ClaimTypes.Email, authorise.Email),
                    new Claim(ClaimTypes.Expired, authorise.ExpiredAfter.ToString()),
                };
            ClaimsIdentity claimsIdentity = new ClaimsIdentity(claimCollection, "Custom");
            AuthenticationTicket ticket = new AuthenticationTicket(claimsIdentity, authProperties);
            return ticket;
        }

        private UserNameAndPwdAuthenticationResult Authorise(IHeaderDictionary headers)
        {
            string[] acceptLanguageValues;
            bool acceptLanguageHeaderPresent = headers.TryGetValue(Constants.AUTHENTICATION_TOKEN, out acceptLanguageValues);
            if (!acceptLanguageHeaderPresent)
            {
                return null;
            }
            string[] elementsInHeader = acceptLanguageValues.ToList()[0].Split(new string[] { Constants.AUTHENTICATION_TOKEN_SEPERATOR }, StringSplitOptions.RemoveEmptyEntries);

            ICommandHandlerStrategy commandHandlerStrategy = CommandHandlerStrategyFactory.Create<User>();
            UserNameAndPwdAuthenticationRequest request = new UserNameAndPwdAuthenticationRequest(elementsInHeader[0], elementsInHeader[1]);
            commandHandlerStrategy.Execute(request);
            return request.Result;
        }
    }
}