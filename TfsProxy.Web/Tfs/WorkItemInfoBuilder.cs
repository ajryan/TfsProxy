using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using TfsProxy.Web.Models;

namespace TfsProxy.Web.Tfs
{
    public class WorkItemInfoBuilder
    {
        public static WorkItemInfo Build(WorkItem wi)
        {
            return new WorkItemInfo
            {
                WorkItemType = wi.Type.Name,
                Id = wi.Id,
                Title = wi.Title,
                AssignedTo = wi.Fields["System.AssignedTo"].Value.ToString(),
                State = wi.State,
                Reason = wi.Reason,
                Area = wi.AreaPath,
                Iteration = wi.IterationPath,
                CreatedDate = wi.CreatedDate,
                ChangedDate = wi.ChangedDate,
                OtherFields = GetOtherFields(wi),
                Description = GetDescription(wi),
                History = GetHistory(wi)
            };
        }

        private static readonly string[] _ExcludeOtherFields =
        {
            "Title", "State", "Reason", "Assigned To", "Work Item Type", "Description", 
            "HTMLDescription", "DescriptionHTML", "History", "Iteration Path", "Iteration ID", "Team Project",
            "Node Name", "Area Path", "Changed Date", "ID", "Area ID"
        };
        private static List<WorkItemFieldInfo> GetOtherFields(WorkItem workItem)
        {
            return workItem.Fields
                .Cast<Field>()
                .Where(f => !_ExcludeOtherFields.Contains(f.Name))
                .Select(
                    field => new WorkItemFieldInfo
                    {
                        Name = field.Name,
                        Value = (field.Value != null) ? field.Value.ToString() : ""
                    })
                .Where(fi => !String.IsNullOrWhiteSpace(fi.Value) && fi.Value != "0")
                .OrderBy(fi => fi.Name)
                .ToList();
        }

        private static readonly string[] _DescriptionFields = { "HTMLDescription", "DescriptionHTML", "Description" };
        private static string GetDescription(WorkItem workItem)
        {
            foreach (string field in _DescriptionFields)
            {
                if (workItem.Fields.Contains(field))
                {
                    string description = workItem.Fields[field].Value.ToString();
                    if (!String.IsNullOrWhiteSpace(description))
                        return description;
                }
            }
            return workItem.Description;
        }

        private static string GetHistory(WorkItem workItem)
        {
            var historyBuilder = new StringBuilder();
            foreach (var rev in workItem.Revisions.Cast<Revision>().OrderByDescending(r => r.Index))
            {
                historyBuilder.AppendFormat("{0} {1}\r\n", rev.Fields["Changed Date"].Value, rev.GetTagLine());
                historyBuilder.AppendLine(rev.Fields["History"].Value.ToString());
                historyBuilder.AppendLine();

            }
            return historyBuilder.ToString();
        }
    }
}