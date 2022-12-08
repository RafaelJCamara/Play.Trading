using Automatonymous;
using Play.Identity.Contracts;
using Play.Inventory.Contracts;
using Play.Trading.Service.Activities;
using Play.Trading.Service.Contracts;
using System;

namespace Play.Trading.Service.StateMachines
{
    public class PurchaseStateMachine : MassTransitStateMachine<PurchaseState>
    {
        public State Accepted { get; }
        public State ItemsGranted { get; }
        public State Completed { get; }
        public State Faulted { get; }

        public Event<PurchaseRequested> PurchaseRequested { get; }
        public Event<GetPurchaseState> GetPurchaseState { get; }
        public Event<InventoryItemsGranted> InventoryItemsGranted { get; }
        public Event<GilDebited> GilDebited { get; }


        public PurchaseStateMachine()
        {
            //this just means that the CurrentState variable will contain the state of the State Machine
            InstanceState(state => state.CurrentState);
            //what this is doing here is configuring all the events we want to performs actions upon
            ConfigureEvents();
            ConfigureInitialState();
            ConfigureAny();
            ConfigureAccepted();
            ConfigureItemsGranted();
        }

        //Configure all the events
        private void ConfigureEvents()
        {
            Event(() => PurchaseRequested );
            Event(() => GetPurchaseState);
            Event(() => InventoryItemsGranted);
            Event(() => GilDebited);
        }

        //this is refering to what happens when the initial 
        private void ConfigureInitialState()
        {
            Initially(
                When(PurchaseRequested)
                .Then(context =>
                {
                    /*
                        context.Instance refers to the current State Machine information
                        context.Data refers to the request data that will start the state machine
                     */
                    context.Instance.UserId = context.Data.UserId;
                    context.Instance.ItemId = context.Data.ItemId;
                    context.Instance.Quantity = context.Data.Quantity;
                    context.Instance.Received = DateTimeOffset.Now;
                    context.Instance.LastUpdated = context.Instance.Received;
                })
                .Activity(x => x.OfType<CalculatePurchaseTotalActivity>())
                //we need to configure to which queue this send is going to end up in
                .Send(context => new GrantItems(
                    context.Instance.UserId,    
                    context.Instance.ItemId,    
                    context.Instance.Quantity,    
                    context.Instance.CorrelationId
                ))
                .TransitionTo(Accepted)
                .Catch<Exception>(ex => ex.Then(
                    context =>
                    {
                        context.Instance.ErrorMessage = context.Exception.Message;
                        context.Instance.LastUpdated = DateTimeOffset.UtcNow;
                    }    
                ).TransitionTo(Faulted))
            );
        }

        private void ConfigureAccepted()
        {
            During(Accepted,
                When(InventoryItemsGranted)
                    .Then(context =>
                    {
                        context.Instance.LastUpdated = DateTimeOffset.UtcNow;
                    })
                    .Send(context => new DebitGil(
                        context.Instance.UserId,
                        context.Instance.PurchaseTotal.Value,
                        context.Instance.CorrelationId
                    ))
                    .TransitionTo(ItemsGranted)
            );
        }

        private void ConfigureItemsGranted()
        {
            During(ItemsGranted,
                When(GilDebited)
                    .Then(context =>
                    {
                        context.Instance.LastUpdated = DateTimeOffset.UtcNow;
                    })
                    .TransitionTo(Completed)
            );
        }

        private void ConfigureAny()
        {
            DuringAny(
                When(GetPurchaseState)
                .Respond(x => x.Instance)
            );
        }

    }
}
