using apbd_cw6.DTOs;
using apbd_cw6.Services;
using Microsoft.AspNetCore.Mvc;

namespace apbd_cw6.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly AppointmentService _service;

    public AppointmentsController(AppointmentService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<AppointmentListDto>>> GetAll([FromQuery] string? status, [FromQuery] string? patientLastName)
    {
        var data = await _service.GetAppointmentsAsync(status, patientLastName);
        return Ok(data);
    }

    [HttpGet("{idAppointment}")]
    public async Task<IActionResult> GetAppointmentById(int idAppointment)
    {
        var appointment = await _service.GetAppointmentByIdAsync(idAppointment);

        if (appointment == null)
        {
            return NotFound(new ErrorResponseDto
            {
                Message = $"Appointment with id {idAppointment} was not found."
            });
        }

        return Ok(appointment);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentRequestDto request)
    {
        var result = await _service.CreateAppointmentAsync(request);

        if (!result.IsSuccess)
        {
            if (result.IsConflict)
            {
                return Conflict(new ErrorResponseDto
                {
                    Message = result.Message
                });
            }

            return BadRequest(new ErrorResponseDto
            {
                Message = result.Message
            });
        }

        return CreatedAtAction(
            nameof(GetAppointmentById),
            new { idAppointment = result.CreatedId },
            new
            {
                IdAppointment = result.CreatedId,
                Message = result.Message
            });
    }
}