using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Principal;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.Security;
using Microsoft.TeamFoundation.Client;

namespace TfsProxy.Web.Authorization
{
    public class UserDataPrincipal : IPrincipal
    {
        public string TfsUrl { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        public UserDataPrincipal()
        {
            // For deserialization
        }

        public UserDataPrincipal(string tfsUrl, string userName, string password)
        {
            TfsUrl = tfsUrl;
            UserName = userName;
            Password = password;
        }

        public static UserDataPrincipal Current
        {
            get { return HttpContext.Current.User as UserDataPrincipal; }
        }

        public static UserDataPrincipal InitFromAuthCookie(HttpRequestHeaders headers)
        {
            string authCookieName = FormsAuthentication.FormsCookieName;

            var cookieValues = headers.GetCookies(authCookieName);
            CookieHeaderValue authCookieValue = cookieValues.FirstOrDefault();
            if (authCookieValue == null)
                return null;

            CookieState authCookie = authCookieValue[authCookieName];
            return DecryptAuthTicket(authCookie.Value);
        }

        public static UserDataPrincipal InitFromAuthCookie(HttpContextBase httpContext)
        {
            string authCookieName = FormsAuthentication.FormsCookieName;

            if (!httpContext.User.Identity.IsAuthenticated ||
                httpContext.Request.Cookies == null ||
                httpContext.Request.Cookies[authCookieName] == null)
            {
                return null;
            }

            var authCookie = httpContext.Request.Cookies[authCookieName];
            return DecryptAuthTicket(authCookie.Value);
        }

        private static UserDataPrincipal DecryptAuthTicket(string cookieValue)
        {
            var authTicket = FormsAuthentication.Decrypt(cookieValue);

            var principal = new JavaScriptSerializer().Deserialize<UserDataPrincipal>(authTicket.UserData);
            return principal;
        }

        public static UserDataPrincipal InitFromHeaders(HttpRequestHeaders headers)
        {
            var authHeader = headers.Authorization;
            var tfsUrl = headers.GetTfsUrl();
            if (authHeader == null || 
                !authHeader.Scheme.Equals("basic", StringComparison.OrdinalIgnoreCase) ||
                String.IsNullOrWhiteSpace(tfsUrl))
            {
                return null;
            }

            string userName = null;
            string password = null;

            string decodedAuthHeader = Encoding.Default.GetString(Convert.FromBase64String(authHeader.Parameter));
            var usernamePasswordTokens = decodedAuthHeader.Split(new[] { ':' }, 2, StringSplitOptions.RemoveEmptyEntries);
            if (usernamePasswordTokens.Length >= 2)
            {
                userName = usernamePasswordTokens[0];
                password = usernamePasswordTokens[1];
            }

            if (String.IsNullOrWhiteSpace(tfsUrl) || String.IsNullOrWhiteSpace(userName) || String.IsNullOrWhiteSpace(password))
                return null;

            var principal = new UserDataPrincipal(tfsUrl, userName, password);
            return principal;
        }

        public ICredentialsProvider GetCredentialsProvider()
        {
            var uri = new Uri(TfsUrl);

            bool tfsService =
                uri.Host.EndsWith("tfspreview.com", StringComparison.OrdinalIgnoreCase) ||
                uri.Host.EndsWith("visualstudio.com", StringComparison.OrdinalIgnoreCase);

            if (tfsService)
            {
                return new ServiceIdentityCredentialsProvider(UserName, Password);
            }
            
            var userNameTokens = UserName.Split(new[] { '\\' }, 2, StringSplitOptions.RemoveEmptyEntries);
            var credential = userNameTokens.Length > 1
                ? new NetworkCredential(userNameTokens[1], Password, userNameTokens[0])
                : new NetworkCredential(UserName, Password);

            return new NetworkCredentialsProvider(credential);
        }

        public HttpCookie CreateAuthCookie()
        {
            string cookieName = TfsUrl + "_" + UserName;

            var authTicket = new FormsAuthenticationTicket(
                2,
                cookieName,
                DateTime.Now,
                DateTime.Now.AddMinutes(FormsAuthentication.Timeout.TotalMinutes),
                true, // TODO: inject "remember me" checkbox from form
                new JavaScriptSerializer().Serialize(this));

            var authCookie = new HttpCookie(
                FormsAuthentication.FormsCookieName,
                FormsAuthentication.Encrypt(authTicket))
            {
                HttpOnly = true
            };

            return authCookie;
        }

        public IIdentity Identity { get; private set; }

        public bool IsInRole(string role)
        {
            throw new NotImplementedException();
        }
    }
}