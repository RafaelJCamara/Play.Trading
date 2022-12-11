using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Play.Common.Repository;
using Play.Trading.Service.Dtos;
using Play.Trading.Service.Entities;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Play.Trading.Service.Controllers
{
    [ApiController]
    [Route("store")]
    [Authorize]
    public class StoreController : ControllerBase
    {
        private readonly IRepository<CatalogItem> catalogRepository;
        private readonly IRepository<ApplicationUser> usersRepository;
        private readonly IRepository<InventoryItem> inventoryRepository;

        public StoreController(
            IRepository<CatalogItem> catalogRepository, 
            IRepository<ApplicationUser> usersRepository, 
            IRepository<InventoryItem> inventoryRepository)
        {
            this.catalogRepository = catalogRepository;
            this.usersRepository = usersRepository;
            this.inventoryRepository = inventoryRepository;
        }


        [HttpGet]
        public async Task<ActionResult<StoreDto>> GetAsync()
        {
            string userId = User.FindFirstValue("sub");

            var catalogItems = await catalogRepository.GetAllAsync();
            var inventoryItems = await inventoryRepository.GetAllAsync(
                item => item.UserId == Guid.Parse(userId)
            );
            var user = await usersRepository.GetAsync(Guid.Parse(userId));

            var storeItems = catalogItems.Select(catalogItem =>
                new StoreItemDto(
                    catalogItem.Id,
                    catalogItem.Name,
                    catalogItem.Description,
                    catalogItem.Price,
                    inventoryItems.FirstOrDefault(
                        inventoryItem => inventoryItem.CatalogItemId == catalogItem.Id    
                    )?.Quantity ?? 0
                )
            );

            var storeDto = new StoreDto(
                null,
                user?.Gil ?? 0
            );

            return Ok(storeDto);
        }
    }
}
