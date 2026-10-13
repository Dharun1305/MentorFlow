using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MentorFlow.API.DTOs;
using MentorFlow.API.Interfaces;

namespace MentorFlow.API.Controllers;
[ApiController, Route("api/assignments"), Authorize(Roles = "Admin,Mentor,Student")]
public sealed class AssignmentsController(IAssignmentService service) : ControllerBase
{
    [HttpGet]
    public Task<PagedResponse<AssignmentSummary>> GetAll([FromQuery] AssignmentListRequest request, CancellationToken ct) => service.GetAllAsync(request, ct);
    [HttpPost, Authorize(Roles = "Admin")]
    public async Task<ActionResult<AssignmentSummary>> Create(CreateAssignmentRequest request, CancellationToken ct)
    {
        var result = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetAll), result);
    }

    [HttpPut("{id:int}"), Authorize(Roles = "Admin")]
    public Task<AssignmentSummary> Update(int id, UpdateAssignmentRequest request, CancellationToken ct) => service.UpdateAsync(id, request, ct);
}
