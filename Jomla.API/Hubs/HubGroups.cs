namespace Jomla.API.Hubs
{
    public static class HubGroups
    {
        public static string OfferGroup(Guid offerId) => $"offer:{offerId}";
    }
}
