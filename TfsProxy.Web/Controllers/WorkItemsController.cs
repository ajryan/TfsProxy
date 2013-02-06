using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using TfsProxy.Web.Authorization;
using TfsProxy.Web.Models;
using TfsProxy.Web.Tfs;

namespace TfsProxy.Web.Controllers
{
    [TfsBasicAuthentication]
    public class WorkItemsController : ApiController
    {
        // GET api/workitems
        public IEnumerable<WorkItemInfo> Get(string collectionId, string projectName, int page = 0, string workItemType = "All")
        {
            if (String.IsNullOrWhiteSpace(collectionId) || String.IsNullOrWhiteSpace(projectName))
            {
                throw new HttpResponseException(
                    new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("collectionUri and projectName are required") });
            }

            var configServer = (TfsConfigurationServer)HttpContext.Current.Items["TFS_CONFIG_SERVER"];
            var collection = configServer.GetTeamProjectCollection(new Guid(collectionId));

            var workItemStore = collection.GetService<WorkItemStore>();
            var wiql = String.Format("Select * From WorkItems Where [System.TeamProject] = '{0}'", projectName);
            if (!String.IsNullOrWhiteSpace(workItemType) &&
                !workItemType.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                wiql += String.Format(" AND [System.WorkItemType] = '{0}'", workItemType);
            }
            wiql += " order by [System.ChangedDate] desc, [System.CreatedDate] desc";
            var query = workItemStore.Query(wiql);

            return query
                .Cast<WorkItem>()
                .Skip(10 * page)
                .Take(10)
                .Select(WorkItemInfoBuilder.Build)
                .ToArray();
        }
    }
}
