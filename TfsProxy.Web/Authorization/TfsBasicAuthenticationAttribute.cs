using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Web.Security;
using Microsoft.TeamFoundation;
using Microsoft.TeamFoundation.Client;

namespace TfsProxy.Web.Authorization
{
    public class TfsBasicAuthenticationAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            bool fromCookie = false;
            var userDataPrincipal = HttpContext.Current.User as UserDataPrincipal;
            if (userDataPrincipal == null)
            {
                userDataPrincipal = UserDataPrincipal.InitFromHeaders(actionContext.Request.Headers);
            }
            if (userDataPrincipal == null)
            {
                userDataPrincipal = UserDataPrincipal.InitFromAuthCookie(actionContext.Request.Headers);
                fromCookie = true;
            }

            if (userDataPrincipal == null)
            {
                SetUnauthorizedResponse(actionContext);
                return;
            }

            try
            {
                var configUri = new Uri(userDataPrincipal.TfsUrl);

                var provider = userDataPrincipal.GetCredentialsProvider();
                var tfsConfigServer = new TfsConfigurationServer(configUri, provider.GetCredentials(null, null), provider);

                tfsConfigServer.EnsureAuthenticated();

                HttpContext.Current.Items["TFS_CONFIG_SERVER"] = tfsConfigServer;
            }
            catch (TeamFoundationServerUnauthorizedException ex)
            {
                SetUnauthorizedResponse(actionContext, ex.Message, fromCookie);
                return;
            }

            HttpContext.Current.User = userDataPrincipal;

            base.OnActionExecuting(actionContext);
        }

        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            var currentUser = HttpContext.Current.User as UserDataPrincipal;
            if (currentUser != null && actionExecutedContext.Response != null)
            {
                var authCookie = currentUser.CreateAuthCookie();
                var cookieHeaderValue = new CookieHeaderValue(authCookie.Name, authCookie.Value);
                actionExecutedContext.Response.Headers.AddCookies(new[] { cookieHeaderValue });
            }

            base.OnActionExecuted(actionExecutedContext);
        }

        private void SetUnauthorizedResponse(HttpActionContext actionContext, string message = null, bool removeAuthCookie = false)
        {
            string messageValue = message ?? "Authorization (Basic) and TfsUrl headers are required.";
            actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Unauthorized, new { Message = messageValue });

            string tfsUrl = actionContext.Request.Headers.GetTfsUrl();
            if (!String.IsNullOrWhiteSpace(tfsUrl))
            {
                actionContext.Response.Headers.Add(
                    "WWW-Authenticate", 
                    String.Format("Basic realm=\"{0}\"", tfsUrl));
            }

            if (removeAuthCookie)
            {
                var authTicket = new FormsAuthenticationTicket(
                    1, "", DateTime.Now, DateTime.Now.AddMinutes(-30), false, "", FormsAuthentication.FormsCookiePath);
                actionContext.Response.Headers.SetCookie(new Cookie(FormsAuthentication.FormsCookieName, FormsAuthentication.Encrypt(authTicket)));
            }
        }
    }
}