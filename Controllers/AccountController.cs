using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using TodoManagementSystem.Models;
using TodoManagementSystem.ViewModels;
using TodoManagementSystem.Services; 

namespace TodoManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ITodoService _todoService; 

        // Constructor güncellendi
        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ITodoService todoService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _todoService = todoService;
        }
        // 1. KAYIT OLMA EKRANINI AÇAN METOT (GET)
        public IActionResult Register()
        {
            return View();
        }

        // 2. KAYIT OLMA BUTONUNA BASILINCA ÇALIŞAN METOT (POST)
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
               
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home"); 
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        // 3. GİRİŞ YAPMA EKRANINI AÇAN METOT (GET)
        public IActionResult Login()
        {
            return View();
        }

        // 4. GİRİŞ YAP BUTONUNA BASILINCA ÇALIŞAN METOT (POST)
        [HttpPost]
public async Task<IActionResult> Login(LoginViewModel model)
{
    if (ModelState.IsValid)
    {
        var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, false, lockoutOnFailure: false);
        
        if (result.Succeeded)
        {
            // Giriş yapan kullanıcıyı veritabanından bul
            var user = await _userManager.FindByEmailAsync(model.Email);
            
            // Eğer giriş yapan kişi belirtilen admin e-posta adresiyse veya Admin rolündeyse
            if (user != null && (user.Email == "admin@mail.com" || await _userManager.IsInRoleAsync(user, "Admin")))
            {
                // Yöneticileri doğrudan kendi paneline (SB Admin 2) yönlendir
                return RedirectToAction("Index", "Admin");
            }
            
            // Normal kullanıcıları kendi görevlerine (Sneat) yönlendir
            return RedirectToAction("Index", "Todo");
        }
        
        ModelState.AddModelError(string.Empty, "Geçersiz e-posta veya şifre.");
    }
    return View(model);
}
        // 5. ÇIKIŞ YAPMA METODU (POST)
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home"); 
        }

        // 6. PROFİL SAYFASI VE FOTOĞRAF YÜKLEME
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            return View(user);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UploadProfilePicture(IFormFile profilePicture)
        {
            if (profilePicture != null && profilePicture.Length > 0 && profilePicture.Length < 3 * 1024 * 1024) // 3MB boyut sınırı
            {
                var user = await _userManager.GetUserAsync(User);
                
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(profilePicture.FileName);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/profiles", fileName);
                var directory = Path.GetDirectoryName(filePath);
                
                if (!Directory.Exists(directory)) 
                    Directory.CreateDirectory(directory!);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await profilePicture.CopyToAsync(stream);
                }
                if (user != null)
                {
                 user.ProfilePicturePath = "/images/profiles/" + fileName;
                 await _userManager.UpdateAsync(user);
                }
            
            }
            
            return RedirectToAction("Profile");
        }
        // 1. PROFİL FOTOĞRAFINI KALDIRMA
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> RemoveProfilePicture()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                user.ProfilePicturePath = null;
                await _userManager.UpdateAsync(user);
            }
            return RedirectToAction("Profile");
        }

        // 2. İSTEĞE BAĞLI BİLGİLERİ GÜNCELLEME
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UpdateProfileInfo(string firstName, string lastName, string bio)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                user.FirstName = firstName;
                user.LastName = lastName;
                user.Bio = bio;
                await _userManager.UpdateAsync(user);
            }
            return RedirectToAction("Profile");
        }

        // 3. BAŞKASININ PROFİLİNİ GÖRÜNTÜLEME
        [Authorize]
        public async Task<IActionResult> UserProfile(string id)
        {
            var targetUser = await _userManager.FindByIdAsync(id);
            if (targetUser == null) return NotFound();

            // O kullanıcının görevlerini getir
            var userTodos = await _todoService.GetTodosByUserIdAsync(id);
            ViewBag.UserTodos = userTodos;

            return View(targetUser);
        }
    }
}