using System;
using System.Net;
using Microsoft.TeamFoundation.Client;

namespace TfsProxy.Web.Authorization
{
    public class NetworkCredentialsProvider : ICredentialsProvider
    {
        private readonly NetworkCredential _networkCredential;

        public NetworkCredentialsProvider(NetworkCredential networkCredential)
        {
            _networkCredential = networkCredential;
        }

        public ICredentials GetCredentials(Uri uri, ICredentials failedCredentials)
        {
            return _networkCredential;
        }

        public void NotifyCredentialsAuthenticated(Uri uri)
        {
        }
    }
}