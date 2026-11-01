using System.ComponentModel.DataAnnotations;

namespace LearnQ.Infrastructure.Configuration;

public sealed class ApiOptions
{
    public const string SectionName = "Api";

    [Required]
    [Url]
    public string BaseUrl { get; set; } = string.Empty;
}
