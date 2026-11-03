using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjetInfo.Data;
using ProjetInfo.Models;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ProjetInfo.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ChatController : ControllerBase
	{
		private readonly RideShareDbContext _context;

		public ChatController(RideShareDbContext context)
		{
			_context = context;
		}

		// GET: api/Chat/{rideId}
		[HttpGet("{rideId}")]
		public async Task<IActionResult> GetChatHistory(int rideId)
		{
			var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

			// Verify user has access to this ride's chat
			var ride = await _context.Rides
				.FirstOrDefaultAsync(r => r.RideID == rideId &&
									   (r.UserID == userId || r.DriverID == userId));

			if (ride == null)
			{
				return Forbid();
			}

			var messages = await _context.ChatMessages
				.Where(m => m.RideID == rideId)
				.OrderBy(m => m.Timestamp)
				.Select(m => new
				{
					m.Id,
					m.SenderID,
					m.Message,
					m.Timestamp,
					IsCurrentUser = m.SenderID == userId
				})
				.ToListAsync();

			return Ok(messages);
		}

		// POST: api/Chat/Send
		[HttpPost("Send")]
		public async Task<IActionResult> SendMessage([FromBody] ChatMessageDto messageDto)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

			var ride = await _context.Rides
				.FirstOrDefaultAsync(r => r.RideID == messageDto.RideID &&
									   (r.UserID == userId || r.DriverID == userId));

			if (ride == null)
			{
				return Forbid();
			}

			var chatMessage = new ChatMessage
			{
				RideID = messageDto.RideID,
				SenderID = userId,
				Message = messageDto.Message,
				Timestamp = DateTime.Now
			};

			_context.ChatMessages.Add(chatMessage);
			await _context.SaveChangesAsync();

			return Ok(new
			{
				chatMessage.Id,
				chatMessage.SenderID,
				chatMessage.Message,
				chatMessage.Timestamp,
				IsCurrentUser = true
			});
		}
	}

	public class ChatMessageDto
	{
		public int RideID { get; set; }
		public string Message { get; set; }
	}
}