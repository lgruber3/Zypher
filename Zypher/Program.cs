using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Model;
using Zypher;
using Zypher.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContextFactory<ContextClass>(
    options => options.UseMySql(builder.Configuration
            .GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 36))
    )
);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
    });

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthService>();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.UseRouting();
app.UseAntiforgery();
app.UseEndpoints(endpoints =>
{
    endpoints.MapHub<TypingHub>("/typingHub");
});

app.Run();