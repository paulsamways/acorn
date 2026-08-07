using System.Text.Json;
using Acorn;
using Acorn.Core;
using Acorn.Core.Data;
using Acorn.Core.Data.Entities;
using Acorn.Core.Extensions;
using Acorn.Core.Security;
using Acorn.Services;
using Markdig;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


internal class Program
{
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "WebApplicationBuilder is a fluent API.")]
  private static void Main(string[] args)
  {
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.ConfigureCore(builder.Configuration, builder.Environment.IsDevelopment());

    builder.Services.AddSession((options) =>
    {
      options.IdleTimeout = TimeSpan.FromMinutes(30);
      options.Cookie.Name = Constants.SessionCookie;
      options.Cookie.HttpOnly = true;
      options.Cookie.IsEssential = true;
    });

    builder.Services
      .AddDataProtection()
      .PersistKeysToDbContext<ApplicationDbContext>();

    builder.Services
      .AddDbContext<ApplicationDbContext>((o) => o.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services
      .AddIdentity<User, Role>()
      .AddEntityFrameworkStores<ApplicationDbContext>()
      .AddSignInManager()
      .AddDefaultTokenProviders();

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<IUserContextService, UserContextService>();
    builder.Services.AddContentManagement();

    builder.Services.AddControllersWithViews()
      .AddMvcOptions(static (options) =>
      {
        options.Filters.Add<AutoValidateAntiforgeryTokenAttribute>();
      })
      .AddViewOptions(static (options) =>
      {
        options.HtmlHelperOptions.ClientValidationEnabled = false;
      })
      .AddJsonOptions(options =>
      {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
      });

    // builder.Services
    //   .AddAuthentication(IdentityConstants.ApplicationScheme)
    //   .AddIdentityCookies();

    builder.Services.AddSingleton(
      new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseEmojiAndSmiley()
        .DisableHtml()
        .Build()
    );

    builder.Services.AddAntiforgery((options) =>
    {
      options.Cookie.IsEssential = true;
      options.Cookie.Name = Constants.AntiforgeryCookie;
    });

    builder.Services.Configure<IdentityOptions>(options =>
    {
      options.Password.RequireDigit = false;
      options.Password.RequireNonAlphanumeric = false;
      options.Password.RequireUppercase = false;
      options.Password.RequireLowercase = false;
      options.Password.RequiredLength = Constants.MinimumPasswordLength;
    });

    builder.Services.ConfigureApplicationCookie(options =>
    {
      options.Cookie.IsEssential = true;
      options.Cookie.Name = Constants.ApplicationCookie;
      options.LoginPath = Routes.AccountSignInUrlTemplate;
    });

    builder.Services.AddOpenApi(options =>
    {
      options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_1;
    });

    builder.Services.AddProblemDetails();

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
      var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
      dbContext.Database.Migrate();
    }

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
      app.UseExceptionHandler("/Home/Error");

      // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
      app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseRouting();

    app.UseAuthorization();

    app.UseSession();

    app.MapStaticAssets();
    app.MapOpenApi().CacheOutput();

    app.MapControllerRoute(
      name: "admin",
      pattern: "{area:exists}/{controller=Notes}/{action=Index}/{id?}");

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
      .WithStaticAssets();


    app.Run();
  }
}
