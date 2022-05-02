using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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

builder.Services.AddDbContext<StockDb>(options => options.UseSqlServer(connectionString));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseHsts();
app.UseHttpsRedirection();

var xRapidKey = builder.Configuration.GetSection("x-rapidapi-key").Get<string>();

var currentApiCall = 0;
var yfApiKeys = builder.Configuration.GetSection("X-API-KEY-LIST").Get<List<string>>();

app.MapGet("/getapiquotesummary/{symbol}", async (string symbol) =>
{
    var currentApiKey = (currentApiCall++ % yfApiKeys.Count);
    var client = new HttpClient();
    var request = new HttpRequestMessage
    {
        Method = HttpMethod.Get,
        RequestUri = new Uri(uriString: $"https://yfapi.net/v11/finance/quoteSummary/{symbol}?lang=it&region=IT&modules=assetProfile%2CfinancialData"),
        Headers =
        {
            { "X-API-KEY", yfApiKeys[currentApiKey] },
        },
    };
    using var response = await client.SendAsync(request);
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadAsStringAsync();
})
    .WithName("GetApiQuoteSummary");

app.MapGet("/getapiautocomplete/{value}", async (string value) =>
{
    var currentApiKey = (currentApiCall++ % yfApiKeys.Count);
    var client = new HttpClient();
    var request = new HttpRequestMessage
    {
        Method = HttpMethod.Get,
        RequestUri = new Uri(uriString: $"https://yfapi.net/v6/finance/autocomplete?region=US&lang=en&query={value}"),
        Headers =
        {
            { "X-API-KEY", yfApiKeys[currentApiKey] },
        },
    };
    using var response = await client.SendAsync(request);
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadAsStringAsync();
})
    .WithName("GetApiAutocomplete");

app.MapGet("/getapicoinslist", async () =>
{
    var client = new HttpClient();
    var request = new HttpRequestMessage
    {
        Method = HttpMethod.Get,
        RequestUri = new Uri(uriString: $"https://investing-cryptocurrency-markets.p.rapidapi.com/coins/list?edition_currency_id=12&time_utc_offset=28800&lang_ID=1&sort=PERC1D_DN&page=1"),
        Headers =
        {
            { "x-rapidapi-host", "investing-cryptocurrency-markets.p.rapidapi.com" },
            { "x-rapidapi-key", xRapidKey },
        },
    };
    using var response = await client.SendAsync(request);
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadAsStringAsync();
})
    .WithName("GetApiCoinsList");

app.MapPost("/insertholding", async (StockDb db, Object content) =>
{
    var stock = JsonSerializer.Deserialize<StockModel>(json: content.ToString());
    await db.Holdings.AddAsync(stock);
    await db.SaveChangesAsync();
    Results.Ok();
});

app.MapGet("/getholdings/{value}", async (StockDb db, string value) => await db.Holdings.Where(x => x.UserId.Contains(value)).ToListAsync());

app.MapDelete("/deleteholding/{value}", async (StockDb db, string value) =>
{
    var stock = await db.Holdings.FindAsync(long.Parse(value));
    if (stock is null)
    {
        return Results.NotFound();
    }
    db.Holdings.Remove(stock);
    await db.SaveChangesAsync();
    return Results.Ok();
});

app.Run();

public class StockModel
{
    public long? Id { get; set; }
    public string? UserId { get; set; }
    public string? Simbolo { get; set; }
    public string? Descrizione { get; set; }
}

public class StockDb : DbContext
{
    public StockDb(DbContextOptions options) : base(options) { }
    public DbSet<StockModel> Holdings { get; set; }
}