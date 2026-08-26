using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using taskAPI.Configuration;
using taskAPI.Contracts;

namespace taskAPI.Controllers;

[ApiController]
[Route("api/configuration")]
public sealed class ConfigurationController : ControllerBase
{
    private readonly MovieGenerationOptions options;

    public ConfigurationController(
        IOptions<MovieGenerationOptions> options)
    {
        this.options = options.Value;
    }

    [HttpGet]
    public ActionResult<ApplicationConfigurationResponse> Get()
    {
        return Ok(
            new ApplicationConfigurationResponse(
                options.DefaultLocale,
                options.Locales.Keys.ToArray(),
                options.DefaultPageSize,
                options.MaximumPageSize,
                options.MinimumAverage,
                options.MaximumAverage,
                options.DefaultLikesAverage,
                options.DefaultReviewsAverage,
                ulong.MaxValue.ToString(
                    CultureInfo.InvariantCulture
                )
            )
        );
    }
}