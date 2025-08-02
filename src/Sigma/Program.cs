using AntDesign.ProLayout;
using Coravel;
using LLama.Native;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sigma.Client.Services;
using Sigma.Components;
using Sigma.Components.Account;
using Sigma.Core.Common;
using Sigma.Core.Data;
using Sigma.Core.Domain.Chat;
using Sigma.Core.Domain.Interface;
using Sigma.Core.Domain.Service;
using Sigma.Core.Options;
using Sigma.Core.Repositories;
using Sigma.Core.Utils;
using Sigma.Data;
using Sigma.plugins.Functions;
using Sigma.Services;
using Sigma.Services.LLamaSharp;
using Sigma;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddControllers()
    .AddJsonOptions(config =>
    {
        config.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
        config.JsonSerializerOptions.Converters.Add(new DateTimeConverter());
        config.JsonSerializerOptions.Converters.Add(new DateTimeNullableConvert());
    });

builder.Services.AddAntDesign();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, PersistingRevalidatingAuthenticationStateProvider>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(PolicyConstants.RequireAdmin, policy => policy.RequireRole(RoleConstants.Admin));
    options.AddPolicy(PolicyConstants.RequireUser, policy => policy.RequireRole(RoleConstants.User, RoleConstants.Admin));
});

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(sp.GetService<NavigationManager>()!.BaseUri)
});
builder.Services.Configure<ProSettings>(builder.Configuration.GetSection("ProSettings"));

builder.Services.AddSingleton(sp => new FunctionService(sp, [typeof(App).Assembly, typeof(Sigma.Core.Common.SigmaFunctionAttribute).Assembly]));
builder.Services.AddScoped<FunctionTest>();

builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IModelMetricsService, ModelMetricsService>();
builder.Services.AddScoped<BackgroundJobService>();
builder.Services.AddSingleton<SimulationService>();
builder.Services.AddScoped<IHttpService, HttpService>();
builder.Services.AddScoped<IImportKMSService, ImportKMSService>();
builder.Services.AddScoped<IKernelService, KernelService>();
builder.Services.AddScoped<IKMService, KMService>();
builder.Services.AddScoped<IPluginRepository, PluginRepository>();
builder.Services.AddScoped<IApps_Repositories, Apps_Repositories>();
builder.Services.AddScoped<IKmss_Repositories, Kmss_Repositories>();
builder.Services.AddScoped<IKmsDetails_Repositories, KmsDetails_Repositories>();
builder.Services.AddScoped<IAIModels_Repositories, AIModels_Repositories>();
builder.Services.AddScoped<IChatRepository, ChatRepository>();

builder.Services.AddScoped<IUsers_Repositories, Users_Repositories>();
builder.Services.AddSingleton<ILLamaChatService, LLamaChatService>();
builder.Services.AddSingleton<ILLamaEmbeddingService, LLamaEmbeddingService>();
builder.Services.AddScoped<ILLamaSharpService, LLamaSharpService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISecurityAssessmentService, SecurityAssessmentService>();
builder.Services.AddScoped<MitreMappingService>();

builder.Services.AddQueue();
builder.Services.AddScheduler();

builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<AuditInterceptor>();

builder.Services.AddScoped<LayoutService>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

builder.Services.Configure<DBConnectionOption>(builder.Configuration.GetSection("DBConnection"));
builder.Services.Configure<LoginOption>(builder.Configuration.GetSection("Login"));
builder.Services.Configure<LLamaSharpOption>(builder.Configuration.GetSection("LLamaSharp"));
builder.Services.Configure<PrometheusConfig>(builder.Configuration.GetSection("PrometheusConfig"));

var llamaOptions = builder.Configuration.GetSection("LLamaSharp").Get<LLamaSharpOption>() ?? new();
if (llamaOptions.RunType.ToUpper() == "CPU")
{
    NativeLibraryConfig.Instance.WithCuda(false).WithLogs(true);
}
else if (llamaOptions.RunType.ToUpper() == "GPU")
{
    NativeLibraryConfig.Instance.WithCuda(true).WithLogs(true);
}

var app = builder.Build();

var prometheusConfig = builder.Configuration.GetSection("PrometheusConfig").Get<PrometheusConfig>() ?? new();
if (prometheusConfig.Enabled)
{
    app.UseMetricServer(prometheusConfig.MetricsEndpoint);
    app.UseHttpMetrics();
}

app.UseMiddleware<GlobalExceptionMiddleware>();

using var scope = app.Services.CreateScope();

//codefirst ´´½¨±í
var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
//db.Database.EnsureCreated();
db.Database.Migrate();

var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
if (userManager.FindByNameAsync("admin").GetAwaiter().GetResult() == null)
{
    var user = new ApplicationUser { UserName = "admin", Email = "admin@example.com", EmailConfirmed = true };
    userManager.CreateAsync(user, "password").GetAwaiter().GetResult();
}

app.Services.UseScheduler(scheduler =>
{
    var retentionDays = app.Configuration.GetValue<int>("Retention:ChatHistoryDays", 30);
    scheduler.Schedule(async () =>
    {
        using var cleanupScope = app.Services.CreateScope();
        var cleanupDb = cleanupScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
        await cleanupDb.ChatHistories.Where(x => x.CreatedAt < cutoff).ExecuteDeleteAsync();
    }).Daily();
});

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

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

// The render mode of /Account components is Static SSR.
app.MapWhen(ctx => ctx.Request.Path.StartsWithSegments("/Account"), second =>
{
    second.UseStaticFiles();
    second.UseStaticFiles("/Account");

    second.UseRouting();
    second.UseAntiforgery();
    second.UseEndpoints(endpoints =>
    {
        endpoints.MapRazorComponents<App>();
    });
});

// The render mode of main area is Interactive SSR.
app.MapRazorComponents<Sigma.Client.App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();


namespace Sigma;

public static class RoleConstants
{
    public const string Admin = "SigmaAdmin";
    public const string User = "SigmaUser";
}

public static class PolicyConstants
{
    public const string RequireAdmin = "RequireAdmin";
    public const string RequireUser = "RequireUser";
}

app.MapControllers();


