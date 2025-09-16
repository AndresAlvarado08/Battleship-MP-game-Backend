namespace Battlefield_Multiplayer_game_.NET.Rooms.Models
{
    public class Room
    {
        public string Code { get; set; } = string.Empty;
        public string Creator { get; set; } = string.Empty;
        public int MaxPlayers { get; set; }
        public List<string> Players { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
