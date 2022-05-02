using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProgettoTesi.Data;

var builder = WebApplication.CreateBuilder(args);

var client = new SecretClient(new Uri(uriString: $"https://progettotesivault.vault.azure.net/"), new DefaultAzureCredential());
var connectionString = "";
try
{
    var secret = await client.GetSecretAsync("progettotesics");
    connectionString = secret.Value.Value.ToString();
}
catch (Exception)
{
    connectionString = builder.Configuration.GetConnectionString("connectionstring");
}

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true).AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

app.UseMigrationsEndPoint();
app.UseExceptionHandler("/Home/Error");

app.UseHsts();
app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();