using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjetInfo.Models
{
	public class ChatMessage
	{
		[Key]
		public int Id { get; set; }

		[ForeignKey("Ride")]
		public int RideID { get; set; }  // Must match Ride.RideID

		[ForeignKey("User")]
		public int SenderID { get; set; }  // User who sent the message

		[Required]
		public string Message { get; set; }

		public DateTime Timestamp { get; set; } = DateTime.Now;

		// Navigation properties
		public Ride Ride { get; set; }
		public User Sender { get; set; }
	}
}