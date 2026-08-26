using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using taskAPI.Configuration;
using taskAPI.Contracts;
using taskAPI.Services;

namespace taskAPI.Controllers;

[ApiController]
[Route("api/trailers")]
public sealed class TrailersController : ControllerBase
{
    private readonly ITrailerService trailerService;
    private readonly MovieGenerationOptions movieOptions;

    public TrailersController(
        ITrailerService trailerService,
        IOptions<MovieGenerationOptions> movieOptions)
    {
        this.trailerService = trailerService;
        this.movieOptions = movieOptions.Value;
    }

    [HttpGet("{movieIndex:long}")]
    [ProducesResponseType<TrailerDescriptor>(
        StatusCodes.Status200OK
    )]
    public ActionResult<TrailerDescriptor> Get(
        long movieIndex,
        [FromQuery] ulong seed,
        [FromQuery] string? locale)
    {
        if (movieIndex < 1)
        {
            return BadRequest();
        }

        var effectiveLocale = string.IsNullOrWhiteSpace(locale)
            ? movieOptions.DefaultLocale
            : locale;

        if (!movieOptions.Locales.ContainsKey(effectiveLocale))
        {
            return BadRequest();
        }

        return Ok(
            trailerService.Generate(
                seed,
                movieIndex,
                effectiveLocale
            )
        );
    }
}