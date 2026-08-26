using Microsoft.AspNetCore.Mvc;
using taskAPI.Contracts;
using taskAPI.Services;

namespace taskAPI.Controllers;

[ApiController]
[Route("api/movies")]
public sealed class MoviesController : ControllerBase
{
    private readonly IMovieService movieService;

    public MoviesController(IMovieService movieService)
    {
        this.movieService = movieService;
    }

    [HttpGet]
    [ProducesResponseType<MoviePageResponse>(StatusCodes.Status200OK)]
    public ActionResult<MoviePageResponse> Get(
        [FromQuery] ulong seed,
        [FromQuery] string? locale,
        [FromQuery] int page = 1,
        [FromQuery] int? pageSize = null,
        [FromQuery] double likes = 0,
        [FromQuery] double reviews = 0)
    {
        var request = new MoviePageRequest(
            seed,
            locale,
            page,
            pageSize,
            likes,
            reviews
        );

        return Ok(movieService.GeneratePage(request));
    }

    [HttpGet("seed")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    public ActionResult<string> GenerateSeed()
    {
        return Ok(
            movieService
                .GenerateSeed()
                .ToString(
                    System.Globalization.CultureInfo.InvariantCulture
                )
        );
    }
}