using System;

namespace Jomla.Application.Features.Batches.Queries.GetMyHubs
{
    public class BuyerHubDto
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "supplier_offer" or "group_request"
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int CommittedUnits { get; set; }
        public Guid? BatchId { get; set; }
        public Guid? RequestId { get; set; }
        public int? FillProgress { get; set; }
        public int? FillTarget { get; set; }
    }
}
