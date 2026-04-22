namespace apbd_cw6.DTOs
{
    public class OperationResultDto
    {
        public bool IsSuccess { get; set; }
        public bool IsConflict { get; set; }
        public bool IsNotFound { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? CreatedId { get; set; }
    }
}