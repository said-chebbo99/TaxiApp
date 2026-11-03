using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjetInfo.Models
{
	public class Ride
	{
		public int RideID { get; set; }
		public int UserID { get; set; }
		public int? DriverID { get; set; }
		public string PickupLocation { get; set; }
		public string DropOffLocation { get; set; }
		public DateTime RideDateTime { get; set; }
		public DateTime StartTime { get; set; }
		public DateTime? EndTime { get; set; }
		public DateTime? ScheduledDateTime { get; set; }
		public int? PaymentTransactionID { get; set; }
		public string Status { get; set; }
		public double? EstimatedTimeMinutes { get; set; }
		public double DistanceKm { get; set; }

		// New properties for loyalty discount
		public bool UsedLoyaltyDiscount { get; set; } = false;
		public decimal OriginalFare { get; set; }
		public decimal FinalFare { get; set; }

		public User User { get; set; }
		public Driver Driver { get; set; }
		public PaymentTransaction PaymentTransaction { get; set; }
		public ICollection<RideFeedback> RideFeedbacks { get; set; }
		public ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
	}
}