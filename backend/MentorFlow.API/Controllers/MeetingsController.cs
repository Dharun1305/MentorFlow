using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MentorFlow.API.DTOs;
using MentorFlow.API.Interfaces;

namespace MentorFlow.API.Controllers;
[ApiController, Route("api/meetings"), Authorize(Roles = "Admin,Mentor,Student")]
public sealed class MeetingsController(IMeetingService service) : ControllerBase
{
    [HttpGet]
    public Task<PagedResponse<MeetingResponse>> GetAll([FromQuery] MeetingListRequest request, CancellationToken ct) => service.GetAllAsync(request, ct);
    [HttpGet("{id:int}")]
    public Task<MeetingResponse> Get(int id, CancellationToken ct) => service.GetByIdAsync(id, ct);
    [HttpPost, Authorize(Roles = "Admin,Mentor")]
    public async Task<ActionResult<MeetingResponse>> Create(CreateMeetingRequest request, CancellationToken ct)
    {
        var result = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = result.MeetingId }, result);
    }

    [HttpPut("{id:int}"), Authorize(Roles = "Admin,Mentor")]
    public Task<MeetingResponse> Update(int id, UpdateMeetingRequest request, CancellationToken ct) => service.UpdateAsync(id, request, ct);
}
