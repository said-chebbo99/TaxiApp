using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ProjetInfo.Data;
using ProjetInfo.Models;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ProjetInfo.Controllers
{
	public class RideController : Controller
	{
		private readonly RideShareDbContext _context;
		private readonly IHubContext<RideHub> _hubContext;

		public RideController(RideShareDbContext context, IHubContext<RideHub> hubContext)
		{
			_context = context;
			_hubContext = hubContext;
		}

		[HttpGet]
		public IActionResult BookRide()
		{
			return View();
		}

		[HttpPost]
		public IActionResult BookRide(string pickup, string dropoff, DateTime? scheduledDateTime, double distanceKm)
		{
			var email = User.FindFirst(ClaimTypes.Email)?.Value;
			var user = _context.Users.FirstOrDefault(u => u.Email == email);
			if (user == null) return Unauthorized();

			bool isScheduled = scheduledDateTime.HasValue && scheduledDateTime.Value > DateTime.Now;

			double averageSpeedKmh = 40.0;
			double estimatedTimeMinutes = (distanceKm / averageSpeedKmh) * 60;

			var ride = new Ride
			{
				UserID = user.ID,
				PickupLocation = pickup,
				DropOffLocation = dropoff,
				RideDateTime = DateTime.Now,
				ScheduledDateTime = scheduledDateTime,
				Status = "Pending",
				EstimatedTimeMinutes = estimatedTimeMinutes
			};

			_context.Rides.Add(ride);
			_context.SaveChanges();

			return Json(new
			{
				rideId = ride.RideID,
				estimatedTimeMinutes = Math.Round(estimatedTimeMinutes, 1),
				message = isScheduled
					? "✅ Ride scheduled! A driver can accept your ride from the available rides list."
					: "✅ Ride booked! A driver can accept your ride from the available rides list.",
				driver = (object)null
			});
		}

		[HttpPost]
		public IActionResult AssignDriversToScheduledRides()
		{
			var now = DateTime.Now;
			var scheduledRides = _context.Rides
				.Where(r => r.Status == "Pending" && r.ScheduledDateTime <= now && r.DriverID == null)
				.ToList();

			foreach (var ride in scheduledRides)
			{
				var availableDriver = _context.Drivers
					.Include(d => d.User)
					.Include(d => d.Vehicles)
					.Where(d => d.Availability == true)
					.FirstOrDefault();

				if (availableDriver != null)
				{
					ride.DriverID = availableDriver.DriverID;
					ride.Status = "Accepted";
					availableDriver.Availability = false;
				}
			}

			_context.SaveChanges();
			return Ok("Drivers assigned to scheduled rides.");
		}

		[HttpGet]
		public IActionResult GetAvailableDrivers()
		{
			var drivers = _context.Drivers
				.Where(d => d.Availability == true && d.Latitude != null && d.Longitude != null)
				.Select(d => new
				{
					id = d.DriverID,
					name = d.User.FullName,
					lat = d.Latitude,
					lng = d.Longitude
				})
				.ToList();

			return Json(drivers);
		}

		[HttpGet]
		public IActionResult GetDriverLocation(int driverId)
		{
			var driver = _context.Drivers.FirstOrDefault(d => d.DriverID == driverId);
			if (driver == null) return NotFound();

			return Json(new { lat = driver.Latitude, lng = driver.Longitude });
		}

		[HttpPost]
		public IActionResult RateDriver(int driverId, int rating)
		{
			var userId = _context.Users.First().ID;

			var feedback = new RideFeedback
			{
				UserID = userId,
				DriverID = driverId,
				Rating = rating,
				Comments = ""
			};

			_context.RideFeedbacks.Add(feedback);
			_context.SaveChanges();

			return Ok("Rating saved!");
		}

		[HttpPost]
		public IActionResult Rate(int driverId, int rating, string comment)
		{
			var user = _context.Users.FirstOrDefault();
			if (user == null)
				return BadRequest("User not found.");

			var ride = _context.Rides
				.Where(r => r.DriverID == driverId && r.UserID == user.ID)
				.OrderByDescending(r => r.StartTime)
				.FirstOrDefault();

			if (ride == null)
				return BadRequest("Ride not found to attach feedback.");

			var feedback = new RideFeedback
			{
				RideID = ride.RideID,
				DriverID = driverId,
				UserID = user.ID,
				Rating = rating,
				Comments = comment ?? ""
			};

			_context.RideFeedbacks.Add(feedback);
			_context.SaveChanges();

			return Ok(new { success = true });
		}

		[HttpGet]
		public IActionResult AvailableRides()
		{
			return View();
		}

		[HttpGet]
		public IActionResult GetAvailableRides()
		{
			var rides = _context.Rides
				.Include(r => r.User)
				.Where(r => r.DriverID == null && r.Status == "Pending")
				.Select(r => new
				{
					rideID = r.RideID,
					pickupLocation = r.PickupLocation,
					dropOffLocation = r.DropOffLocation,
					rideDateTime = r.RideDateTime,
					estimatedTimeMinutes = r.EstimatedTimeMinutes
				})
				.ToList();

			return Json(rides);
		}

		[HttpPost]
		public async Task<IActionResult> AcceptRide(int rideId)
		{
			var email = User.FindFirst(ClaimTypes.Email)?.Value;
			var user = _context.Users.FirstOrDefault(u => u.Email == email);
			if (user == null || user.Role != "driver") return Unauthorized();

			var driver = _context.Drivers.FirstOrDefault(d => d.UserID == user.ID);
			if (driver == null) return NotFound();

			var ride = _context.Rides.FirstOrDefault(r => r.RideID == rideId);
			if (ride == null || ride.Status != "Pending") return NotFound();

			ride.DriverID = driver.DriverID;
			ride.Status = "InProgress";
			ride.StartTime = DateTime.Now;
			driver.Availability = false;

			if (!string.IsNullOrEmpty(ride.PickupLocation) && ride.PickupLocation.Contains(","))
			{
				var parts = ride.PickupLocation.Split(',');
				if (parts.Length == 2 &&
					double.TryParse(parts[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double lat) &&
					double.TryParse(parts[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double lng))
				{
					driver.Latitude = lat;
					driver.Longitude = lng;
				}
			}

			_context.SaveChanges();

			await _hubContext.Clients.Group($"ride-{rideId}").SendAsync("ReceiveRideStatus", "InProgress");

			return Ok();
		}

		[HttpPost]
		public async Task<IActionResult> CompleteRide(int rideId)
		{
			var ride = _context.Rides.Include(r => r.User).FirstOrDefault(r => r.RideID == rideId);
			if (ride == null || ride.Status != "InProgress") return NotFound();

			ride.Status = "Completed";
			ride.EndTime = DateTime.Now;

			var user = ride.User;
			if (user != null)
			{
				user.Points += 10; // Award 10 points per ride
			}

			var driver = _context.Drivers.FirstOrDefault(d => d.DriverID == ride.DriverID);
			if (driver != null)
			{
				driver.Availability = true;
			}

			_context.SaveChanges();

			await _hubContext.Clients.Group($"ride-{rideId}").SendAsync("ReceiveRideStatus", "Completed");

			return Ok(new { message = "Ride completed and points awarded!" });
		}

		[HttpGet]
		public IActionResult GetRideStatus(int rideId)
		{
			var ride = _context.Rides
				.Include(r => r.Driver)
					.ThenInclude(d => d.User)
				.Include(r => r.Driver)
					.ThenInclude(d => d.Vehicles)
				.FirstOrDefault(r => r.RideID == rideId);

			if (ride == null || ride.Driver == null)
				return Json(new { status = ride?.Status ?? "Unknown" });

			var driver = ride.Driver;
			var user = driver.User;
			var vehicle = driver.Vehicles?.FirstOrDefault();

			var rating = _context.RideFeedbacks
				.Where(f => f.DriverID == driver.DriverID)
				.Select(f => (double?)f.Rating)
				.DefaultIfEmpty()
				.Average();

			return Json(new
			{
				status = ride.Status,
				estimatedTimeMinutes = ride.EstimatedTimeMinutes,
				driver = new
				{
					id = driver.DriverID,
					name = user?.FullName ?? "",
					phone = user?.PhoneNumber ?? "",
					carModel = vehicle != null ? $"{vehicle.Brand} {vehicle.Model}" : "",
					plate = vehicle?.PlateNumber ?? "",
					rating = rating.HasValue ? Math.Round(rating.Value, 2) : (double?)null,
					lat = driver.Latitude,
					lng = driver.Longitude
				}
			});
		}

		public IActionResult AboutUs()
		{
			return View();
		}
	}
}
