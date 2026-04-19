using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Narodnici.Models;
using Narodnici.Data;
using Narodnici.Services; // Добавено
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Narodnici.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender; // Добавено

        public ProfileController(UserManager<ApplicationUser> userManager, ApplicationDbContext context, IEmailSender emailSender)
        {
            _userManager = userManager;
            _context = context;
            _emailSender = emailSender;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return NotFound();
                }

                var userProgresses = await _context.UserBookProgresses
                    .Where(p => p.UserId == user.Id)
                    .ToListAsync();

                var progressDict = new Dictionary<int, int>();
                foreach (var p in userProgresses)
                {
                    int score = 0;
                    if (p.IsTextRead) score += 30;
                    if (p.IsAnalysisRead) score += 30;
                    if (p.HasPassedTest) score += 40;
                    progressDict[p.BookId] = score;
                }

                var allMaturaBooks = await _context.Books.Where(b => b.IsForMatura).Select(b => b.Id).ToListAsync();
                if (allMaturaBooks.Any())
                {
                    int totalScore = 0;
                    foreach (var id in allMaturaBooks)
                    {
                        if (progressDict.ContainsKey(id)) totalScore += progressDict[id];
                    }
                    ViewBag.OverallMaturaProgress = totalScore / allMaturaBooks.Count;
                }
                else
                {
                    ViewBag.OverallMaturaProgress = 0;
                }

                var userStats = new UserStatistics
                {
                    TotalBooksInCollections = await _context.BookCollections
                        .Where(bc => bc.UserId == user.Id)
                        .SelectMany(bc => bc.Books)
                        .CountAsync(),
                    TotalCollections = await _context.BookCollections
                        .Where(bc => bc.UserId == user.Id)
                        .CountAsync(),
                    BooksForMatura = await _context.BookCollections
                        .Where(bc => bc.UserId == user.Id)
                        .SelectMany(bc => bc.Books)
                        .CountAsync(b => b.IsForMatura),
                    MemberSince = user.RegistrationDate,
                    LastActivity = DateTime.Now
                };

                var model = new ProfileViewModel
                {
                    User = user,
                    Statistics = userStats
                };

                return View(model);
            }
            catch (Exception)
            {
                throw;
            }
        }

        // ==========================================
        // НОВИ МЕТОДИ ЗА СМЯНА НА ПАРОЛАТА
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendPasswordResetCode()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // Генерира сигурен 6-цифрен код чрез Identity
            var code = await _userManager.GenerateTwoFactorTokenAsync(user, "Email");

            // За тестване в конзолата (ако нямаш реален SMTP)
            Console.WriteLine($"!!! КОДЪТ ЗА СИГУРНОСТ ЗА {user.Email} Е: {code} !!!");

            string message = $@"
                <div style='font-family: Arial, sans-serif; text-align: center; padding: 20px;'>
                    <h2 style='color: #10b981;'>Смяна на парола</h2>
                    <p>Здравейте, {user.FirstName}. Заявихте смяна на паролата си.</p>
                    <p>Вашият код за сигурност е:</p>
                    <h1 style='letter-spacing: 5px; color: #333; background: #f3f4f6; padding: 10px; border-radius: 8px; display: inline-block;'>{code}</h1>
                    <p><small>Ако не сте заявили тази промяна, моля игнорирайте този имейл.</small></p>
                </div>";

            await _emailSender.SendEmailAsync(user.Email, "Код за смяна на парола - Narodnici", message);

            return Json(new { success = true, message = "Кодът е изпратен успешно на вашия имейл! (Проверете конзолата на Visual Studio, ако нямате SMTP)" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePasswordWithCode(string code, string newPassword)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // 1. Проверяваме дали 6-цифреният код съвпада
            var isCodeValid = await _userManager.VerifyTwoFactorTokenAsync(user, "Email", code);

            if (!isCodeValid)
            {
                return Json(new { success = false, message = "Грешен или изтекъл код за сигурност!" });
            }

            // 2. Сменяме паролата
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);

            if (result.Succeeded)
            {
                return Json(new { success = true, message = "Паролата е сменена успешно!" });
            }

            return Json(new { success = false, message = "Възникна грешка. Може би паролата е твърде слаба." });
        }
    }
}