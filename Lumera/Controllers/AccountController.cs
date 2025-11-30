using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Lumera.Services;
using Lumera.Models.ViewModels;

namespace Lumera.Controllers
{
    public class AccountController(IUserService userService) : Controller
    {
        private readonly IUserService _userService = userService;

        [HttpGet]
        public IActionResult LoginSignup(string? activeTab = "login")
        {
            ViewBag.ActiveTab = activeTab ?? "login";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ActiveTab = "login";
                return View("LoginSignup", new RegisterLoginViewModel { Email = model.Email });
            }

            var user = await _userService.AuthenticateAsync(model.Email, model.Password);
            if (user == null)
            {
                ModelState.AddModelError("Password", "Invalid email or password");
                ViewBag.ActiveTab = "login";
                return View("LoginSignup", new RegisterLoginViewModel
                {
                    Email = model.Email,
                    Password = model.Password,
                    RememberMe = model.RememberMe
                });
            }

            // Check if user is approved (for Organizers and Suppliers)
            if (!user.IsApproved && user.Role != "Client")
            {
                ModelState.AddModelError("", "Your account is pending approval. Please wait for admin verification.");
                ViewBag.ActiveTab = "login";
                return View("LoginSignup", new RegisterLoginViewModel
                {
                    Email = model.Email,
                    Password = model.Password,
                    RememberMe = model.RememberMe
                });
            }
                // Create claims
                var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
        new Claim(ClaimTypes.Role, user.Role)
    };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // Log successful login
            Console.WriteLine($"User {user.Email} logged in successfully as {user.Role}");
            Console.WriteLine($"Login success: {user.Email}, RememberMe: {model.RememberMe}");

            // Redirect based on role with explicit route values
            return user.Role switch
            {
                "Admin" => RedirectToAction("Index", "Admin"),
                "Organizer" => RedirectToAction("Dashboard", "Organizer", new { area = "" }),
                "Supplier" => RedirectToAction("Dashboard", "Supplier"),
                _ => RedirectToAction("Dashboard", "Client")
            };
        }
        

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterLoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ActiveTab = "register";
                return View("LoginSignup", model);
            }

            // Check if email already exists
            if (await _userService.UserExistsAsync(model.Email))
            {
                ModelState.AddModelError("Email", "Email is already registered.");
                ViewBag.ActiveTab = "register";
                return View("LoginSignup", model);
            }

            // Create user
            var user = await _userService.RegisterAsync(model);
            if (user == null)
            {
                ModelState.AddModelError("Password", "Invalid email or password");
                ViewBag.ActiveTab = "login";
                return View("LoginSignup", model);
            }

            // Auto-login after registration or redirect to login
            if (user.Role == "Client")
            {
                // Auto-login for clients
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                    new Claim(ClaimTypes.Role, user.Role)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity));

                return RedirectToAction("Dashboard", "Client");
            }
            else
            {
                // For Organizers/Suppliers, show pending approval message
                TempData["SuccessMessage"] = "Registration successful! Your account is pending admin approval.";
                return RedirectToAction("LoginSignup", new { activeTab = "login" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}