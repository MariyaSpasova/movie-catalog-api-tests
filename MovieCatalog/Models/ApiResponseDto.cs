using System.Text.Json.Serialization;

namespace MovieCatalog.Models
{
    internal class ApiResponseDto
    {
        [JsonPropertyName("msg")]
        public string Msg { get; set; }

        [JsonPropertyName("movie")]
        public MovieDto Movie { get; set; } = new MovieDto();
    }
}
