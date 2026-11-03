using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ProjetInfo.Data;
using ProjetInfo.Models;
using System.Security.Claims;

namespace ProjetInfo.Hubs
{
	[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
	public class ChatHub : Hub
	{
		private readonly RideShareDbContext _context;

		public ChatHub(RideShareDbContext context)
		{
			_context = context;
		}

		public override async Task OnConnectedAsync()
		{
			var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (string.IsNullOrEmpty(userId))
			{
				Context.Abort();
				return;
			}

			var httpContext = Context.GetHttpContext();
			if (httpContext == null)
			{
				Context.Abort();
				return;
			}

			var rideId = httpContext.Request.Query["rideId"];
			if (!string.IsNullOrEmpty(rideId) && int.TryParse(rideId, out int rideIdInt))
			{
				// Verify user has access to this ride
				var hasAccess = await _context.Rides
					.AnyAsync(r => r.RideID == rideIdInt &&
								 (r.UserID.ToString() == userId || r.DriverID.ToString() == userId));

				if (hasAccess)
				{
					await Groups.AddToGroupAsync(Context.ConnectionId, $"ride-{rideIdInt}-chat");
					await Clients.Caller.SendAsync("ConnectionEstablished", $"Connected to ride {rideIdInt} chat");
				}
				else
				{
					Context.Abort();
					return;
				}
			}

			await base.OnConnectedAsync();
		}

		public async Task SendMessage(int rideId, string message)
		{
			var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (string.IsNullOrEmpty(userId))
				throw new HubException("Unauthorized");

			var ride = await _context.Rides
				.Include(r => r.User)
				.Include(r => r.Driver)
				.FirstOrDefaultAsync(r => r.RideID == rideId);

			if (ride == null)
				throw new HubException("Ride not found");

			// Verify user is part of this ride
			if (ride.UserID.ToString() != userId && ride.DriverID.ToString() != userId)
				throw new HubException("Unauthorized");

			var isDriver = await _context.Drivers.AnyAsync(d => d.UserID.ToString() == userId);
			var senderName = isDriver ? ride.Driver?.User?.FullName ?? "Driver"
									 : ride.User?.FullName ?? "Passenger";

			var chatMessage = new ChatMessage
			{
				RideID = rideId,
				SenderID = int.Parse(userId),
				Message = message,
				Timestamp = DateTime.Now
			};

			_context.ChatMessages.Add(chatMessage);
			await _context.SaveChangesAsync();

			await Clients.Group($"ride-{rideId}-chat").SendAsync("ReceiveMessage", new
			{
				senderId = userId,
				senderName,
				message,
				timestamp = chatMessage.Timestamp.ToString("HH:mm"),
				isDriver
			});
		}

		public async Task GetChatHistory(int rideId)
		{
			var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (string.IsNullOrEmpty(userId))
				throw new HubException("Unauthorized");

			// Verify user has access to this ride's chat history
			var hasAccess = await _context.Rides
				.AnyAsync(r => r.RideID == rideId &&
							 (r.UserID.ToString() == userId || r.DriverID.ToString() == userId));

			if (!hasAccess)
				throw new HubException("Unauthorized");

			var messages = await _context.ChatMessages
				.Where(m => m.RideID == rideId)
				.OrderBy(m => m.Timestamp)
				.Select(m => new
				{
					SenderID = m.SenderID.ToString(),
					Message = m.Message,
					Timestamp = m.Timestamp.ToString("HH:mm"),
					IsCurrentUser = m.SenderID.ToString() == userId,
					IsDriver = _context.Drivers.Any(d => d.UserID == m.SenderID)
				})
				.ToListAsync();

			await Clients.Caller.SendAsync("LoadChatHistory", messages);
		}

		public override async Task OnDisconnectedAsync(Exception? exception)
		{
			var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			Console.WriteLine($"User {userId} disconnected: {exception?.Message}");
			await base.OnDisconnectedAsync(exception);
		}
	}
}