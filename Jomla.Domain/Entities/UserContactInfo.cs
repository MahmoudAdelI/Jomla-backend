using System;

namespace Jomla.Domain.Entities
{
    public class UserContactInfo
    {
        public Guid UserId { get; set; }
        public AppUser User { get; set; } = null!;
        public string ShippingAddress { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
    }
}
