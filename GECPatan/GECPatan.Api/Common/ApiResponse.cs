namespace GECPatan.Api.Common
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public int? Total { get; set; } // for paged responses

        // ── Static helpers ────────────────────────────
        public static ApiResponse<T> Ok(T data,
            string message = "OK", int? total = null)
            => new()
            {
                Success = true,
                Message = message,
                Data = data,
                Total = total
            };

        public static ApiResponse<T> Fail(
            string message = "Not found")
            => new()
            {
                Success = false,
                Message = message,
                Data = default
            };
    }
}