using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MentorFlow.API.DTOs;
using MentorFlow.API.Interfaces;

namespace MentorFlow.API.Controllers;
[ApiController, Route("api/mentors"), Authorize]
public sealed class MentorsController(IMentorService service) : ControllerBase
{
    [HttpGet, Authorize(Roles = "Admin,Mentor,Student")]
    public Task<PagedResponse<MentorResponse>> GetAll([FromQuery] ProfileListRequest request, CancellationToken ct) => service.GetAllAsync(request, ct);
    [HttpGet("{id:int}"), Authorize(Roles = "Admin,Mentor,Student")]
    public Task<MentorResponse> Get(int id, CancellationToken ct) => service.GetByIdAsync(id, ct);
    [HttpPost, Authorize(Roles = "Admin")]
    public async Task<ActionResult<MentorResponse>> Create(CreateMentorRequest request, CancellationToken ct)
    {
        var result = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = result.MentorId }, result);
    }

    [HttpPut("{id:int}"), Authorize(Roles = "Admin")]
    public Task<MentorResponse> Update(int id, UpdateMentorRequest request, CancellationToken ct) => service.UpdateAsync(id, request, ct);
    [HttpPatch("{id:int}/status"), Authorize(Roles = "Admin")]
    public Task<MentorResponse> SetStatus(int id, SetProfileStatusRequest request, CancellationToken ct) => service.SetActiveAsync(id, request.IsActive!.Value, ct);

    [HttpDelete("{id:int}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}
