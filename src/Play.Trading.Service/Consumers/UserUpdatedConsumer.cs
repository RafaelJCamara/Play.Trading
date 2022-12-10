using MassTransit;
using Play.Common.Repository;
using Play.Identity.Contracts;
using Play.Trading.Service.Entities;
using System.Threading.Tasks;

namespace Play.Trading.Service.Consumers
{
    public class UserUpdatedConsumer : IConsumer<UserUpdated>
    {

        private readonly IRepository<ApplicationUser> repository;

        public UserUpdatedConsumer(IRepository<ApplicationUser> repository)
        {
            this.repository = repository;
        }

        public async Task Consume(ConsumeContext<UserUpdated> context)
        {
            var message = context.Message;

            var user = await repository.GetAsync(message.UserId);

            if(user == null)
            {
                user = new ApplicationUser
                {
                    Id = message.UserId,
                    Gil = message.NewTotalGil,
                    Email = message.Email
                };
            }
            else
            {
                user.Gil = message.NewTotalGil;
                user.Email = message.Email;
                await repository.UpdateAsync(user);
            }
        }
    }
}
