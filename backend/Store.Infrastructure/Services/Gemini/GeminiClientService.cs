using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Store.Application.Abstractions.Services;
using Store.Application.Services.AiChat.Tools;
using Store.Contracts.AiChat;

namespace Store.Infrastructure.Services.Gemini;

public class GeminiClientService : IGeminiClient
{
    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;
    private readonly AiToolRegistry _toolRegistry;

    // Circuit Breaker State (Static to survive across scoped lifecycles)
    private static int _consecutiveErrors = 0;
    private static DateTime? _circuitOpenedUntil = null;
    private static readonly object _circuitLock = new();

    public GeminiClientService(HttpClient httpClient, IOptions<GeminiOptions> options, AiToolRegistry toolRegistry)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _toolRegistry = toolRegistry;
    }

    public async Task<GeminiResult> GenerateContentWithToolsAsync(List<AiChatMessageDto> history, string currentRoute, string? activeFormKey, StoreContextSnapshot? context = null, CancellationToken cancellationToken = default)
    {
        if (!_options.HasLiveApiKey)
        {
            if (_options.RequireLive)
            {
                throw new InvalidOperationException("Gemini live mode requires Gemini:ApiKey.");
            }

            if (_options.AllowMockFallback)
            {
                return MarkFallback(
                    GenerateMockResult(history, currentRoute, activeFormKey, context),
                    "Gemini fallback enabled by configuration.");
            }

            throw new InvalidOperationException("Gemini API key is missing and mock fallback is disabled.");
        }

        // 1. Check Circuit Breaker
        lock (_circuitLock)
        {
            if (_circuitOpenedUntil.HasValue)
            {
                if (_circuitOpenedUntil.Value > DateTime.UtcNow)
                {
                    if (_options.AllowMockFallback)
                    {
                        // Graceful fallback to mock mode when circuit breaker is active
                        return MarkFallback(
                            GenerateMockResult(history, currentRoute, activeFormKey, context),
                            "Gemini circuit breaker is active after recent API failures.");
                    }
                    else
                    {
                        throw new InvalidOperationException("Gemini circuit breaker is active and mock fallback is disabled.");
                    }
                }
                else
                {
                    // Time expired, reset
                    _circuitOpenedUntil = null;
                    _consecutiveErrors = 0;
                }
            }
        }

        var requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{_options.Model}:generateContent?key={_options.ApiKey}";

        // 2. Map History
        var contents = MapHistoryToGeminiFormat(history);

        var requestBody = new
        {
            contents = contents,
            systemInstruction = new { parts = new[] { new { text = SystemPrompt.Build(currentRoute, activeFormKey, context) } } },
            tools = new[] { new { functionDeclarations = _toolRegistry.GetWhitelistedDeclarations() } },
            safetySettings = GeminiTools.GetSafetySettings()
        };

        // 3. Post with retry (Exponential Backoff for 429)
        int maxRetries = 2; // Only 2 attempts (1 retry) to keep response times fast
        int delayMs = 1000;
        HttpResponseMessage? response = null;

        try
        {
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(Math.Max(5, _options.TimeoutSeconds))); // timeout from options

                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                    request.Headers.Add("x-goog-api-key", _options.ApiKey);
                    request.Content = JsonContent.Create(requestBody);
                    response = await _httpClient.SendAsync(request, cts.Token);

                    if (response.IsSuccessStatusCode)
                    {
                        break;
                    }

                    if ((int)response.StatusCode == 429)
                    {
                        if (attempt < maxRetries - 1)
                        {
                            var retryDelay = delayMs;
                            if (response.Headers.RetryAfter != null)
                            {
                                if (response.Headers.RetryAfter.Delta.HasValue)
                                {
                                    retryDelay = (int)response.Headers.RetryAfter.Delta.Value.TotalMilliseconds;
                                }
                                else if (response.Headers.RetryAfter.Date.HasValue)
                                {
                                    var delta = response.Headers.RetryAfter.Date.Value - DateTimeOffset.UtcNow;
                                    if (delta.TotalMilliseconds > 0)
                                    {
                                        retryDelay = (int)delta.TotalMilliseconds;
                                    }
                                }
                            }

                            await Task.Delay(retryDelay, cancellationToken);
                            delayMs *= 2;
                            continue;
                        }
                    }

                    // If not 429 or final retry failed, throw to trigger circuit breaker logic
                    throw new HttpRequestException($"Gemini API returned error code {response.StatusCode}", null, response.StatusCode);
                }
                catch (HttpRequestException ex)
                {
                    if (attempt == maxRetries - 1)
                    {
                        if (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                        {
                            HandleRateLimit();
                        }
                        else
                        {
                            HandleFailure();
                        }
                        throw;
                    }
                    await Task.Delay(delayMs, cancellationToken);
                    delayMs *= 2;
                }
                catch (TaskCanceledException)
                {
                    if (attempt == maxRetries - 1)
                    {
                        HandleFailure();
                        throw;
                    }
                    await Task.Delay(delayMs, cancellationToken);
                    delayMs *= 2;
                }
            }

            if (response == null || !response.IsSuccessStatusCode)
            {
                if (response?.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    HandleRateLimit();
                }
                else
                {
                    HandleFailure();
                }

                if (_options.AllowMockFallback)
                {
                    // Fall back to MockResult
                    return MarkFallback(
                        GenerateMockResult(history, currentRoute, activeFormKey, context),
                        $"Gemini API returned an unsuccessful response: {response?.StatusCode.ToString() ?? "no response"}.");
                }
                else
                {
                    throw new HttpRequestException($"Gemini API returned an unsuccessful response: {response?.StatusCode.ToString() ?? "no response"}.", null, response?.StatusCode);
                }
            }

            // Reset failure count on success
            lock (_circuitLock)
            {
                _consecutiveErrors = 0;
                _circuitOpenedUntil = null;
            }

            var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseGeminiJson(jsonString);
        }
        catch (Exception ex)
        {
            if (_options.AllowMockFallback)
            {
                // Graceful fallback to Mock mode on any network error or rate limit exhaustion
                return MarkFallback(
                    GenerateMockResult(history, currentRoute, activeFormKey, context),
                    $"Gemini API request failed: {ex.GetType().Name}.");
            }
            throw;
        }
    }

    private static GeminiResult MarkFallback(GeminiResult result, string reason)
    {
        result.UsedFallback = true;
        result.FallbackReason = reason;
        return result;
    }

    private void HandleFailure()
    {
        lock (_circuitLock)
        {
            _consecutiveErrors++;
            if (_consecutiveErrors >= 5)
            {
                _circuitOpenedUntil = DateTime.UtcNow.AddMinutes(2);
                _consecutiveErrors = 0;
            }
        }
    }

    private void HandleRateLimit()
    {
        lock (_circuitLock)
        {
            _circuitOpenedUntil = DateTime.UtcNow.AddMinutes(1);
        }
    }

    private List<object> MapHistoryToGeminiFormat(List<AiChatMessageDto> history)
    {
        var list = new List<object>();

        foreach (var msg in history)
        {
            string role = msg.Role.ToLower();
            if (role == "user")
            {
                list.Add(new
                {
                    role = "user",
                    parts = new[] { new { text = $"<user_message>{msg.Message}</user_message>" } }
                });
            }
            else if (role == "assistant" || role == "model")
            {
                if (!string.IsNullOrEmpty(msg.FunctionName)) // Function call message
                {
                    // Deserialized function call argument
                    var args = string.IsNullOrEmpty(msg.Message) ? (JsonNode)new JsonObject() : JsonSerializer.Deserialize<JsonNode>(msg.Message);
                    list.Add(new
                    {
                        role = "model",
                        parts = new[]
                        {
                            new
                            {
                                functionCall = new
                                {
                                    name = msg.FunctionName,
                                    args = args
                                }
                            }
                        }
                    });
                }
                else
                {
                    list.Add(new
                    {
                        role = "model",
                        parts = new[] { new { text = msg.Message } }
                    });
                }
            }
            else if (role == "tool" || role == "function")
            {
                var responseObj = string.IsNullOrEmpty(msg.Message) ? (JsonNode)new JsonObject() : JsonSerializer.Deserialize<JsonNode>(msg.Message);
                list.Add(new
                {
                    role = "function",
                    parts = new[]
                    {
                        new
                        {
                            functionResponse = new
                            {
                                name = msg.FunctionName ?? "unknown",
                                response = new { result = responseObj }
                            }
                        }
                    }
                });
            }
        }

        return list;
    }

    private GeminiResult ParseGeminiJson(string jsonString)
    {
        var result = new GeminiResult();
        try
        {
            var node = JsonNode.Parse(jsonString);
            var candidate = node?["candidates"]?[0];
            var part = candidate?["content"]?.AsObject().ContainsKey("parts") == true ? candidate?["content"]?["parts"]?[0] : null;

            if (part == null)
            {
                result.Text = "Maaf, sistem tidak menerima respon yang valid dari model.";
                return result;
            }

            if (part.AsObject().ContainsKey("functionCall"))
            {
                var fc = part["functionCall"];
                result.HasFunctionCall = true;
                result.FunctionName = fc?["name"]?.ToString();
                result.Arguments = fc?["args"]?.ToJsonString();
            }
            else if (part.AsObject().ContainsKey("text"))
            {
                result.Text = part["text"]?.ToString() ?? string.Empty;
            }
        }
        catch (Exception)
        {
            result.Text = "Gagal memproses respon dari server AI.";
        }

        return result;
    }

    private class MockProduct
    {
        public string ProductId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal Stock { get; set; }
    }

    private class MockCustomer
    {
        public string CustomerId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    private class MockSupplier
    {
        public string SupplierId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    private List<MockProduct> ParseProducts(string json)
    {
        var list = new List<MockProduct>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    decimal price = 0;
                    if (el.TryGetProperty("sellingPrice", out var priceEl))
                    {
                        price = priceEl.GetDecimal();
                    }

                    decimal stock = 0;
                    if (el.TryGetProperty("currentStock", out var stockEl))
                    {
                        stock = stockEl.GetDecimal();
                    }

                    list.Add(new MockProduct
                    {
                        ProductId = el.GetProperty("productId").GetGuid().ToString(),
                        Name = el.GetProperty("name").GetString() ?? "",
                        Price = price,
                        Stock = stock
                    });
                }
            }
        }
        catch { }
        return list;
    }

    private List<MockCustomer> ParseCustomers(string json)
    {
        var list = new List<MockCustomer>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    list.Add(new MockCustomer
                    {
                        CustomerId = el.GetProperty("customerId").GetGuid().ToString(),
                        Name = el.GetProperty("name").GetString() ?? ""
                    });
                }
            }
        }
        catch { }
        return list;
    }

    private List<MockSupplier> ParseSuppliers(string json)
    {
        var list = new List<MockSupplier>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    list.Add(new MockSupplier
                    {
                        SupplierId = el.GetProperty("supplierId").GetGuid().ToString(),
                        Name = el.GetProperty("name").GetString() ?? ""
                    });
                }
            }
        }
        catch { }
        return list;
    }

    private GeminiResult GenerateMockResult(List<AiChatMessageDto> history, string currentRoute, string? activeFormKey, StoreContextSnapshot? context)
    {
        var lastMsg = history.LastOrDefault();
        if (lastMsg == null)
        {
            return new GeminiResult
            {
                Text = "Halo! Saya SobatEntong AI. Ada yang bisa saya bantu?"
            };
        }

        // Get the initial user prompt (usually the first message, or the last user message before any tool calls)
        string userText = "";
        for (int i = history.Count - 1; i >= 0; i--)
        {
            if (history[i].Role.Equals("user", StringComparison.OrdinalIgnoreCase))
            {
                userText = history[i].Message;
                break;
            }
        }

        userText = userText ?? "";

        // Determine intent based on userText
        bool isSale = userText.Contains("jual", StringComparison.OrdinalIgnoreCase) || userText.Contains("kasir", StringComparison.OrdinalIgnoreCase) || userText.Contains("transaksi", StringComparison.OrdinalIgnoreCase);
        bool isPurchase = userText.Contains("beli", StringComparison.OrdinalIgnoreCase) || userText.Contains("supplier", StringComparison.OrdinalIgnoreCase) || userText.Contains("kulakan", StringComparison.OrdinalIgnoreCase);
        bool isStockCheck = userText.Contains("stok", StringComparison.OrdinalIgnoreCase) && !userText.Contains("koreksi", StringComparison.OrdinalIgnoreCase) && !userText.Contains("penyesuaian", StringComparison.OrdinalIgnoreCase) && !userText.Contains("rendah", StringComparison.OrdinalIgnoreCase) && !userText.Contains("nilai", StringComparison.OrdinalIgnoreCase) && !userText.Contains("valuasi", StringComparison.OrdinalIgnoreCase) && !userText.Contains("menipis", StringComparison.OrdinalIgnoreCase) && !userText.Contains("tipis", StringComparison.OrdinalIgnoreCase) && !userText.Contains("habis", StringComparison.OrdinalIgnoreCase);
        bool isLowStock = userText.Contains("stok rendah", StringComparison.OrdinalIgnoreCase) || userText.Contains("low stock", StringComparison.OrdinalIgnoreCase) || userText.Contains("stok habis", StringComparison.OrdinalIgnoreCase) || userText.Contains("menipis", StringComparison.OrdinalIgnoreCase) || userText.Contains("tipis", StringComparison.OrdinalIgnoreCase) || userText.Contains("habis", StringComparison.OrdinalIgnoreCase);
        bool isStockAdjustment = userText.Contains("koreksi", StringComparison.OrdinalIgnoreCase) || userText.Contains("penyesuaian", StringComparison.OrdinalIgnoreCase) || userText.Contains("adjust", StringComparison.OrdinalIgnoreCase);
        bool isReceivable = userText.Contains("piutang", StringComparison.OrdinalIgnoreCase) || userText.Contains("receivable", StringComparison.OrdinalIgnoreCase);
        bool isPayable = userText.Contains("hutang", StringComparison.OrdinalIgnoreCase) || userText.Contains("payable", StringComparison.OrdinalIgnoreCase);
        bool isPriceCheck = userText.Contains("harga", StringComparison.OrdinalIgnoreCase) || userText.Contains("price", StringComparison.OrdinalIgnoreCase);

        bool isCreateProduct = userText.Contains("tambah produk", StringComparison.OrdinalIgnoreCase) || userText.Contains("buat produk", StringComparison.OrdinalIgnoreCase) || userText.Contains("produk baru", StringComparison.OrdinalIgnoreCase);
        bool isCreateCustomer = userText.Contains("tambah pelanggan", StringComparison.OrdinalIgnoreCase) || userText.Contains("buat pelanggan", StringComparison.OrdinalIgnoreCase) || userText.Contains("pelanggan baru", StringComparison.OrdinalIgnoreCase);
        bool isCreateSupplier = userText.Contains("tambah supplier", StringComparison.OrdinalIgnoreCase) || userText.Contains("buat supplier", StringComparison.OrdinalIgnoreCase) || userText.Contains("supplier baru", StringComparison.OrdinalIgnoreCase);

        bool isTopSelling = userText.Contains("laku", StringComparison.OrdinalIgnoreCase) || userText.Contains("laris", StringComparison.OrdinalIgnoreCase) || userText.Contains("best seller", StringComparison.OrdinalIgnoreCase);
        bool isDailySales = userText.Contains("harian", StringComparison.OrdinalIgnoreCase) || userText.Contains("tren", StringComparison.OrdinalIgnoreCase);
        bool isProfit = userText.Contains("untung", StringComparison.OrdinalIgnoreCase) || userText.Contains("laba", StringComparison.OrdinalIgnoreCase) || userText.Contains("profit", StringComparison.OrdinalIgnoreCase) || userText.Contains("margin", StringComparison.OrdinalIgnoreCase);
        bool isExpense = userText.Contains("pengeluaran", StringComparison.OrdinalIgnoreCase) || userText.Contains("biaya", StringComparison.OrdinalIgnoreCase) || userText.Contains("expense", StringComparison.OrdinalIgnoreCase);
        bool isStockValuation = userText.Contains("nilai stok", StringComparison.OrdinalIgnoreCase) || userText.Contains("inventaris", StringComparison.OrdinalIgnoreCase) || userText.Contains("valuasi", StringComparison.OrdinalIgnoreCase);
        bool isDashboard = userText.Contains("ringkasan", StringComparison.OrdinalIgnoreCase) || userText.Contains("dashboard", StringComparison.OrdinalIgnoreCase) || userText.Contains("summary", StringComparison.OrdinalIgnoreCase) || userText.Contains("omzet", StringComparison.OrdinalIgnoreCase) || userText.Contains("pendapatan", StringComparison.OrdinalIgnoreCase) || userText.Contains("revenue", StringComparison.OrdinalIgnoreCase);

        // If the last message is from the user, we initiate the first tool call or reply text
        if (lastMsg.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
        {
            if (isCreateProduct)
            {
                string name = "";
                decimal sellingPrice = 0;
                decimal purchasePrice = 0;
                
                var numbers = Regex.Matches(userText, @"\b\d+\b").Cast<Match>().Select(m => decimal.Parse(m.Value)).ToList();
                if (numbers.Count >= 2)
                {
                    purchasePrice = numbers.Min();
                    sellingPrice = numbers.Max();
                }
                
                var nameMatch = Regex.Match(userText, @"(?:produk|barang|nama)\s+([a-zA-Z0-9\s]+?)(?:\s+harga|\s+modal|\s+beli|\s+jual|\b\d+\b|$)");
                if (nameMatch.Success)
                {
                    name = nameMatch.Groups[1].Value.Trim();
                }

                if (string.IsNullOrEmpty(name) || sellingPrice <= 0 || purchasePrice <= 0)
                {
                    return new GeminiResult
                    {
                        Text = "Untuk membuat **draf produk baru**, silakan lengkapi data berikut:\n" +
                               $"- **Nama Produk**: {(string.IsNullOrEmpty(name) ? "*(Belum diisi)*" : $"\"{name}\"")}\n" +
                               $"- **Harga Modal/Beli**: {(purchasePrice <= 0 ? "*(Belum diisi)*" : $"Rp {purchasePrice:N0}")}\n" +
                               $"- **Harga Jual**: {(sellingPrice <= 0 ? "*(Belum diisi)*" : $"Rp {sellingPrice:N0}")}\n\n" +
                               "*Contoh perintah: \"tambah produk Indomie Goreng harga 3000 modal 2200\"*"
                    };
                }

                return new GeminiResult
                {
                    HasFunctionCall = true,
                    FunctionName = "create_product_draft",
                    Arguments = JsonSerializer.Serialize(new
                    {
                        name = name,
                        sellingPrice = sellingPrice,
                        purchasePrice = purchasePrice
                    })
                };
            }
            else if (isCreateCustomer)
            {
                string name = "";
                string? phone = null;
                string? address = null;

                var phoneMatch = Regex.Match(userText, @"\b08\d{8,11}\b|\b\d{9,14}\b");
                if (phoneMatch.Success)
                {
                    phone = phoneMatch.Value;
                }

                var nameMatch = Regex.Match(userText, @"(?:pelanggan|nama)\s+([a-zA-Z\s]+?)(?:\s+telp|\s+hp|\s+alamat|\b08\d|\b\d|$)");
                if (nameMatch.Success)
                {
                    name = nameMatch.Groups[1].Value.Trim();
                }

                if (string.IsNullOrEmpty(name))
                {
                    return new GeminiResult
                    {
                        Text = "Untuk mendaftarkan **pelanggan baru**, silakan lengkapi data berikut:\n" +
                               "- **Nama Pelanggan**: *(Belum diisi)*\n" +
                               $"- **Nomor Telepon**: {(string.IsNullOrEmpty(phone) ? "*(Opsional, belum diisi)*" : phone)}\n" +
                               "- **Alamat**: *(Opsional, belum diisi)*\n\n" +
                               "*Contoh perintah: \"tambah pelanggan Budi Santoso telp 08123456789\"*"
                    };
                }

                return new GeminiResult
                {
                    HasFunctionCall = true,
                    FunctionName = "create_customer_draft",
                    Arguments = JsonSerializer.Serialize(new
                    {
                        name = name,
                        phone = phone,
                        address = address
                    })
                };
            }
            else if (isCreateSupplier)
            {
                string name = "";
                string? phone = null;
                string? address = null;

                var phoneMatch = Regex.Match(userText, @"\b08\d{8,11}\b|\b\d{9,14}\b");
                if (phoneMatch.Success)
                {
                    phone = phoneMatch.Value;
                }

                var nameMatch = Regex.Match(userText, @"(?:supplier|distributor|nama)\s+([a-zA-Z\s]+?)(?:\s+telp|\s+hp|\s+alamat|\b08\d|\b\d|$)");
                if (nameMatch.Success)
                {
                    name = nameMatch.Groups[1].Value.Trim();
                }

                if (string.IsNullOrEmpty(name))
                {
                    return new GeminiResult
                    {
                        Text = "Untuk mendaftarkan **supplier baru**, silakan lengkapi data berikut:\n" +
                               "- **Nama Supplier**: *(Belum diisi)*\n" +
                               $"- **Nomor Telepon**: {(string.IsNullOrEmpty(phone) ? "*(Opsional, belum diisi)*" : phone)}\n" +
                               "- **Alamat**: *(Opsional, belum diisi)*\n\n" +
                               "*Contoh perintah: \"tambah supplier PT Makmur Sejahtera telp 0811223344\"*"
                    };
                }

                return new GeminiResult
                {
                    HasFunctionCall = true,
                    FunctionName = "create_supplier_draft",
                    Arguments = JsonSerializer.Serialize(new
                    {
                        name = name,
                        phone = phone,
                        address = address
                    })
                };
            }
            else if (isSale)
            {
                string keyword = "";
                var words = userText.Split(new[] { ' ', ',', '.', '!' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var word in words)
                {
                    if (word.Equals("jual", StringComparison.OrdinalIgnoreCase) || 
                        word.Equals("cek", StringComparison.OrdinalIgnoreCase) || 
                        word.Equals("stok", StringComparison.OrdinalIgnoreCase) || 
                        word.Equals("beli", StringComparison.OrdinalIgnoreCase) ||
                        word.Equals("koreksi", StringComparison.OrdinalIgnoreCase) ||
                        word.Equals("penyesuaian", StringComparison.OrdinalIgnoreCase) ||
                        word.Equals("harga", StringComparison.OrdinalIgnoreCase) ||
                        word.Equals("kasir", StringComparison.OrdinalIgnoreCase) ||
                        word.Equals("transaksi", StringComparison.OrdinalIgnoreCase))
                        continue;
                    
                    if (word.Length > 2 && !int.TryParse(word, out _))
                    {
                        keyword = word.ToLower();
                        break;
                    }
                }

                if (string.IsNullOrEmpty(keyword))
                {
                    if (context != null && !string.IsNullOrEmpty(context.ActiveSaleDraftJson))
                    {
                        try
                        {
                            using var doc = JsonDocument.Parse(context.ActiveSaleDraftJson);
                            var items = doc.RootElement.GetProperty("items").EnumerateArray().ToList();
                            var totalAmount = doc.RootElement.GetProperty("totalAmount").GetDecimal();

                            var lines = new List<string>();
                            int no = 1;
                            foreach (var item in items)
                            {
                                var name = item.GetProperty("productName").GetString();
                                var qty = item.GetProperty("quantity").GetInt32();
                                var price = item.GetProperty("unitPrice").GetDecimal();
                                var sub = item.GetProperty("subtotal").GetDecimal();
                                lines.Add($"{no++}. **{name}** x{qty} @ Rp {price:N0} = Rp {sub:N0}");
                            }

                            return new GeminiResult
                            {
                                Text = $"### 🛒 Rincian Tagihan Draf Penjualan Aktif:\n" +
                                       string.Join("\n", lines) + "\n\n" +
                                       $"**Total Tagihan**: Rp {totalAmount:N0}\n\n" +
                                       $"*Silakan ketik \"Konfirmasi\" untuk menyimpan transaksi ini.*"
                            };
                        }
                        catch { }
                    }

                    return new GeminiResult
                    {
                        Text = "Untuk membuat **draf penjualan baru**, silakan lengkapi data berikut:\n" +
                               "- **Nama Produk/Barang**: *(Belum diisi)*\n" +
                               "- **Jumlah (Qty)**: *(Belum diisi, default: 1)*\n" +
                               "- **Harga Kustom**: *(Opsional, default: harga database)*\n\n" +
                               "*Contoh perintah: \"jual Aqua 3\" atau \"jual indomie 3 harga 3500\"*"
                    };
                }

                return new GeminiResult
                {
                    HasFunctionCall = true,
                    FunctionName = "search_product",
                    Arguments = JsonSerializer.Serialize(new { keyword = keyword })
                };
            }
            else if (isPurchase || isStockCheck || isStockAdjustment || isPriceCheck)
            {
                // Find a keyword for product search
                string keyword = "indomie";
                var words = userText.Split(new[] { ' ', ',', '.', '!' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var word in words)
                {
                    if (word.Equals("jual", StringComparison.OrdinalIgnoreCase) || 
                        word.Equals("cek", StringComparison.OrdinalIgnoreCase) || 
                        word.Equals("stok", StringComparison.OrdinalIgnoreCase) || 
                        word.Equals("beli", StringComparison.OrdinalIgnoreCase) ||
                        word.Equals("koreksi", StringComparison.OrdinalIgnoreCase) ||
                        word.Equals("penyesuaian", StringComparison.OrdinalIgnoreCase) ||
                        word.Equals("harga", StringComparison.OrdinalIgnoreCase) ||
                        word.Equals("berapa", StringComparison.OrdinalIgnoreCase) ||
                        word.Equals("price", StringComparison.OrdinalIgnoreCase))
                        continue;
                    
                    if (word.Length > 2 && !int.TryParse(word, out _))
                    {
                        keyword = word.ToLower();
                        break;
                    }
                }

                return new GeminiResult
                {
                    HasFunctionCall = true,
                    FunctionName = "search_product",
                    Arguments = JsonSerializer.Serialize(new { keyword = keyword })
                };
            }
            else if (isLowStock)
            {
                return new GeminiResult
                {
                    HasFunctionCall = true,
                    FunctionName = "get_low_stock",
                    Arguments = "{}"
                };
            }
            else if (isReceivable)
            {
                // Look for customer name
                string keyword = "budi";
                var words = userText.Split(new[] { ' ', ',', '.', '!' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var word in words)
                {
                    if (word.Equals("bayar", StringComparison.OrdinalIgnoreCase) || 
                        word.Equals("piutang", StringComparison.OrdinalIgnoreCase) || 
                        word.Equals("receivable", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (word.Length > 2 && !int.TryParse(word, out _))
                    {
                        keyword = word.ToLower();
                        break;
                    }
                }
                return new GeminiResult
                {
                    HasFunctionCall = true,
                    FunctionName = "search_customer",
                    Arguments = JsonSerializer.Serialize(new { keyword = keyword })
                };
            }
            else if (isPayable)
            {
                // Look for supplier name
                string keyword = "distributor";
                var words = userText.Split(new[] { ' ', ',', '.', '!' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var word in words)
                {
                    if (word.Equals("bayar", StringComparison.OrdinalIgnoreCase) || 
                        word.Equals("hutang", StringComparison.OrdinalIgnoreCase) || 
                        word.Equals("payable", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (word.Length > 2 && !int.TryParse(word, out _))
                    {
                        keyword = word.ToLower();
                        break;
                    }
                }
                return new GeminiResult
                {
                    HasFunctionCall = true,
                    FunctionName = "search_supplier",
                    Arguments = JsonSerializer.Serialize(new { keyword = keyword })
                };
            }
            else if (isTopSelling)
            {
                return new GeminiResult
                {
                    HasFunctionCall = true,
                    FunctionName = "get_top_selling_products",
                    Arguments = JsonSerializer.Serialize(new
                    {
                        fromDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).ToString("yyyy-MM-dd"),
                        toDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                        limit = 10
                    })
                };
            }
            else if (isDailySales)
            {
                return new GeminiResult
                {
                    HasFunctionCall = true,
                    FunctionName = "get_daily_sales_report",
                    Arguments = JsonSerializer.Serialize(new
                    {
                        fromDate = DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-dd"),
                        toDate = DateTime.UtcNow.ToString("yyyy-MM-dd")
                    })
                };
            }
            else if (isProfit)
            {
                return new GeminiResult
                {
                    HasFunctionCall = true,
                    FunctionName = "get_profit_report",
                    Arguments = JsonSerializer.Serialize(new
                    {
                        fromDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).ToString("yyyy-MM-dd"),
                        toDate = DateTime.UtcNow.ToString("yyyy-MM-dd")
                    })
                };
            }
            else if (isExpense)
            {
                return new GeminiResult
                {
                    HasFunctionCall = true,
                    FunctionName = "get_expense_summary",
                    Arguments = JsonSerializer.Serialize(new
                    {
                        fromDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).ToString("yyyy-MM-dd"),
                        toDate = DateTime.UtcNow.ToString("yyyy-MM-dd")
                    })
                };
            }
            else if (isStockValuation)
            {
                return new GeminiResult
                {
                    HasFunctionCall = true,
                    FunctionName = "get_stock_valuation",
                    Arguments = "{}"
                };
            }
            else if (isDashboard)
            {
                return new GeminiResult
                {
                    HasFunctionCall = true,
                    FunctionName = "get_dashboard_summary",
                    Arguments = JsonSerializer.Serialize(new
                    {
                        date = DateTime.UtcNow.ToString("yyyy-MM-dd")
                    })
                };
            }
            else
            {
                // Welcome / General response
                return new GeminiResult
                {
                    Text = "Halo! Saya SobatEntong AI, asisten POS pintar Anda. Saya bisa membantu Anda membuat draft transaksi penjualan cepat (misalnya: 'jual indomie 3'), mengecek stok produk ('cek stok aqua'), melihat produk stok menipis ('stok rendah'), atau mencatat pembayaran hutang/piutang. Ada yang bisa saya bantu hari ini?"
                };
            }
        }

        // If the last message is from a tool, we process the tool's result and make the next step or final text
        if (lastMsg.Role.Equals("tool", StringComparison.OrdinalIgnoreCase))
        {
            string toolName = lastMsg.FunctionName ?? "";
            string toolResultJson = lastMsg.Message ?? "[]";

            if (toolName.Equals("search_product", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var products = ParseProducts(toolResultJson);
                    var product = products.FirstOrDefault();

                    if (product != null)
                    {
                        string productId = product.ProductId;
                        string productName = product.Name;
                        decimal unitPrice = product.Price;
                        decimal currentStock = product.Stock;

                        if (isSale)
                        {
                            // Extract quantity
                            int qty = 1;
                            var qtyMatch = Regex.Match(userText, @"\b\d+\b");
                            if (qtyMatch.Success)
                            {
                                qty = int.Parse(qtyMatch.Value);
                            }

                            // Extract custom price if present
                            decimal? customPrice = null;
                            var priceMatch = Regex.Match(userText, @"(?:harga|@)\s*(\d+)");
                            if (priceMatch.Success)
                            {
                                customPrice = decimal.Parse(priceMatch.Groups[1].Value);
                            }

                            var itemsList = new[]
                            {
                                new
                                {
                                    productId = productId,
                                    productName = productName,
                                    quantity = qty,
                                    unitPrice = customPrice ?? unitPrice
                                }
                            };

                            return new GeminiResult
                            {
                                HasFunctionCall = true,
                                FunctionName = "create_sale_draft",
                                Arguments = JsonSerializer.Serialize(new { items = itemsList })
                            };
                        }
                        else if (isPriceCheck)
                        {
                            return new GeminiResult
                            {
                                Text = $"Harga jual untuk **{productName}** adalah **Rp {unitPrice:N0}** (Stok saat ini: {currentStock})."
                            };
                        }
                        else if (isStockCheck)
                        {
                            return new GeminiResult
                            {
                                Text = $"Produk **{productName}** saat ini memiliki stok sebanyak **{currentStock}** dengan harga jual Rp {unitPrice:N0}."
                            };
                        }
                        else if (isStockAdjustment)
                        {
                            // Extract new stock quantity from userText (e.g. "koreksi stok menjadi 50")
                            int newStock = 10;
                            var stockMatch = Regex.Match(userText, @"(?:menjadi|ke|adjust)\s*(\d+)");
                            if (!stockMatch.Success)
                            {
                                stockMatch = Regex.Match(userText, @"\b\d+\b");
                            }
                            if (stockMatch.Success)
                            {
                                newStock = int.Parse(stockMatch.Value);
                            }

                            return new GeminiResult
                            {
                                HasFunctionCall = true,
                                FunctionName = "create_stock_adjustment_draft",
                                Arguments = JsonSerializer.Serialize(new
                                {
                                    productId = productId,
                                    newStock = newStock,
                                    reason = "Koreksi stok lewat SobatEntong AI"
                                })
                            };
                        }
                    }
                    else
                    {
                        return new GeminiResult
                        {
                            Text = "Maaf, saya tidak menemukan produk tersebut di database."
                        };
                    }
                }
                catch
                {
                    return new GeminiResult
                    {
                        Text = "Maaf, terjadi kesalahan saat memproses data produk."
                    };
                }
            }
            else if (toolName.Equals("create_sale_draft", StringComparison.OrdinalIgnoreCase))
            {
                return new GeminiResult
                {
                    Text = "Saya sudah membuat draf transaksi penjualan POS untuk produk tersebut. Silakan periksa detailnya dan tekan tombol **Konfirmasi** untuk memproses transaksi."
                };
            }
            else if (toolName.Equals("create_stock_adjustment_draft", StringComparison.OrdinalIgnoreCase))
            {
                return new GeminiResult
                {
                    Text = "Draf koreksi stok berhasil disiapkan. Silakan klik **Konfirmasi Penyesuaian** untuk memperbarui stok di sistem."
                };
            }
            else if (toolName.Equals("get_low_stock", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var products = ParseProducts(toolResultJson);
                    if (products.Count > 0)
                    {
                        var list = new List<string>();
                        foreach (var item in products)
                        {
                            list.Add($"- **{item.Name}** (Sisa Stok: {item.Stock})");
                        }
                        return new GeminiResult
                        {
                            Text = $"Berikut adalah produk dengan stok menipis:\n{string.Join("\n", list)}\n\nSebaiknya segera lakukan pembelian stok ke supplier."
                        };
                    }
                }
                catch {}
                
                return new GeminiResult
                {
                    Text = "Hebat! Semua produk Anda saat ini memiliki stok yang cukup."
                };
            }
            else if (toolName.Equals("search_customer", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var customers = ParseCustomers(toolResultJson);
                    var customer = customers.FirstOrDefault();
                    if (customer != null)
                    {
                        string customerId = customer.CustomerId;
                        
                        return new GeminiResult
                        {
                            HasFunctionCall = true,
                            FunctionName = "get_receivables",
                            Arguments = JsonSerializer.Serialize(new { customerId = customerId })
                        };
                    }
                }
                catch {}

                return new GeminiResult
                {
                    Text = "Maaf, pelanggan tidak ditemukan."
                };
            }
            else if (toolName.Equals("get_receivables", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var doc = JsonDocument.Parse(toolResultJson);
                    decimal totalDebt = doc.RootElement.GetProperty("totalDebt").GetDecimal();
                    string customerId = doc.RootElement.GetProperty("customerId").GetString() ?? "";

                    if (totalDebt <= 0)
                    {
                        return new GeminiResult
                        {
                            Text = "Pelanggan tersebut tidak memiliki sisa piutang."
                        };
                    }

                    // Extract payment amount
                    decimal payAmount = totalDebt;
                    var payMatch = Regex.Match(userText, @"\b\d{4,9}\b");
                    if (payMatch.Success)
                    {
                        payAmount = decimal.Parse(payMatch.Value);
                    }

                    return new GeminiResult
                    {
                        HasFunctionCall = true,
                        FunctionName = "create_receivable_payment_draft",
                        Arguments = JsonSerializer.Serialize(new
                        {
                            customerId = customerId,
                            amount = payAmount
                        })
                    };
                }
                catch {}

                return new GeminiResult
                {
                    Text = "Maaf, terjadi kesalahan saat mengecek data piutang."
                };
            }
            else if (toolName.Equals("create_receivable_payment_draft", StringComparison.OrdinalIgnoreCase))
            {
                return new GeminiResult
                {
                    Text = "Saya sudah membuat draf pembayaran piutang. Silakan tekan tombol **Konfirmasi Pembayaran** untuk mencatatnya ke kas."
                };
            }
            else if (toolName.Equals("search_supplier", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var suppliers = ParseSuppliers(toolResultJson);
                    var supplier = suppliers.FirstOrDefault();
                    if (supplier != null)
                    {
                        string supplierId = supplier.SupplierId;

                        // Default to pay
                        decimal payAmount = 100000;
                        var payMatch = Regex.Match(userText, @"\b\d{4,9}\b");
                        if (payMatch.Success)
                        {
                            payAmount = decimal.Parse(payMatch.Value);
                        }

                        return new GeminiResult
                        {
                            HasFunctionCall = true,
                            FunctionName = "create_payable_payment_draft",
                            Arguments = JsonSerializer.Serialize(new
                            {
                                supplierId = supplierId,
                                amount = payAmount
                            })
                        };
                    }
                }
                catch {}

                return new GeminiResult
                {
                    Text = "Maaf, supplier tidak ditemukan."
                };
            }
            else if (toolName.Equals("create_payable_payment_draft", StringComparison.OrdinalIgnoreCase))
            {
                return new GeminiResult
                {
                    Text = "Draf pembayaran hutang ke supplier berhasil dibuat. Silakan tekan **Konfirmasi Pembayaran** untuk memproses pengeluaran kas."
                };
            }
            else if (toolName.Equals("get_dashboard_summary", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var doc = JsonDocument.Parse(toolResultJson);
                    var date = doc.RootElement.GetProperty("date").GetString();
                    var totalSalesAmount = doc.RootElement.GetProperty("totalSalesAmount").GetDecimal();
                    var totalSalesTransactions = doc.RootElement.GetProperty("totalSalesTransactions").GetInt32();
                    var totalPurchaseAmount = doc.RootElement.GetProperty("totalPurchaseAmount").GetDecimal();
                    var totalExpenseAmount = doc.RootElement.GetProperty("totalExpenseAmount").GetDecimal();
                    var grossProfitAmount = doc.RootElement.GetProperty("grossProfitAmount").GetDecimal();

                    return new GeminiResult
                    {
                        Text = $"### 📊 Ringkasan Laporan Toko (Hari Ini - {date})\n" +
                               $"- **Total Penjualan**: Rp {totalSalesAmount:N0} ({totalSalesTransactions} transaksi)\n" +
                               $"- **Total Pembelian (Kulakan)**: Rp {totalPurchaseAmount:N0}\n" +
                               $"- **Total Pengeluaran**: Rp {totalExpenseAmount:N0}\n" +
                               $"- **Laba Kotor**: Rp {grossProfitAmount:N0}\n\n" +
                               $"*Data disajikan secara realtime dari sistem database.*"
                    };
                }
                catch
                {
                    return new GeminiResult { Text = "Maaf, gagal memproses data ringkasan dashboard." };
                }
            }
            else if (toolName.Equals("get_daily_sales_report", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var doc = JsonDocument.Parse(toolResultJson);
                    var fromDate = doc.RootElement.GetProperty("fromDate").GetString();
                    var toDate = doc.RootElement.GetProperty("toDate").GetString();
                    var dailySales = doc.RootElement.GetProperty("dailySales").EnumerateArray().ToList();

                    var lines = new List<string>();
                    decimal grandTotal = 0;
                    int totalTx = 0;
                    foreach (var sale in dailySales)
                    {
                        var date = sale.GetProperty("date").GetString();
                        var count = sale.GetProperty("transactionCount").GetInt32();
                        var amount = sale.GetProperty("totalAmount").GetDecimal();
                        lines.Add($"- **{date}**: Rp {amount:N0} ({count} transaksi)");
                        grandTotal += amount;
                        totalTx += count;
                    }

                    return new GeminiResult
                    {
                        Text = $"### 📈 Laporan Tren Penjualan Harian ({fromDate} s/d {toDate})\n" +
                               string.Join("\n", lines) + $"\n\n" +
                               $"- **Total Penjualan**: Rp {grandTotal:N0}\n" +
                               $"- **Total Transaksi**: {totalTx} transaksi"
                    };
                }
                catch
                {
                    return new GeminiResult { Text = "Maaf, gagal memproses laporan tren penjualan harian." };
                }
            }
            else if (toolName.Equals("get_top_selling_products", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var doc = JsonDocument.Parse(toolResultJson);
                    var topProducts = doc.RootElement.GetProperty("topProducts").EnumerateArray().ToList();

                    var lines = new List<string>();
                    int rank = 1;
                    foreach (var prod in topProducts)
                    {
                        var name = prod.GetProperty("productName").GetString();
                        var qty = prod.GetProperty("quantitySold").GetInt32();
                        var salesAmount = prod.GetProperty("grossSalesAmount").GetDecimal();
                        lines.Add($"{rank++}. **{name}** - Terjual: {qty} pcs | Omset: Rp {salesAmount:N0}");
                    }

                    return new GeminiResult
                    {
                        Text = $"### 🏆 Produk Paling Laris Bulan Ini\n" +
                               (lines.Count > 0 ? string.Join("\n", lines) : "Belum ada data penjualan produk harian.")
                    };
                }
                catch
                {
                    return new GeminiResult { Text = "Maaf, gagal memproses data produk terlaris." };
                }
            }
            else if (toolName.Equals("get_profit_report", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var doc = JsonDocument.Parse(toolResultJson);
                    var fromDate = doc.RootElement.GetProperty("fromDate").GetString();
                    var toDate = doc.RootElement.GetProperty("toDate").GetString();
                    var netSales = doc.RootElement.GetProperty("netSalesAmount").GetDecimal();
                    var cogs = doc.RootElement.GetProperty("estimatedCostOfGoodsSold").GetDecimal();
                    var grossProfit = doc.RootElement.GetProperty("estimatedGrossProfit").GetDecimal();
                    var expense = doc.RootElement.GetProperty("expenseAmount").GetDecimal();
                    var netProfit = doc.RootElement.GetProperty("estimatedNetProfit").GetDecimal();
                    var margin = doc.RootElement.GetProperty("grossProfitMargin").GetDecimal();

                    return new GeminiResult
                    {
                        Text = $"### 💵 Laporan Keuntungan & Margin ({fromDate} s/d {toDate})\n" +
                               $"- **Pendapatan Bersih (Net Sales)**: Rp {netSales:N0}\n" +
                               $"- **Harga Pokok Penjualan (HPP/COGS)**: Rp {cogs:N0}\n" +
                               $"- **Laba Kotor (Gross Profit)**: Rp {grossProfit:N0} (Margin: {margin}%)\n" +
                               $"- **Biaya Pengeluaran (Expense)**: Rp {expense:N0}\n" +
                               $"- **Estimasi Laba Bersih (Net Profit)**: Rp {netProfit:N0}"
                    };
                }
                catch
                {
                    return new GeminiResult { Text = "Maaf, gagal memproses laporan keuntungan." };
                }
            }
            else if (toolName.Equals("get_expense_summary", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var doc = JsonDocument.Parse(toolResultJson);
                    var totalExpense = doc.RootElement.GetProperty("totalExpense").GetDecimal();
                    var categories = doc.RootElement.GetProperty("categories").EnumerateArray().ToList();

                    var lines = new List<string>();
                    foreach (var cat in categories)
                    {
                        var name = cat.GetProperty("categoryName").GetString();
                        var amount = cat.GetProperty("totalAmount").GetDecimal();
                        lines.Add($"- **{name}**: Rp {amount:N0}");
                    }

                    return new GeminiResult
                    {
                        Text = $"### 💸 Ringkasan Biaya Pengeluaran Bulan Ini\n" +
                               $"- **Total Seluruh Pengeluaran**: Rp {totalExpense:N0}\n\n" +
                               $"**Rincian per Kategori:**\n" +
                               (lines.Count > 0 ? string.Join("\n", lines) : "Tidak ada data pengeluaran.")
                    };
                }
                catch
                {
                    return new GeminiResult { Text = "Maaf, gagal memproses ringkasan pengeluaran." };
                }
            }
            else if (toolName.Equals("get_stock_valuation", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var doc = JsonDocument.Parse(toolResultJson);
                    var totalProducts = doc.RootElement.GetProperty("totalProductCount").GetInt32();
                    var totalQty = doc.RootElement.GetProperty("totalStockQuantity").GetInt32();
                    var costVal = doc.RootElement.GetProperty("totalCostValue").GetDecimal();
                    var sellVal = doc.RootElement.GetProperty("totalSellingValue").GetDecimal();
                    var potentialProfit = doc.RootElement.GetProperty("potentialProfit").GetDecimal();

                    return new GeminiResult
                    {
                        Text = $"### 📦 Valuasi Aset Stok Toko Kelontong\n" +
                               $"- **Total Produk**: {totalProducts} jenis barang\n" +
                               $"- **Total Kuantitas Fisik**: {totalQty} unit/pcs\n" +
                               $"- **Nilai Aset Modal (HPP/Cost)**: Rp {costVal:N0}\n" +
                               $"- **Nilai Jual Aset (Selling)**: Rp {sellVal:N0}\n" +
                               $"- **Potensi Keuntungan**: Rp {potentialProfit:N0}"
                    };
                }
                catch
                {
                    return new GeminiResult { Text = "Maaf, gagal memproses laporan valuasi stok." };
                }
            }
        }

        return new GeminiResult
        {
            Text = "Saya memahami pesan Anda, namun tidak ada aksi otomatis yang sesuai. Silakan ketik perintah lain."
        };
    }
}
