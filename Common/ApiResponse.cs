namespace DashboardAPI.Common
{
    /// <summary>
    /// Enveloppe standardisée pour toutes les réponses API.
    /// Garantit un format JSON cohérent : { success, data, error, meta }
    /// </summary>
    public class ApiResponse<T>
    {
        public bool    Success   { get; init; }
        public T?      Data      { get; init; }
        public string? Error     { get; init; }
        public object? Meta      { get; init; }
        public string  Timestamp { get; init; } = DateTime.UtcNow.ToString("O");

        public static ApiResponse<T> Ok(T data, object? meta = null) => new()
        {
            Success = true,
            Data    = data,
            Meta    = meta
        };

        public static ApiResponse<T> Fail(string error) => new()
        {
            Success = false,
            Error   = error
        };
    }

    /// <summary>Réponse paginée standardisée.</summary>
    public class PagedResponse<T>
    {
        public List<T> Items      { get; init; } = [];
        public int     TotalCount { get; init; }
        public int     Page       { get; init; }
        public int     PageSize   { get; init; }
        public int     TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool    HasNext    => Page < TotalPages;
        public bool    HasPrev    => Page > 1;
    }
}
