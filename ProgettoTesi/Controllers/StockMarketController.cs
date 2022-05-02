using CsvHelper;
using Microsoft.AspNetCore.Mvc;
using ProgettoTesi.Models;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace ProgettoTesi.Controllers
{
    public class StockMarketController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private static readonly HttpClient client = new HttpClient();

        private readonly IConfiguration _config;

        private readonly IWebHostEnvironment _env;

        private static String? _path { get; set; }

        public StockMarketController(ILogger<HomeController> logger, IConfiguration config, IWebHostEnvironment env)
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
        }

        public IActionResult Index()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        public async Task<IActionResult> _GetApiAutocomplete(string value)
        {
            if (!ModelState.IsValid)
            {
                return Json(new
                {
                    success = false,
                    isFreeCallEnd = false,
                    errorMsg = "Nessun suggerimento trovato"
                });
            }
            try
            {
                return Json(await client.GetStringAsync(requestUri: $"{_path}/getapiautocomplete/{value}"));
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
                    errorMsg = "Nessun suggerimento trovato"
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> _StrumentoFinanziario(StrumentoFinanziarioViewModel vm)
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
                return PartialView("_StrumentoFinanziario", vmodel);
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

        [HttpGet]
        public async Task<IActionResult> _Watchlist(string stringSimboli)
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
                var listSimboli = stringSimboli.Split(",");
                var listVmodel = new List<StrumentoFinanziarioViewModel>();
                foreach (var item in listSimboli)
                {
                    var jsonResponse = await client.GetStringAsync(requestUri: $"{_path}/getapiquotesummary/{item}");
                    using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                    JsonElement root = doc.RootElement;
                    root.TryGetProperty("quoteSummary", out root);
                    root.TryGetProperty("result", out root);
                    root[0].TryGetProperty("financialData", out root);
                    var vmodel = new StrumentoFinanziarioViewModel()
                    {
                        Nome = item,
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
                    listVmodel.Add(vmodel);
                }
                var records = listVmodel;
                using (var writer = new StreamWriter("wwwroot/csv/export.csv"))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(records);
                }
                return PartialView("_Watchlist", listVmodel);
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

        [HttpGet]
        public ActionResult Download()
        {
            try
            {
                var file = "~/csv/export.csv";
                var fileName = "export.csv";
                return File(file, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return Json(new
                {
                    success = false,
                    errorMsg = "Non è stato possibile scaricare il file"
                });
            }
        }
    }
}