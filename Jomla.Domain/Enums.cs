namespace Jomla.Domain
{
    public enum UserRole { Buyer, Supplier }

    public enum SupplierOfferStatus { Active, Inactive, Expired }

    public enum BatchStatus { Open, Completed, Failed }

    public enum GroupRequestStatus { Active, Inactive, Closed }

    public enum GroupRequestOfferStatus { Open, Accepted, Countered, Expired }

    public enum BuyerOfferResponseType { Accepted, Rejected, Cancelled }

    public enum GroupRequestAlertStatus { Pending, Responded, Ignored }

    public enum OrderStatus { Pending, Paid, Failed }

    public enum BatchParticipantStatus { Active, Left }
    // Active = payment hold in place (join = pay)

    public enum GroupRequestParticipantStatus { Active, Left }
    // Active = still part of the demand pool, no payment implication

    public enum NotificationType {
        BatchCompleted,
        GroupRequestOfferPlaced,
        GroupRequestOfferFilled,
        GroupRequestMatched,
        OfferApproved,
        GroupRequestApproved,
        OfferFlagged,
        GroupRequestFlagged,
    }

    public enum ModerationStatus { Pending, Approved, Flagged }
}
