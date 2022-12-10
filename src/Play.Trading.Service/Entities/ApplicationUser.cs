using Play.Common.Repository;
using System;

namespace Play.Trading.Service.Entities
{
    public class ApplicationUser : IEntity
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public decimal Gil { get; set; }
    }
}
