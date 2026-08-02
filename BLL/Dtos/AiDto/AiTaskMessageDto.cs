namespace BLL.Dtos.AiDto
{
    public class AiTaskMessageDto
    {
        public string TaskType { get; set; } = string.Empty; // "AnalyzeImage" or "ReadBill"
        public string S3ObjectKey { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public bool TrackCalories { get; set; } = true;
        public bool EstimatePrice { get; set; } = true;
    }
}
