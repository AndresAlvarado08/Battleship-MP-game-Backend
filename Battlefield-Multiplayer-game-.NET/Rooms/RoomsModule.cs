using Battlefield_Multiplayer_game_.NET.Rooms.Services;

namespace Battlefield_Multiplayer_game_.NET.Rooms
{
    public static class RoomsModule
    {
        public static IServiceCollection AddRoomsModule(this IServiceCollection services)
        {
            services.AddSingleton<IRoomsService, RoomsService>();
            return services;
        }
    }
}
