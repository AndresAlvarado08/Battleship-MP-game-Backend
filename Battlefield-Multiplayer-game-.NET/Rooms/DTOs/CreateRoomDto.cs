using System.ComponentModel.DataAnnotations;

namespace Battlefield_Multiplayer_game_.NET.Rooms.DTOs
{
    public class CreateRoomDto
    {
        [Required]
        public string Creator { get; set; } = string.Empty;

        [Range(2, 6)]
        public int MaxPlayers { get; set; }
    }
}
