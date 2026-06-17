using Gatx.Application.Auth;
using Gatx.Application.Auth.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gatx.WebApi.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    [HttpPost("login")]
    [ProducesResponseType<AuthResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<AuthResultDto> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        return await sender.Send(new LoginCommand(request.Username, request.Password), cancellationToken);
    }
}

public sealed record LoginRequest(string Username, string Password);
