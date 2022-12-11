using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Play.Trading.Service.StateMachines;

namespace Play.Trading.Service.SignalR
{
    [Authorize]
    public class MessageHub : Hub
    {
        /*
            Called to send the state machine state to the frontend
         */
        public async Task SendStatusAsync(PurchaseState status)
        {
            if (Clients != null)
            {
                await Clients.User(Context.UserIdentifier)
                       /*
                           This ReceivePurchaseStatus method is the method from the frontend side that will receive the update messages from the server
                        */
                       .SendAsync("ReceivePurchaseStatus", status);
            }
        }
    }
}