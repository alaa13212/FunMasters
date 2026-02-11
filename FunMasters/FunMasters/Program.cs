using FunMasters.Authentication;
using FunMasters.Components;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FunMasters.Data;
using FunMasters.Endpoints;
using FunMasters.Jobs;
using FunMasters.Services;
using FunMasters.Shared.Services;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization(options =>
    {
        // Serialize all claims including our custom avatar_timestamp claim
        options.SerializeAllClaims = true;
    });

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, PersistingRevalidatingAuthenticationStateProvider>();
builder.Services.AddHostedService<QueueManagerJob>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                       throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContextFactory<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<ApplicationDbContext>(sp =>
    sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());

builder.Services.AddResponseCompression()
    .AddRequestDecompression();

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
    {
        options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+ \'\"";
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// Configure cookie to return 401/403 for API calls instead of redirecting
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api"))
        {
            ctx.Response.StatusCode = 401;
            return Task.CompletedTask;
        }
        ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api"))
        {
            ctx.Response.StatusCode = 403;
            return Task.CompletedTask;
        }
        ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
});

// Add claims transformation to include avatar timestamp
builder.Services.AddScoped<IClaimsTransformation, AvatarClaimsTransformation>();

builder.Services.AddHttpClient<IgdbService>();
builder.Services.AddHttpClient<HltbService>();
builder.Services.AddScoped<GameCoverStorage>();
builder.Services.AddScoped<AvatarStorage>();
builder.Services.AddScoped<QueueManager>();

// Enable access to HttpContext in services
builder.Services.AddHttpContextAccessor();

// Business logic services (server implementations of shared interfaces)
builder.Services.AddScoped<ISuggestionApiService, SuggestionService>();
builder.Services.AddScoped<IRatingApiService, RatingService>();
builder.Services.AddScoped<IAdminApiService, AdminService>();
builder.Services.AddScoped<IAccountApiService, AccountService>();
builder.Services.AddScoped<IIgdbApiService, IgdbApiService>();
builder.Services.AddScoped<IHltbApiService, HltbApiService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Create a file provider with change monitoring enabled
var uploadsPath = Path.Combine(app.Environment.WebRootPath, "uploads");
var fileProvider = new PhysicalFileProvider(uploadsPath);

app.UseResponseCompression();
app.UseRequestDecompression();

// Serve static files from /uploads/avatars
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = fileProvider,
    RequestPath = "/uploads",
    ServeUnknownFileTypes = true
});

app.UseStaticFiles();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(FunMasters.Client._Imports).Assembly);

// Map API endpoints
app.MapSuggestionEndpoints();
app.MapRatingEndpoints();
app.MapAdminEndpoints();
app.MapAccountEndpoints();
app.MapIgdbEndpoints();
app.MapHltbEndpoints();

using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if(db.Database.GetPendingMigrations().Any())
        db.Database.Migrate();
}

using(var scope = app.Services.CreateScope()){
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    string[] roles = ["Admin", "Member"];
    foreach (var role in roles)
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole<Guid>(role));

    var adminEmail = "Admin";
    var admin = await userManager.FindByEmailAsync(adminEmail);
    if (admin == null)
    {
        admin = new ApplicationUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true, RequirePasswordChange = false };
        await userManager.CreateAsync(admin, "Admin123!");
    }

    if (!await userManager.IsInRoleAsync(admin, "Admin"))
        await userManager.AddToRoleAsync(admin, "Admin");
}

app.Run();