using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TfsProxy.Web.Models
{
    [KnownType(typeof (WorkItemFieldInfo))]
    public class WorkItemInfo
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string AssignedTo { get; set; }
        public string Description { get; set; }
        public string WorkItemType { get; set; }
        public string State { get; set; }
        public string Reason { get; set; }
        public string Area { get; set; }
        public string Iteration { get; set; }
        public string History { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? ChangedDate { get; set; }
        public List<WorkItemFieldInfo> OtherFields { get; set ;}
    }
}