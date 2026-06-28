using Jomla.Application.Common.BaseClass;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Features.Offers.DTOs
{
    public class GetAllOffersPagedResponse : PagedResponse<OfferDto>
    {
        public int ActiveOffersCount { get; set; }

        public int ExpiredOffersCount { get; set; }

        public int PendingModerationCount { get; set; }
    }
}
