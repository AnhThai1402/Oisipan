using FirebaseAdmin.Auth;
using LoginNumPhone.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Diagnostics;
using System.Security.Claims;
using System.Text;

namespace LoginNumPhone.Controllers
{
    public class HomeController : Controller
    {
       
        private readonly OishipanContext _context;
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;
        public HomeController(OishipanContext context, ILogger<HomeController> logger, IConfiguration configuration)
        {
           
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }
       

        public IActionResult Index()
        {
            ViewBag.ApiKey = _configuration["Firebase:apiKey"];
            ViewBag.AuthDomain = _configuration["Firebase:authDomain"];
            ViewBag.ProjectId = _configuration["Firebase:projectId"];
            ViewBag.StorageBucket = _configuration["Firebase:storageBucket"];
            ViewBag.MessagingSenderId = _configuration["Firebase:messagingSenderId"];
            ViewBag.AppId = _configuration["Firebase:appId"];

            return View();
        }
        public IActionResult Login()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var listAcc = await _context.Accounts.ToListAsync();
            return View(listAcc);
        }

        private readonly PasswordHasher<Account> _passwordHasher = new PasswordHasher<Account>();
        [HttpPost]
        public async Task<IActionResult> LoginPhone([FromBody] Models.LoginRequest request)
        {
            var token = await FirebaseAuth.DefaultInstance
                .VerifyIdTokenAsync(request.IdToken);

            if (!token.Claims.TryGetValue("phone_number", out var phoneObj))
            {
                return BadRequest("Không lấy được số điện thoại.");
            }
            string phone = phoneObj?.ToString() ?? "";

            // +84901234567 -> 0901234567
            if (phone.StartsWith("+84"))
            {
                phone = "0" + phone.Substring(3);
            }

            var account = await _context.Accounts
                .FirstOrDefaultAsync(x => x.PhoneNumber == phone);

            if (account == null)
            {
                account = new Account
                {
                    FullName = "Người dùng mới",
                    PhoneNumber = phone,
                    Email = $"phone_{Guid.NewGuid():N}@local",
                    Role = "User",
                    Address = "",
                    Status = true
                };

                account.Password = new PasswordHasher<Account>()
                    .HashPassword(account, "");

                _context.Accounts.Add(account);
                await _context.SaveChangesAsync();
            }
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, account.UserId.ToString()),
        new Claim(ClaimTypes.MobilePhone, phone)
    };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));

            return Ok(new
            {
                success = true,
                userId = account.UserId
            });
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string pass)
        {
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Email == email);

            if (account == null)
            {
                ViewBag.Error = "Email hoặc mật khẩu không đúng";
                return View("Index");
            }

            var result = _passwordHasher.VerifyHashedPassword(
                account,
                account.Password,
                pass);

            if (result == PasswordVerificationResult.Failed)
            {
                ViewBag.Error = "Email hoặc mật khẩu không đúng";
                return View("Index");
            }

            var claims = new List<Claim>
{
    new Claim(ClaimTypes.NameIdentifier, account.UserId.ToString()),
    new Claim(ClaimTypes.Email, account.Email),
    new Claim(ClaimTypes.Name, account.FullName),
    new Claim(ClaimTypes.Role, account.Role)
};

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));

            return RedirectToAction("GetAll");

            
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
