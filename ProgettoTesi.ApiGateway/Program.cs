var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseHsts();
app.UseHttpsRedirection();

var path = "";
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    path = builder.Configuration.GetSection("pathDevelopment").Get<string>();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI();
    path = builder.Configuration.GetSection("pathProduction").Get<string>();
}

app.MapGet("/getapiquotesummary/{symbol}", async (string symbol) =>
{
    var client = new HttpClient();
    return await client.GetStringAsync(requestUri: $"{path}/getapiquotesummary/{symbol}");
})
.WithName("GetApiQuoteSummary");

app.MapGet("/getapiautocomplete/{value}", async (string value) =>
{
    var client = new HttpClient();
    return await client.GetStringAsync(requestUri: $"{path}/getapiautocomplete/{value}");
})
.WithName("GetApiAutocomplete");

app.MapGet("/getapicoinslist", async () =>
{
    var client = new HttpClient();
    return await client.GetStringAsync(requestUri: $"{path}/getapicoinslist/");
})
.WithName("GetApiCoinsList");

app.MapPost("/insertholding", async (Object value) =>
{
    var client = new HttpClient();
    (await client.PostAsJsonAsync($"{path}/insertholding/", value)).EnsureSuccessStatusCode();
})
.WithName("InsertHolding");

app.MapGet("/getholdings/{value}", async (string value) =>
{
    var client = new HttpClient();
    return await client.GetStringAsync($"{path}/getholdings/{value}");
})
.WithName("GetHoldings");

app.MapDelete("/deleteholding/{value}", async (int value) =>
{
    var client = new HttpClient();
    return await client.DeleteAsync($"{path}/deleteholding/{value}");
})
.WithName("DeleteHolding");

app.Run();