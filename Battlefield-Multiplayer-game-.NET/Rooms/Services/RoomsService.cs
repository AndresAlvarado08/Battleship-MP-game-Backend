using System.Text;
using Battlefield_Multiplayer_game_.NET.Rooms.DTOs;
using Battlefield_Multiplayer_game_.NET.Rooms.Models;

namespace Battlefield_Multiplayer_game_.NET.Rooms.Services
{
    public class RoomsService : IRoomsService
    {
        private readonly Dictionary<string, Room> _rooms = new();

        public Room Create(CreateRoomDto dto)
        {
            var code = GenerateUniqueCode(6);
            var room = new Room
            {
                Code = code,
                Creator = dto.Creator.Trim(),
                MaxPlayers = dto.MaxPlayers,
                Players = new List<string> { dto.Creator.Trim() },
                CreatedAt = DateTime.UtcNow
            };

            _rooms[code] = room;
            return room;
        }

        public Room? GetByCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;
            _rooms.TryGetValue(code.ToUpperInvariant(), out var room);
            return room;
        }

        public IEnumerable<Room> GetAll() => _rooms.Values;

        private string GenerateUniqueCode(int length)
        {
            const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var rng = Random.Shared;

            while (true)
            {
                var sb = new StringBuilder(length);
                for (int i = 0; i < length; i++)
                    sb.Append(alphabet[rng.Next(alphabet.Length)]);

                var code = sb.ToString();
                if (!_rooms.ContainsKey(code))
                    return code;
            }
        }
    }
}
