using CsvHelper;
using Microsoft.AspNetCore.Mvc;
using ProgettoTesi.Models;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace ProgettoTesi.Controllers
{
    public class CryptoController : Controller
    {
        private readonly ILogger<CryptoController> _logger;

        private static readonly HttpClient client = new HttpClient();

        private readonly IConfiguration _config;

        private readonly IWebHostEnvironment Env;

        private static String? _path { get; set; }

        public CryptoController(ILogger<CryptoController> logger, IConfiguration config, IWebHostEnvironment env)
        {
            _logger = logger;
            _config = config;
            Env = env;
            if (Env.IsDevelopment())
            {
                _path = _config.GetSection("pathDevelopment").Get<string>();
            }
            else
            {
                _path = _config.GetSection("pathProduction").Get<string>();
            }
            _logger.LogInformation(_path);
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
        public async Task<IActionResult> _ListCrypto(CryptoViewModel vm)
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
                var jsonResponse = await client.GetStringAsync(requestUri: $"{_path}/getapicoinslist/");
                using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                JsonElement root = doc.RootElement;
                root.TryGetProperty("data", out root);
                root[0].TryGetProperty("screen_data", out root);
                root.TryGetProperty("crypto_data", out root);
                var list = root.EnumerateArray().ToList();
                var vmodel = list.Select(x => new CryptoViewModel()
                {
                    Name = x.GetProperty("name").ToString(),
                    Currency_symbol = x.GetProperty("currency_symbol").ToString(),
                    Inst_price_usd = x.GetProperty("inst_price_usd").ToString(),
                    Change_percent_1d = x.GetProperty("change_percent_1d").ToString(),
                    Change_percent_7d = x.GetProperty("change_percent_7d").ToString(),
                    Inst_price_btc = x.GetProperty("inst_price_btc").ToString(),
                    Inst_market_cap_plain = x.GetProperty("inst_market_cap_plain").ToString(),
                    Volume_24h_usd_plain = x.GetProperty("volume_24h_usd_plain").ToString()
                })
                .ToList()
                .AsReadOnly();

                var records = vmodel;
                using (var writer = new StreamWriter("wwwroot/csv/exportCrypto.csv"))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(records);
                }

                return PartialView("_ListCrypto", vmodel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return Json(new
            {
                success = false,
                errorMsg = "Si è verificato un errore durante il caricamento delle crypto"
            });
        }

        [HttpGet]
        public ActionResult Download()
        {
            try
            {
                var file = "~/csv/exportCrypto.csv";
                var fileName = "exportCrypto.csv";
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