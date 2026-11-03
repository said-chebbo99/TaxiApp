namespace ProjetInfo.Models
{
    public class RideFeedback
    {
        public int FeedbackID { get; set; }

        public int RideID { get; set; }
        public int UserID { get; set; }
        public int DriverID { get; set; } // ⭐ Add this if not present

        public string ? Comments { get; set; }
        public int Rating { get; set; }

        // Navigation
        public virtual Ride Ride { get; set; }
        public virtual User User { get; set; }
        public virtual Driver Driver { get; set; }
		public DateTime FeedbackDate { get; set; } = DateTime.Now;
	}
}
