using Automatonymous;
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

        public PurchaseStateMachine()
        {
            //this just means that the CurrentState variable will contain the state of the State Machine
            InstanceState(state => state.CurrentState);
            //what this is doing here is configuring the initial event that will trigger everything
            ConfigureEvents();
            ConfigureInitialState();
        }

        private void ConfigureEvents()
        {
            Event(() => PurchaseRequested );
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
                .TransitionTo(Accepted)
            );;
        }

    }
}
