using Battlefield_Multiplayer_game_.NET.Rooms.DTOs;
using Battlefield_Multiplayer_game_.NET.Rooms.Models;

namespace Battlefield_Multiplayer_game_.NET.Rooms.Services
{
    public interface IRoomsService
    {
        Room Create(CreateRoomDto dto);
        Room? GetByCode(string code);
        IEnumerable<Room> GetAll();
    }
}
