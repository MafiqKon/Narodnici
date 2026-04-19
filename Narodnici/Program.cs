using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Narodnici.Data;
using Narodnici.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Настройка на базата данни
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// РЕГИСТРИРАНЕ НА УСЛУГАТА ЗА ИМЕЙЛИ
builder.Services.AddTransient<Narodnici.Services.IEmailSender, Narodnici.Services.EmailSender>();

// 2. Настройка на Identity (Потребители и Роли)
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false; // За по-лесно тестване

    // Опростени пароли за development
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 3;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders(); // ВАЖНО: Активира системата за генериране на 6-цифрени кодове!

// 3. Добавяне на MVC и Razor Pages
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// 4. Конфигурация на HTTP пайплайна
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// === 5. АВТОМАТИЧНО СЪЗДАВАНЕ И ПЪЛНЕНЕ НА БАЗАТА ===
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        context.Database.Migrate();
        await DbInitializer.Initialize(context, userManager, roleManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Грешка при създаване/инициализация на базата данни.");
    }
}

app.Run();