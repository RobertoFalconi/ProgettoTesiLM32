using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProgettoTesi.Models;
using System.Diagnostics;
using System.Net;
using System.Security.Claims;
using System.Text.Json;

namespace ProgettoTesi.Controllers
{
    public class HoldingsController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private static readonly HttpClient client = new HttpClient();

        private readonly UserManager<IdentityUser> _userManager;

        private readonly IConfiguration _config;

        private readonly IWebHostEnvironment _env;

        private static String? _path { get; set; }

        public HoldingsController(ILogger<HomeController> logger, IConfiguration config, IWebHostEnvironment env, UserManager<IdentityUser> userManager)
        {
            _logger = logger;
            _config = config;
            _env = env;
            if (_env.IsDevelopment())
            {
                _path = _config.GetSection("pathDevelopment").Get<string>();
            }
            else
            {
                _path = _config.GetSection("pathProduction").Get<string>();
            }
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        public async Task<IActionResult> InsertHolding(StrumentoFinanziarioViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return Json(new
                {
                    success = false,
                    errorMsg = "Sono presenti dei campi non validi"
                });
            }
            try
            {
                var request = new StrumentoFinanziarioViewModel();
                request.Simbolo = vm.Simbolo;
                request.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                request.Descrizione = vm.Descrizione;
                var stringContent = JsonSerializer.Serialize(request);
                var response = await client.PostAsJsonAsync($"{_path}/insertholding", stringContent);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return Json(new
                    {
                        success = true,
                        errorMsg = "Strumento finanziario inserito con successo tra le holdings"
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        errorMsg = "Non è stato possibile inserire lo strumento finanziario tra le holdings"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return Json(new
                {
                    success = false,
                    errorMsg = "Non è stato possibile inserire lo strumento finanziario tra le holdings"
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> DeleteHoldings(StrumentoFinanziarioViewModel vm)
        {
            try
            {
                await client.DeleteAsync($"{_path}/deleteholding/{vm.Id}");

                return Json(new
                {
                    success = true,
                    errorMsg = "Strumento finanziario eliminato con successo dalle holdings"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return Json(new
                {
                    success = false,
                    errorMsg = "Non è stato possibile cancellare lo strumento finanziario tra le holdings"
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> _HoldingsList()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return PartialView("_HoldingsList", new List<StrumentoFinanziarioViewModel>());
            }
            try
            {
                var jsonResponse = await client.GetStringAsync(requestUri: $"{_path}/getholdings/{userId}");
                using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                JsonElement root = doc.RootElement;
                var list = root.EnumerateArray().ToList();
                var vmodel = list.Select(x => new StrumentoFinanziarioViewModel()
                {
                    Id = Int32.Parse(x.GetProperty("id").ToString()),
                    //Nome = x.GetProperty("nome").ToString(),
                    Simbolo = x.GetProperty("simbolo").ToString(),
                    Descrizione = x.GetProperty("descrizione").ToString()
                })
                .ToList()
                .AsReadOnly();

                return PartialView("_HoldingsList", vmodel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return Json(new
            {
                success = false,
                errorMsg = "Si è verificato un errore durante il caricamento delle holdings"
            });
        }

        [HttpGet]
        public async Task<IActionResult> _QuotazioneModal(StrumentoFinanziarioViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return Json(new
                {
                    success = false,
                    isFreeCallEnd = false,
                    errorMsg = "Sono presenti dei campi non validi"
                });
            }
            try
            {
                vm.Simbolo = vm.Simbolo?.Trim();
                var formatoCorretto = vm.Simbolo?.Contains("(");
                if (formatoCorretto.HasValue && formatoCorretto.Value)
                {
                    vm.Simbolo = vm.Simbolo?.Split("(")[1].Trim().Trim(')');
                }
                else
                {
                    var jsonSuggestion = await client.GetStringAsync(requestUri: $"{_path}/getapiautocomplete/{vm.Simbolo}");
                    using JsonDocument sug = JsonDocument.Parse(jsonSuggestion);
                    vm.Simbolo = sug.RootElement.GetProperty("ResultSet").GetProperty("Result")[0].GetProperty("symbol").ToString();
                }

                var jsonResponse = await client.GetStringAsync(requestUri: $"{_path}/getapiquotesummary/{vm.Simbolo}");
                using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                JsonElement root = doc.RootElement;
                root.TryGetProperty("quoteSummary", out root);
                root.TryGetProperty("result", out root);
                var descrizione = root[0].GetProperty("assetProfile").GetProperty("longBusinessSummary").ToString();
                root[0].TryGetProperty("financialData", out root);
                var vmodel = new StrumentoFinanziarioViewModel()
                {
                    Nome = vm.Simbolo,
                    Simbolo = vm.Simbolo,
                    Descrizione = descrizione,
                    TotalRevenue = root.TryGetProperty("totalRevenue", out JsonElement totalRevenue) ? (totalRevenue.TryGetProperty("fmt", out JsonElement totalRevenueFmt) ? totalRevenueFmt.ToString() : "N.A.") : "N.A.",
                    GrossProfits = root.TryGetProperty("grossProfits", out JsonElement grossProfits) ? (grossProfits.TryGetProperty("fmt", out JsonElement grossProfitsFmt) ? grossProfitsFmt.ToString() : "N.A.") : "N.A.",
                    ProfitMargins = root.TryGetProperty("profitMargins", out JsonElement profitMargins) ? (profitMargins.TryGetProperty("fmt", out JsonElement profitMarginsFmt) ? profitMarginsFmt.ToString() : "N.A.") : "N.A.",
                    EbitdaMargins = root.TryGetProperty("ebitdaMargins", out JsonElement ebitdaMargins) ? (ebitdaMargins.TryGetProperty("fmt", out JsonElement ebitdaMarginsFmt) ? ebitdaMarginsFmt.ToString() : "N.A.") : "N.A.",
                    CurrentPrice = root.TryGetProperty("currentPrice", out JsonElement currentPrice) ? (currentPrice.TryGetProperty("fmt", out JsonElement currentPriceFmt) ? currentPriceFmt.ToString() : "N.A.") : "N.A.",
                    TargetHighPrice = root.TryGetProperty("targetHighPrice", out JsonElement targetHighPrice) ? (targetHighPrice.TryGetProperty("fmt", out JsonElement targetHighPriceFmt) ? targetHighPriceFmt.ToString() : "N.A.") : "N.A.",
                    TargetMedianPrice = root.TryGetProperty("targetMedianPrice", out JsonElement targetMedianPrice) ? (targetMedianPrice.TryGetProperty("fmt", out JsonElement targetMedianPriceFmt) ? targetMedianPriceFmt.ToString() : "N.A.") : "N.A.",
                    TargetLowPrice = root.TryGetProperty("targetLowPrice", out JsonElement targetLowPrice) ? (targetLowPrice.TryGetProperty("fmt", out JsonElement targetLowPriceFmt) ? targetLowPriceFmt.ToString() : "N.A.") : "N.A.",
                    EarningsGrowth = root.TryGetProperty("earningsGrowth", out JsonElement earningsGrowth) ? (earningsGrowth.TryGetProperty("fmt", out JsonElement earningsGrowthFmt) ? earningsGrowthFmt.ToString() : "N.A.") : "N.A.",
                    RevenueGrowth = root.TryGetProperty("revenueGrowth", out JsonElement revenueGrowth) ? (revenueGrowth.TryGetProperty("fmt", out JsonElement revenueGrowthFmt) ? revenueGrowthFmt.ToString() : "N.A.") : "N.A.",
                    OperatingCashflow = root.TryGetProperty("operatingCashflow", out JsonElement operatingCashflow) ? (operatingCashflow.TryGetProperty("fmt", out JsonElement operatingCashflowFmt) ? operatingCashflowFmt.ToString() : "N.A.") : "N.A.",
                    FinancialCurrency = root.TryGetProperty("financialCurrency", out JsonElement financialCurrency) ? financialCurrency.ToString() : "N.A."
                };
                return PartialView("_QuotazioneModal", vmodel);
            }
            catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.InternalServerError)
            {
                return Json(new
                {
                    success = false,
                    isFreeCallEnd = true,
                    errorMsg = "Non sono più disponibili ricerche gratuite per oggi"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return Json(new
                {
                    success = false,
                    isFreeCallEnd = false,
                    errorMsg = "Lo strumento finanziario selezionato non contiene informazioni pubbliche e consultabili oppure non esiste"
                });
            }
        }

    }
}