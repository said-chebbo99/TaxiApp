using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ProjetInfo.Data;
using ProjetInfo.Models;


namespace ProjetInfo
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			builder.Services.AddSignalR();

			var key = "this_is_a_very_secure_and_long_jwt_key_123456";

			builder.Services.AddControllersWithViews();
			builder.Services.AddSession();
			builder.Services.AddHttpContextAccessor();

			builder.Services.AddDbContext<RideShareDbContext>(options =>
				options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

			// 🔐 External Authentication (Google & Facebook)
			builder.Services.AddAuthentication(options =>
			{
				options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
				options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
			})
			// 🔑 JWT for internal token auth
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

				options.Events = new JwtBearerEvents
				{
					OnMessageReceived = context =>
					{
						if (context.Request.Cookies.ContainsKey("jwt"))
						{
							context.Token = context.Request.Cookies["jwt"];
						}
						return Task.CompletedTask;
					}
				};
			})
			// 🌐 Google Login
			.AddGoogle(googleOptions =>
			{
				googleOptions.ClientId = "327744417287-3qg9lvv0bkmgd131s49c6os6vsjeik39.apps.googleusercontent.com";
				googleOptions.ClientSecret = "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";
			});
			

			builder.Services.AddAuthorization();

			var app = builder.Build();

			app.MapHub<RideHub>("/rideHub");

			if (!app.Environment.IsDevelopment())
			{
				app.UseExceptionHandler("/Home/Error");
				app.UseHsts();
			}

			app.UseHttpsRedirection();
			app.UseStaticFiles();
			app.UseRouting();

			app.UseAuthentication(); // ✅
			app.UseAuthorization();
			app.UseSession();

			app.MapControllerRoute(
				name: "default",
				pattern: "{controller=Account}/{action=SignUp}/{id?}");

			// 🧪 Seed test driver
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
