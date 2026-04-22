using System.Data;
using apbd_cw6.DTOs;
using Microsoft.Data.SqlClient;

namespace apbd_cw6.Services;

public class AppointmentService
{
    private readonly IConfiguration _config;

    public AppointmentService(IConfiguration config)
    {
        _config = config;
    }

    public async Task<List<AppointmentListDto>> GetAppointmentsAsync(string? status, string? patientLastName)
    {
        var appointments = new List<AppointmentListDto>();

        var connectionString = _config.GetConnectionString("DefaultConnection");

        await using var connection = new SqlConnection(connectionString);

        const string sql = @"
        SELECT
            a.IdAppointment,
            a.AppointmentDate,
            a.Status,
            a.Reason,
            p.FirstName,
            p.LastName,
            p.Email
        FROM dbo.Appointments a
        JOIN dbo.Patients p ON p.IdPatient = a.IdPatient
        WHERE (@Status IS NULL OR a.Status = @Status)
          AND (@PatientLastName IS NULL OR p.LastName = @PatientLastName)
        ORDER BY a.AppointmentDate;";

        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add("@Status", SqlDbType.NVarChar, 50).Value =
            string.IsNullOrWhiteSpace(status) ? DBNull.Value : status;

        command.Parameters.Add("@PatientLastName", SqlDbType.NVarChar, 100).Value =
            string.IsNullOrWhiteSpace(patientLastName) ? DBNull.Value : patientLastName;

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        var idIndex = reader.GetOrdinal("IdAppointment");
        var dateIndex = reader.GetOrdinal("AppointmentDate");
        var statusIndex = reader.GetOrdinal("Status");
        var reasonIndex = reader.GetOrdinal("Reason");
        var firstNameIndex = reader.GetOrdinal("FirstName");
        var lastNameIndex = reader.GetOrdinal("LastName");
        var emailIndex = reader.GetOrdinal("Email");

        while (await reader.ReadAsync())
        {
            var dto = new AppointmentListDto
            {
                IdAppointment = reader.GetInt32(idIndex),
                AppointmentDate = reader.GetDateTime(dateIndex),
                Status = reader.GetString(statusIndex),
                Reason = reader.GetString(reasonIndex),
                PatientFirstName = reader.GetString(firstNameIndex),
                PatientLastName = reader.GetString(lastNameIndex),
                PatientEmail = reader.GetString(emailIndex)
            };

            appointments.Add(dto);
        }

        return appointments;
    }
    public async Task<AppointmentDetailsDto?> GetAppointmentByIdAsync(int idAppointment)
    {
        var connectionString = _config.GetConnectionString("DefaultConnection");

        await using var connection = new SqlConnection(connectionString);

        const string sql = @"
        SELECT
            a.IdAppointment,
            a.AppointmentDate,
            a.Status,
            a.Reason,
            a.InternalNotes,
            a.CreatedAt,
            p.IdPatient,
            p.FirstName AS PatientFirstName,
            p.LastName AS PatientLastName,
            p.Email AS PatientEmail,
            p.Phone AS PatientPhone,
            d.IdDoctor,
            d.FirstName AS DoctorFirstName,
            d.LastName AS DoctorLastName,
            d.LicenseNumber,
            s.Name AS SpecializationName
        FROM dbo.Appointments a
        JOIN dbo.Patients p ON p.IdPatient = a.IdPatient
        JOIN dbo.Doctors d ON d.IdDoctor = a.IdDoctor
        JOIN dbo.Specializations s ON s.IdSpecialization = d.IdSpecialization
        WHERE a.IdAppointment = @IdAppointment;";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@IdAppointment", SqlDbType.Int).Value = idAppointment;

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new AppointmentDetailsDto
        {
            IdAppointment = reader.GetInt32(reader.GetOrdinal("IdAppointment")),
            AppointmentDate = reader.GetDateTime(reader.GetOrdinal("AppointmentDate")),
            Status = reader.GetString(reader.GetOrdinal("Status")),
            Reason = reader.GetString(reader.GetOrdinal("Reason")),

            IdPatient = reader.GetInt32(reader.GetOrdinal("IdPatient")),
            PatientFirstName = reader.GetString(reader.GetOrdinal("PatientFirstName")),
            PatientLastName = reader.GetString(reader.GetOrdinal("PatientLastName")),
            PatientEmail = reader.GetString(reader.GetOrdinal("PatientEmail")),
            PatientPhone = reader.IsDBNull(reader.GetOrdinal("PatientPhone"))
                ? null
                : reader.GetString(reader.GetOrdinal("PatientPhone")),

            IdDoctor = reader.GetInt32(reader.GetOrdinal("IdDoctor")),
            DoctorFirstName = reader.GetString(reader.GetOrdinal("DoctorFirstName")),
            DoctorLastName = reader.GetString(reader.GetOrdinal("DoctorLastName")),
            DoctorLicenseNumber = reader.GetString(reader.GetOrdinal("LicenseNumber")),
            SpecializationName = reader.GetString(reader.GetOrdinal("SpecializationName")),

            InternalNotes = reader.IsDBNull(reader.GetOrdinal("InternalNotes"))
                ? null
                : reader.GetString(reader.GetOrdinal("InternalNotes")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }
}