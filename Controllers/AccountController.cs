using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProjetInfo.Data;
using ProjetInfo.Models;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace ProjetInfo.Controllers
{
	public class AccountController : Controller
	{
		private readonly RideShareDbContext _context;
		private readonly string _jwtKey = "this_is_a_very_secure_and_long_jwt_key_123456";

		public AccountController(RideShareDbContext context)
		{
			_context = context;
		}

		public IActionResult SignUp()
		{
			return View();
		}

		[HttpPost]
		public IActionResult SignUp(string FullName, string Email, string PhoneNumber, string Password, string Role, string? CarMake, string? CarModel, string? LicensePlate, string? LicenseNumber)
		{
			try
			{
				if (_context.Users.Any(u => u.Email == Email))
				{
					ViewData["Error"] = "Email already registered.";
					return View();
				}

				var user = new User
				{
					FullName = FullName,
					Email = Email,
					PhoneNumber = PhoneNumber,
					Password = Password,
					Role = Role,
					Points = 0
				};

				_context.Users.Add(user);
				_context.SaveChanges();

				if ((Role ?? string.Empty).ToLower() == "driver")
				{
					var driver = new Driver
					{
						UserID = user.ID,
						LicenseNumber = LicenseNumber ?? "UNKNOWN",
						Availability = true
					};

					_context.Drivers.Add(driver);
					_context.SaveChanges();

					var vehicle = new Vehicle
					{
						DriverID = driver.DriverID,
						Brand = CarMake,
						Model = CarModel,
						PlateNumber = LicensePlate
					};

					_context.Vehicles.Add(vehicle);
					_context.SaveChanges();
				}

				TempData["SignUpSuccess"] = "✅ Sign up successful! Please sign in.";
				return RedirectToAction("SignUp");
			}
			catch (Exception ex)
			{
				ViewData["Error"] = $"Error: {ex.Message}";
				return View();
			}
		}

		public IActionResult SignIn()
		{
			return View();
		}

		[HttpPost]
		public IActionResult SignIn(string Email, string Password)
		{
			try
			{
				if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
					return StatusCode(400, "Email or password is missing.");

				var user = _context.Users.FirstOrDefault(u => u.Email == Email);

				if (user == null || user.Password != Password)
					return StatusCode(401, "Invalid login.");

				var token = GenerateJwtToken(user);

				HttpContext.Response.Cookies.Append("jwt", token, new CookieOptions
				{
					HttpOnly = true,
					Secure = true,
					SameSite = SameSiteMode.Strict,
					Expires = DateTimeOffset.UtcNow.AddHours(2)
				});

				return RedirectToAction("Index", "Home");
			}
			catch (Exception ex)
			{
				return StatusCode(500, $"🔥 Exception: {ex.Message}");
			}
		}

		[HttpGet]
		public IActionResult ExternalLogin(string provider, string returnUrl = "/")
		{
			var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { returnUrl });
			var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
			return Challenge(properties, provider);
		}

		[HttpGet]
		public async Task<IActionResult> ExternalLoginCallback(string returnUrl = "/")
		{
			var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
			if (!result.Succeeded) return RedirectToAction("SignUp");

			var email = result.Principal.FindFirst(ClaimTypes.Email)?.Value;
			var name = result.Principal.FindFirst(ClaimTypes.Name)?.Value;

			var user = _context.Users.FirstOrDefault(u => u.Email == email);
			if (user == null)
			{
				user = new User
				{
					FullName = name,
					Email = email,
					Password = "External",
					Role = "passenger"
				};
				_context.Users.Add(user);
				_context.SaveChanges();
			}

			var token = GenerateJwtToken(user);
			HttpContext.Response.Cookies.Append("jwt", token, new CookieOptions
			{
				HttpOnly = true,
				Secure = true,
				SameSite = SameSiteMode.Strict,
				Expires = DateTimeOffset.UtcNow.AddHours(2)
			});

			return Redirect(returnUrl);
		}

		[HttpGet]
		public IActionResult Dashboard()
		{
			return View();
		}

		private string GenerateJwtToken(User user)
		{
			var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));
			var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

			var claims = new[]
			{
				new Claim(ClaimTypes.NameIdentifier, user.ID.ToString()),
				new Claim(ClaimTypes.Name, user.FullName),
				new Claim(ClaimTypes.Email, user.Email),
				new Claim(ClaimTypes.Role, user.Role ?? "regular")
			};

			var token = new JwtSecurityToken(
				claims: claims,
				expires: DateTime.UtcNow.AddHours(2),
				signingCredentials: creds);

			return new JwtSecurityTokenHandler().WriteToken(token);
		}

		[HttpGet]
		public IActionResult Profile()
		{
			var email = User.FindFirst(ClaimTypes.Email)?.Value;
			var role = User.FindFirst(ClaimTypes.Role)?.Value;

			if (string.IsNullOrEmpty(email))
				return RedirectToAction("SignUp");

			var user = _context.Users.FirstOrDefault(u => u.Email == email);
			if (user == null) return NotFound();

			List<Ride> rides;

			if (role == "driver")
			{
				var driver = _context.Drivers.FirstOrDefault(d => d.UserID == user.ID);
				if (driver == null) return NotFound();

				rides = _context.Rides.Where(r => r.DriverID == driver.DriverID).ToList();
			}
			else
			{
				rides = _context.Rides.Where(r => r.UserID == user.ID).ToList();
			}

			ViewBag.Rides = rides;
			return View(user);
		}

		[HttpPost]
		public IActionResult Profile(User updatedUser)
		{
			var email = User.FindFirst(ClaimTypes.Email)?.Value;
			var user = _context.Users.FirstOrDefault(u => u.Email == email);

			if (user == null) return NotFound();

			user.FullName = updatedUser.FullName;
			user.Email = updatedUser.Email;
			user.PhoneNumber = updatedUser.PhoneNumber;
			_context.SaveChanges();

			ViewData["Success"] = "Profile updated successfully!";

			var rides = _context.Rides.Where(r => r.UserID == user.ID).ToList();
			ViewBag.Rides = rides;

			return View(user);
		}

		[HttpPost]
		public IActionResult ChangePassword(string CurrentPassword, string NewPassword)
		{
			var email = User.FindFirst(ClaimTypes.Email)?.Value;
			var user = _context.Users.FirstOrDefault(u => u.Email == email);

			if (user == null || user.Password != CurrentPassword)
			{
				ViewData["Error"] = "Current password is incorrect.";
				return View("Profile", user);
			}

			user.Password = NewPassword;
			_context.SaveChanges();

			ViewData["Success"] = "Password changed successfully!";

			List<Ride> rides;
			var role = User.FindFirst(ClaimTypes.Role)?.Value;

			if (role == "driver")
			{
				var driver = _context.Drivers.FirstOrDefault(d => d.UserID == user.ID);
				rides = _context.Rides.Where(r => r.DriverID == driver.DriverID).ToList();
			}
			else
			{
				rides = _context.Rides.Where(r => r.UserID == user.ID).ToList();
			}

			ViewBag.Rides = rides;

			return View("Profile", user);
		}

		[HttpPost]
		public IActionResult Logout()
		{
			Response.Cookies.Delete("jwt");
			return RedirectToAction("SignUp", "Account");
		}

		public IActionResult AboutUs()
		{
			return View();
		}
	}
}
