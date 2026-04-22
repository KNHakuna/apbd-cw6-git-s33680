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

    public async Task<OperationResultDto> CreateAppointmentAsync(CreateAppointmentRequestDto request)
    {
        if (request.IdPatient <= 0)
        {
            return new OperationResultDto
            {
                IsSuccess = false,
                Message = "IdPatient must be greater than 0."
            };
        }

        if (request.IdDoctor <= 0)
        {
            return new OperationResultDto
            {
                IsSuccess = false,
                Message = "IdDoctor must be greater than 0."
            };
        }

        if (request.AppointmentDate <= DateTime.Now)
        {
            return new OperationResultDto
            {
                IsSuccess = false,
                Message = "Appointment date cannot be in the past."
            };
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return new OperationResultDto
            {
                IsSuccess = false,
                Message = "Reason cannot be empty."
            };
        }

        if (request.Reason.Length > 250)
        {
            return new OperationResultDto
            {
                IsSuccess = false,
                Message = "Reason cannot be longer than 250 characters."
            };
        }

        var connectionString = _config.GetConnectionString("DefaultConnection");

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await using (var patientCommand = new SqlCommand(@"
        SELECT COUNT(1)
        FROM dbo.Patients
        WHERE IdPatient = @IdPatient AND IsActive = 1;
    ", connection))
        {
            patientCommand.Parameters.Add("@IdPatient", SqlDbType.Int).Value = request.IdPatient;

            var patientExists = (int)(await patientCommand.ExecuteScalarAsync() ?? 0);

            if (patientExists == 0)
            {
                return new OperationResultDto
                {
                    IsSuccess = false,
                    Message = "Patient does not exist or is inactive."
                };
            }
        }

        await using (var doctorCommand = new SqlCommand(@"
        SELECT COUNT(1)
        FROM dbo.Doctors
        WHERE IdDoctor = @IdDoctor AND IsActive = 1;
    ", connection))
        {
            doctorCommand.Parameters.Add("@IdDoctor", SqlDbType.Int).Value = request.IdDoctor;

            var doctorExists = (int)(await doctorCommand.ExecuteScalarAsync() ?? 0);

            if (doctorExists == 0)
            {
                return new OperationResultDto
                {
                    IsSuccess = false,
                    Message = "Doctor does not exist or is inactive."
                };
            }
        }

        await using (var conflictCommand = new SqlCommand(@"
        SELECT COUNT(1)
        FROM dbo.Appointments
        WHERE IdDoctor = @IdDoctor
          AND AppointmentDate = @AppointmentDate
          AND Status = 'Scheduled';
    ", connection))
        {
            conflictCommand.Parameters.Add("@IdDoctor", SqlDbType.Int).Value = request.IdDoctor;
            conflictCommand.Parameters.Add("@AppointmentDate", SqlDbType.DateTime2).Value = request.AppointmentDate;

            var conflictExists = (int)(await conflictCommand.ExecuteScalarAsync() ?? 0);

            if (conflictExists > 0)
            {
                return new OperationResultDto
                {
                    IsSuccess = false,
                    IsConflict = true,
                    Message = "Doctor already has a scheduled appointment at this time."
                };
            }
        }

        await using (var insertCommand = new SqlCommand(@"
        INSERT INTO dbo.Appointments
            (IdPatient, IdDoctor, AppointmentDate, Status, Reason, InternalNotes, CreatedAt)
        OUTPUT INSERTED.IdAppointment
        VALUES
            (@IdPatient, @IdDoctor, @AppointmentDate, @Status, @Reason, @InternalNotes, @CreatedAt);
    ", connection))
        {
            insertCommand.Parameters.Add("@IdPatient", SqlDbType.Int).Value = request.IdPatient;
            insertCommand.Parameters.Add("@IdDoctor", SqlDbType.Int).Value = request.IdDoctor;
            insertCommand.Parameters.Add("@AppointmentDate", SqlDbType.DateTime2).Value = request.AppointmentDate;
            insertCommand.Parameters.Add("@Status", SqlDbType.NVarChar, 50).Value = "Scheduled";
            insertCommand.Parameters.Add("@Reason", SqlDbType.NVarChar, 250).Value = request.Reason;
            insertCommand.Parameters.Add("@InternalNotes", SqlDbType.NVarChar, -1).Value = DBNull.Value;
            insertCommand.Parameters.Add("@CreatedAt", SqlDbType.DateTime2).Value = DateTime.Now;

            var createdId = (int)(await insertCommand.ExecuteScalarAsync() ?? 0);

            return new OperationResultDto
            {
                IsSuccess = true,
                Message = "Appointment created successfully.",
                CreatedId = createdId
            };
        }
    }
}