using System;

namespace Store.Domain.Common;

public interface ITenantEntity
{
    Guid StoreId { get; set; }
}

public interface IOptionalTenantEntity
{
    Guid? StoreId { get; set; }
}
