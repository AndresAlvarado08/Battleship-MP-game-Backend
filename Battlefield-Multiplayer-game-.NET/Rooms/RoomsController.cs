using Microsoft.AspNetCore.Mvc;
using Battlefield_Multiplayer_game_.NET.Rooms.DTOs;
using Battlefield_Multiplayer_game_.NET.Rooms.Models;
using Battlefield_Multiplayer_game_.NET.Rooms.Services;

namespace Battlefield_Multiplayer_game_.NET.Rooms
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoomsController : ControllerBase
    {
        private readonly IRoomsService _rooms;

        public RoomsController(IRoomsService rooms)
        {
            _rooms = rooms;
        }

        [HttpPost]
        [ProducesResponseType(typeof(Room), StatusCodes.Status201Created)]
        public ActionResult<Room> Create([FromBody] CreateRoomDto dto)
        {
            var room = _rooms.Create(dto);
            return CreatedAtAction(nameof(GetByCode), new { code = room.Code }, room);
        }

        [HttpGet("{code}")]
        [ProducesResponseType(typeof(Room), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<Room> GetByCode([FromRoute] string code)
        {
            var room = _rooms.GetByCode(code);
            if (room is null) return NotFound();
            return Ok(room);
        }

        [HttpGet]
        public IEnumerable<Room> GetAll() => _rooms.GetAll();
    }
}
