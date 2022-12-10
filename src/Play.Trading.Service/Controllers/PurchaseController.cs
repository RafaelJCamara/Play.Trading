using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Play.Trading.Service.Contracts;
using Play.Trading.Service.Dtos;
using Play.Trading.Service.StateMachines;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Play.Trading.Service.Controllers
{
    [ApiController]
    [Route("purchase")]
    [Authorize]
    public class PurchaseController : ControllerBase
    {
        private readonly IPublishEndpoint publishEndpoint;
        private readonly IRequestClient<GetPurchaseState> purchaseClient;

        public PurchaseController(IPublishEndpoint publishEndpoint, IRequestClient<GetPurchaseState> purchaseClient)
        {
            this.publishEndpoint = publishEndpoint;
            this.purchaseClient = purchaseClient;
        }


        [HttpGet("status/{idempotencyId}")]
        public async Task<ActionResult<PurchaseDto>> GetStatusAsync(Guid idempotencyId)
        {
            var response = await purchaseClient
                                    .GetResponse<PurchaseState>(new GetPurchaseState(idempotencyId));
            var purchaseState = response.Message;

            var purchase = new PurchaseDto(
                purchaseState.UserId,
                purchaseState.ItemId,
                purchaseState.PurchaseTotal,
                purchaseState.Quantity,
                purchaseState.CurrentState,
                purchaseState.ErrorMessage,
                purchaseState.Received,
                purchaseState.LastUpdated
            );

            return Ok(purchase);
        }


        [HttpPost]
        public async Task<IActionResult> PostAsync(SubmitPurchaseDto purchase)
        {
            var userId = User.FindFirstValue("sub");

            /*
                As we have seen before, the correlationId is what uniquely identitifes the state machine
                By using it as the idempotencyId, we avoid troubling situations that come with not having idempotency
                If you want to generate another purchase, you must must create another idempotency ID
             */
            var correlationId = purchase.IdempotencyId.Value;

            var message = new PurchaseRequested(
                Guid.Parse(userId),
                purchase.ItemId.Value,
                purchase.Quantity,
                correlationId
            );

            await publishEndpoint.Publish(message);

            return AcceptedAtAction(nameof(GetStatusAsync), new { correlationId }, new { correlationId });
        }

    }
}
