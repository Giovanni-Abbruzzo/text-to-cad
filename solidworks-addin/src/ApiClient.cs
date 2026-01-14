using System;
using System.Configuration;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TextToCad.SolidWorksAddin.Models;

namespace TextToCad.SolidWorksAddin
{
    /// <summary>
    /// HTTP client for communicating with the FastAPI backend.
    /// Handles all API calls to /dry_run and /process_instruction endpoints.
    /// </summary>
    public static class ApiClient
    {
        private static readonly HttpClient httpClient;
        private static string baseUrl;
        private static int timeoutSeconds;

        static ApiClient()
        {
            // Initialize HTTP client with timeout
            timeoutSeconds = int.Parse(ConfigurationManager.AppSettings["ApiTimeoutSeconds"] ?? "30");
            httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(timeoutSeconds)
            };

            // Set base URL from config (can be overridden via UI)
            baseUrl = NormalizeBaseUrl(ConfigurationManager.AppSettings["ApiBaseUrl"] ?? "http://localhost:8000");

            Logger.Info($"ApiClient initialized with base URL: {baseUrl}, timeout: {timeoutSeconds}s");
        }

        /// <summary>
        /// Set or update the base URL for API calls.
        /// This overrides the App.config value for the current session.
        /// </summary>
        public static void SetBaseUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("Base URL cannot be empty", nameof(url));

            baseUrl = NormalizeBaseUrl(url);
            Logger.Info($"API base URL updated to: {baseUrl}");
        }

        /// <summary>
        /// Get the current base URL
        /// </summary>
        public static string GetBaseUrl()
        {
            return baseUrl;
        }

        /// <summary>
        /// Call the /dry_run endpoint for plan preview without execution
        /// </summary>
        public static async Task<InstructionResponse> DryRunAsync(InstructionRequest request)
        {
            Logger.Info($"Calling /dry_run with instruction: '{request.Instruction}' (use_ai={request.UseAI})");

            try
            {
                var response = await PostJsonAsync<InstructionResponse>("/dry_run", request);
                Logger.Info($"Dry run successful: {response.GetSummary()}");
                return response;
            }
            catch (Exception ex)
            {
                Logger.Error($"Dry run failed for instruction: '{request.Instruction}'", ex);
                throw;
            }
        }

        /// <summary>
        /// Call the /process_instruction endpoint to execute and save to database
        /// </summary>
        public static async Task<InstructionResponse> ProcessInstructionAsync(InstructionRequest request)
        {
            Logger.Info($"Calling /process_instruction with instruction: '{request.Instruction}' (use_ai={request.UseAI})");

            try
            {
                var response = await PostJsonAsync<InstructionResponse>("/process_instruction", request);
                Logger.Info($"Process instruction successful: {response.GetSummary()}");
                return response;
            }
            catch (Exception ex)
            {
                Logger.Error($"Process instruction failed for instruction: '{request.Instruction}'", ex);
                throw;
            }
        }

        /// <summary>
        /// Test connection to the backend API
        /// </summary>
        public static async Task<bool> TestConnectionAsync()
        {
            try
            {
                Logger.Info("Testing API connection...");
                var response = await httpClient.GetAsync($"{baseUrl}/health");

                if (response.IsSuccessStatusCode)
                {
                    Logger.Info("API connection successful");
                    return true;
                }
                else
                {
                    Logger.Warning($"API health check returned status: {response.StatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("API connection test failed", ex);
                return false;
            }
        }

        /// <summary>
        /// Generic POST method with JSON serialization and deserialization
        /// </summary>
        private static async Task<TResponse> PostJsonAsync<TResponse>(string route, object payload)
        {
            string url = $"{baseUrl}{route}";

            // Serialize request
            string jsonPayload = JsonConvert.SerializeObject(payload);
            Logger.Debug($"POST {url}\nPayload: {jsonPayload}");

            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // Send request
            HttpResponseMessage response = await httpClient.PostAsync(url, content);

            // Read response
            string responseBody = await response.Content.ReadAsStringAsync();
            Logger.Debug($"Response status: {response.StatusCode}\nBody: {responseBody}");

            // Check for errors
            if (!response.IsSuccessStatusCode)
            {
                string errorMessage = $"API request failed with status {response.StatusCode}";

                // Try to parse error details
                try
                {
                    var errorObj = JsonConvert.DeserializeObject<dynamic>(responseBody);
                    if (errorObj?.detail != null)
                    {
                        errorMessage += $": {errorObj.detail}";
                    }
                }
                catch
                {
                    errorMessage += $": {responseBody}";
                }

                throw new HttpRequestException(errorMessage);
            }

            // Deserialize response
            try
            {
                var result = JsonConvert.DeserializeObject<TResponse>(responseBody);

                // Debug: Log operations array for InstructionResponse
                if (result is InstructionResponse instructionResponse)
                {
                    if (instructionResponse.Operations != null)
                    {
                        Logger.Info($"Deserialized InstructionResponse with {instructionResponse.Operations.Count} operations");
                        for (int i = 0; i < instructionResponse.Operations.Count; i++)
                        {
                            var op = instructionResponse.Operations[i];
                            Logger.Info($"  Op {i + 1}: Action={op?.Action}, Shape={op?.ParametersData?.Shape}");
                        }
                    }
                    else
                    {
                        Logger.Warning("InstructionResponse.Operations is null after deserialization");
                    }
                }

                return result;
            }
            catch (JsonException ex)
            {
                Logger.Error($"Failed to deserialize response from {url}", ex);
                throw new JsonException($"Invalid JSON response from API: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get raw JSON response as string (for debugging/logging)
        /// </summary>
        public static async Task<string> PostJsonRawAsync(string route, object payload)
        {
            string url = $"{baseUrl}{route}";
            string jsonPayload = JsonConvert.SerializeObject(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await httpClient.PostAsync(url, content);
            string responseBody = await response.Content.ReadAsStringAsync();

            response.EnsureSuccessStatusCode();
            return responseBody;
        }

        private static string NormalizeBaseUrl(string url)
        {
            string normalized = url.Trim();
            normalized = normalized.TrimEnd('/');
            if (normalized.EndsWith("/docs", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(0, normalized.Length - 5);
            }

            return normalized;
        }
    }
}
