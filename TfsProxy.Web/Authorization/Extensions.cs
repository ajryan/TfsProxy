using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Web;

namespace TfsProxy.Web.Authorization
{
    public static class Extensions
    {
        public static string GetTfsUrl(this HttpRequestHeaders headers)
        {
            IEnumerable<string> tfsUrlValues;
            if (headers.TryGetValues("TfsUrl", out tfsUrlValues))
                return tfsUrlValues.FirstOrDefault();
            return null;
        }

        public static void SetCookie(this HttpResponseHeaders headers, Cookie cookie)
        {
            var cookieBuilder = new StringBuilder(
                HttpUtility.UrlEncode(cookie.Name) + "=" + HttpUtility.UrlEncode(cookie.Value));
            
            if (cookie.HttpOnly)
                cookieBuilder.Append("; HttpOnly");

            if (cookie.Secure)
                cookieBuilder.Append("; Secure");

            headers.Add("Set-Cookie", cookieBuilder.ToString());
        }
    }
}