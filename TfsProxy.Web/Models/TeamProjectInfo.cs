using System;
using System.Collections.Generic;

namespace TfsProxy.Web.Models
{
    public class TeamProjectInfo
    {
        public string CollectionName { get; set; }
        public string CollectionId { get; set; }
        public string ProjectName { get; set; }
        public string ProjectUri { get; set; }
        public List<string> WorkItemTypes { get; set; }
    }
}