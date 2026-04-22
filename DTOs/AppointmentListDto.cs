namespace apbd_cw6.DTOs
{
    public class AppointmentListDto
    {
        public int IdAppointment { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string PatientFirstName { get; set; } = string.Empty;
        public string PatientLastName { get; set; } = string.Empty;
        public string PatientEmail { get; set;} = string.Empty;
    }
}
