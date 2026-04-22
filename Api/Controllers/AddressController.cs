using System.Security.Claims;
using DeliveryAPI.Api.Contracts.Request;
using DeliveryAPI.Api.Contracts.Response;
using DeliveryAPI.Api.Middleware;
using DeliveryAPI.Application.Exeptions;
using DeliveryAPI.Application.Models.Input;
using DeliveryAPI.Application.Models.Result;
using DeliveryAPI.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliveryAPI.Api.Controllers
{
    [ApiController]
    [Route("/address")]
    public class AddressController : ControllerBase
    {
        private readonly AddressService _addressService;

        public AddressController(AddressService addressService)
        {
            _addressService = addressService;
        }

        [Authorize]
        [HttpPost]
        [ProducesResponseType(typeof(object), 201)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        public async Task<ActionResult<object>> CreateAddress([FromBody] AddressCreateRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new UnauthorizedException("UserId claim missing");

            int userId = int.Parse(userIdClaim.Value);

            int result = await _addressService.CreateAddressAsync(new AddressCreateServiceInput
            {
                userId = userId,
                title = request.title,
                latitude = request.latitude,
                longitude = request.longitude,
                house = request.house,
                apartment = request.apartment,
                entrance = request.entrance,
                floor = request.floor,
                comment = request.comment,
                is_default = request.is_default,
            });

            return CreatedAtAction(nameof(CreateAddress), new { id = result }, new { addressId = result });
        }

        [Authorize]
        [HttpGet]
        [ProducesResponseType(typeof(List<AddressUserIdResponse>), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        public async Task<ActionResult<List<AddressUserIdResponse>>> GetAddressByUserId()
        {
            List<AddressUserIdResponse> addressUserIdResponses = new List<AddressUserIdResponse>();

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new UnauthorizedException("UserId claim missing");

            int userId = int.Parse(userIdClaim.Value);

            addressUserIdResponses = await _addressService.GetAddressByUserIdAsync(userId);

            return Ok(addressUserIdResponses);
        }

        [Authorize]
        [HttpPut("{id}/default")]
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        public async Task<ActionResult> SetDefaultAddress(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new UnauthorizedException("UserId claim missing");

            int userId = int.Parse(userIdClaim.Value);

            await _addressService.SetDefaultAddressAsync(userId, id);
            return Ok();
        }

        [Authorize]
        [HttpDelete("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        public async Task<ActionResult> DeleteAddress(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new UnauthorizedException("UserId claim missing");
            int userId = int.Parse(userIdClaim.Value);

            await _addressService.DeleteAddressAsync(userId, id);
            return Ok();
        }
    }
}
