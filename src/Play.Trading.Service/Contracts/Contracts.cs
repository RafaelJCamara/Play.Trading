using System;

namespace Play.Trading.Service.Contracts
{
    /*
        This will trigger the begining of our sage, thus the beginning of the state machine
     */
    public record PurchaseRequested(
        Guid UserId,
        Guid ItemId,
        int Quantity,
        Guid CorrelationId
    );
}
