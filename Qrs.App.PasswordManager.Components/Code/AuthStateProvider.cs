using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;

namespace Qrs.App.PasswordManager.Components.Code
{
    public class AuthStateProvider : AuthenticationStateProvider
    {
        private ApplicationService Appl;
        public AuthStateProvider(ApplicationService appl)
        {
            Appl = appl;
            Appl.RegisterAuthStateProvider(this);
        }

        public async override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            
                var loggedIn = new ClaimsIdentity(Appl.Context?.Folder?.FolderPath ?? "unknown_identity");
                return await Task.FromResult(new AuthenticationState(new ClaimsPrincipal(loggedIn)));
            

        }

        public void AuthChanged()
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

    }
}
