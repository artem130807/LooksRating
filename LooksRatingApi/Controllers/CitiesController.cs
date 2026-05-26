using LooksRatingApi.CQRS.Cities.Query.GetCities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LooksRatingApi.Controllers
{
    [ApiController]
    [Route("api/cities")]
    public class CitiesController : ControllerBase
    {
        private readonly ISender _sender;

        public CitiesController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetCitiesQuery(), cancellationToken);
            if (result.IsFailure)
                return BadRequest(new { error = result.Error });

            return Ok(result.Value);
        }
    }
}
