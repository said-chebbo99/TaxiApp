using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjetInfo.Models
{
	public class User
	{
		[Key]
		public int ID { get; set; }

		[Required]
		[StringLength(100)]
		public string FullName { get; set; }

		[Required]
		[EmailAddress]
		[StringLength(100)]
		public string Email { get; set; }

		[Required]
		[StringLength(20)]
		public string PhoneNumber { get; set; }

		[Required]
		[StringLength(100)]
		public string Password { get; set; }

		[Required]
		[StringLength(20)]
		public string Role { get; set; }

		public virtual UserPreferences? UserPreferences { get; set; }
		public virtual ICollection<Ride> Rides { get; set; }
		public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; }
		public virtual ICollection<RideFeedback> RideFeedbacks { get; set; }
		public ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
		public int Points { get; set; } = 0;

		public User()
		{
			Rides = new HashSet<Ride>();
			PaymentTransactions = new HashSet<PaymentTransaction>();
			RideFeedbacks = new HashSet<RideFeedback>();
		}
	}
}