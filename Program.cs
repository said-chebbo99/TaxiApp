using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ProjetInfo.Data;
using ProjetInfo.Models;
using ProjetInfo.Hubs;

namespace ProjetInfo
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			// ✅ SignalR
			builder.Services.AddSignalR();

			// ✅ JWT Key
			var key = "this_is_a_very_secure_and_long_jwt_key_123456";

			builder.Services.AddControllersWithViews();
			builder.Services.AddSession();
			builder.Services.AddHttpContextAccessor();

			builder.Services.AddDbContext<RideShareDbContext>(options =>
				options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

			// ✅ Authentication with JWT
			builder.Services.AddAuthentication(options =>
			{
				options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
				options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
			})
			.AddJwtBearer(options =>
			{
				options.TokenValidationParameters = new TokenValidationParameters
				{
					ValidateIssuer = false,
					ValidateAudience = false,
					ValidateLifetime = true,
					ValidateIssuerSigningKey = true,
					IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
				};

				// ✅ Allow JWT in query string for SignalR
				options.Events = new JwtBearerEvents
				{
					OnMessageReceived = context =>
					{
						var accessToken = context.Request.Query["access_token"];
						var path = context.HttpContext.Request.Path;

						if (!string.IsNullOrEmpty(accessToken) &&
							(path.StartsWithSegments("/chatHub") || path.StartsWithSegments("/rideHub")))
						{
							context.Token = accessToken;
						}
						else if (context.Request.Cookies.ContainsKey("jwt"))
						{
							context.Token = context.Request.Cookies["jwt"];
						}
						return Task.CompletedTask;
					}
				};
			})
			// ✅ Optional: Google Login
			.AddGoogle(googleOptions =>
			{
				googleOptions.ClientId = "327744417287-3qg9lvv0bkmgd131s49c6os6vsjeik39.apps.googleusercontent.com";
				googleOptions.ClientSecret = "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";
			});

			builder.Services.AddAuthorization();

			var app = builder.Build();

			// ✅ Map SignalR Hubs
			app.MapHub<ChatHub>("/chatHub");
			app.MapHub<RideHub>("/rideHub");

			// 🔧 Error & HTTPS
			if (!app.Environment.IsDevelopment())
			{
				app.UseExceptionHandler("/Home/Error");
				app.UseHsts();
			}

			app.UseHttpsRedirection();
			app.UseStaticFiles();
			app.UseRouting();

			// ✅ Auth, Session
			app.UseAuthentication();
			app.UseAuthorization();
			app.UseSession();

			// ✅ MVC Routing
			app.MapControllerRoute(
				name: "default",
				pattern: "{controller=Account}/{action=SignUp}/{id?}");

			// ✅ Seed test driver
			using (var scope = app.Services.CreateScope())
			{
				var db = scope.ServiceProvider.GetRequiredService<RideShareDbContext>();

				if (!db.Users.Any(u => u.Role == "driver"))
				{
					var driverUser = new User
					{
						FullName = "Ali Kassem",
						Email = "ali@example.com",
						PhoneNumber = "71123456",
						Password = "1234",
						Role = "driver"
					};

					db.Users.Add(driverUser);
					db.SaveChanges();

					var driver = new Driver
					{
						UserID = driverUser.ID,
						LicenseNumber = "XYZ123",
						Availability = true
					};

					db.Drivers.Add(driver);
					db.SaveChanges();
				}
			}

			app.Run();
		}
	}
}