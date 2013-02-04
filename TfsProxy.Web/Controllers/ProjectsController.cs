using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using TfsProxy.Web.Authorization;
using TfsProxy.Web.Models;

namespace TfsProxy.Web.Controllers
{
    [TfsBasicAuthentication]
    public class ProjectsController : ApiController
    {
        // GET api/projects
        public IEnumerable<TeamProjectInfo> Get()
        {
            var configServer = (TfsConfigurationServer) HttpContext.Current.Items["TFS_CONFIG_SERVER"];
            var collections = configServer.CatalogNode.QueryChildren(
                new Guid[] { CatalogResourceTypes.ProjectCollection }, false, CatalogQueryOptions.None);

            var projects = new List<TeamProjectInfo>();
            foreach (var collectionNode in collections)
            {
                var collectionId = new Guid(collectionNode.Resource.Properties["InstanceId"]);
                var collection = configServer.GetTeamProjectCollection(collectionId);
                var workItemStore = collection.GetService<WorkItemStore>();
                foreach (Project project in workItemStore.Projects)
                {
                    if (project.HasWorkItemReadRights)
                    {
                        string collectionUri = collection.Name.Replace('\\', '/');
                        
                        string collectionName = collection.Name.Substring(collectionUri.LastIndexOf('/') + 1);
                        collectionName = HttpUtility.UrlDecode(collectionName);

                        var projectInfo = new TeamProjectInfo
                        {
                            CollectionName = collectionName,
                            CollectionId = collectionId.ToString(),
                            ProjectName = project.Name,
                            ProjectUri = project.Uri.ToString(),
                            WorkItemTypes = project.WorkItemTypes
                                .Cast<WorkItemType>()
                                .Select(wit => wit.Name)
                                .OrderBy(name => name)
                                .ToList()
                        };

                        projects.Add(projectInfo);
                    }
                }
            }

            return projects
                .OrderBy(p => p.CollectionName)
                .ThenBy(p => p.ProjectName);
        }
    }
}
