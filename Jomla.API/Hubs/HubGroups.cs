namespace Jomla.API.Hubs
{
    public static class HubGroups
    {
        public static string OfferGroup(Guid offerId) => $"offer:{offerId}";
        public static string GroupRequestGroup(Guid id) => $"grouprequest:{id}";
        public static string AdminGroup() => "admin";
    }
}
