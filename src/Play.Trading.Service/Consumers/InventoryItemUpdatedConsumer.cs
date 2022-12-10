﻿using MassTransit;
using Play.Common.Repository;
using Play.Inventory.Contracts;
using Play.Trading.Service.Entities;
using System.Threading.Tasks;

namespace Play.Trading.Service.Consumers
{
    public class InventoryItemUpdatedConsumer : IConsumer<InventoryItemUpdated>
    {
        private readonly IRepository<InventoryItem> repository;

        public InventoryItemUpdatedConsumer(IRepository<InventoryItem> repository)
        {
            this.repository = repository;
        }

        public async Task Consume(ConsumeContext<InventoryItemUpdated> context)
        {
            var message = context.Message;

            var inventoryItem = await repository
                                        .GetAsync(item => item.UserId == message.UserId && item.CatalogItemId == message.CatalogItemId );

            if(inventoryItem == null)
            {
                inventoryItem = new InventoryItem
                {
                    UserId = message.UserId,
                    CatalogItemId = message.CatalogItemId,
                    Quantity = message.NewTotalQuantity
                };
                await repository.CreateAsync(inventoryItem);
            }
            else
            {
                inventoryItem.Quantity = message.NewTotalQuantity;
                await repository.UpdateAsync(inventoryItem);
            }
        }
    }
}
