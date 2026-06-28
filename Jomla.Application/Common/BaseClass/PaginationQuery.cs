using Jomla.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jomla.Application.Common.BaseClass
{
    public abstract record PaginationQuery
    {
        public int? PageNumber { get; init; }

        public int? PageSize { get; init; }

        public string? Search { get; init; }

        public Guid? CategoryId { get; init; }

        public SupplierOfferStatus? Status { get; init; }
        public OfferSortBy? SortBy { get; init; }
        public bool Descending { get; init; }
    }
}
