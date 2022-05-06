using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdentityProtectionMonitoring.Models
{
    // A class representing a ticket returned from the Autotask API
    public class AutotaskTicket
    {
        public int Id { get; set; }
        public int? AssignedResourceID { get; set; }
        public int? AssignedResourceRoleID { get; set; }
        public int? BillingCodeID { get; set; }
        public int CompanyID { get; set; }
        public int? CompanyLocationID { get; set; }
        public int? CompletedByResourceID { get; set; }
        public DateTime? CompletedDate { get; set; }
        public int? ConfigurationItemID { get; set; }
        public int? ContactID { get; set; }
        public int? ContractID { get; set; }
        public DateTime? CreateDate { get; set; }
        public int? CreatedByContactID { get; set; }
        public int? CreatorResourceID { get; set; }
        public int? CreatorType { get; set; }
        public string? Description { get; set; }
        public DateTime DueDateTime { get; set; }
        public int? FirstResponseAssignedResourceID { get; set; }
        public DateTime? FirstResponseDateTime { get; set; }
        public DateTime? FirstResponseDueDateTime { get; set; }
        public int? FirstResponseInitiatingResourceID { get; set; }
        public int? IssueType { get; set; }
        public DateTime? LastActivityDate { get; set; }
        public int? LastActivityPersonType { get; set; }
        public int? LastActivityResourceID { get; set; }
        public DateTime? LastCustomerNotificationDateTime { get; set; }
        public DateTime? LastCustomerVisibleActivityDateTime { get; set; }
        public DateTime? LastTrackedModificationDateTime { get; set; }
        public int Priority { get; set; }
        public int? QueueID { get; set; }
        public string? Resolution { get; set; }
        public DateTime? ResolutionPlanDateTime { get; set; }
        public DateTime? ResolutionPlanDueDateTime { get; set; }
        public DateTime? ResolvedDateTime { get; set; }
        public DateTime? ResolvedDueDateTime { get; set; }
        public bool? ServiceLevelAgreementHasBeenMet { get; set; }
        public int? ServiceLevelAgreementID { get; set; }
        public int? Source { get; set; }
        public int Status { get; set; }
        public int? SubIssueType { get; set; }
        public int? TicketCategory { get; set; }
        public string? TicketNumber { get; set; }
        public int? TicketType { get; set; }
        public string Title { get; set; }
        public IList<AutotaskUserDefinedField>? UserDefinedFields { get; set; }
    }

    public class AutotaskNewTicket
    {
        public int CompanyID { get; set; }
        public int? CompanyLocationID { get; set; }
        public int Priority { get; set; }
        public int Status { get; set; }
        public int? QueueID { get; set; }
        public int? IssueType { get; set; }
        public int? SubIssueType { get; set; }
        public int? ServiceLevelAgreementID { get; set; }
        public int? ContractID { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
    }

    public class AutotaskUpdateTicketStatus
    {
        public int Id { get; set; }
        public int Status { get; set; }
    }



    public class AutotaskTicketsList
    {
        public List<AutotaskTicket>? Items { get; set; }
        public AutotaskTicket? Item { get; set; }
        public AutotaskPageDetails? PageDetails { get; set; }
    }
}
