using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MentorFlow.API.DTOs;
using MentorFlow.API.Interfaces;

namespace MentorFlow.API.Controllers;

[ApiController, Route("api/goals"), Authorize(Roles = "Admin,Mentor,Student")]
public sealed class GoalsController(IGoalService service) : ControllerBase
{
    [HttpGet] public Task<IReadOnlyList<GoalResponse>> GetAll(CancellationToken ct) => service.GetAllAsync(ct);
    [HttpGet("{id:int}")] public Task<GoalResponse> Get(int id, CancellationToken ct) => service.GetByIdAsync(id, ct);
    [HttpPost, Authorize(Roles = "Admin,Mentor")] public async Task<ActionResult<GoalResponse>> Create(CreateGoalRequest request, CancellationToken ct) { var result = await service.CreateAsync(request, ct); return CreatedAtAction(nameof(Get), new { id = result.GoalId }, result); }
    [HttpPut("{id:int}"), Authorize(Roles = "Admin,Mentor")] public Task<GoalResponse> Update(int id, UpdateGoalRequest request, CancellationToken ct) => service.UpdateAsync(id, request, ct);
}
