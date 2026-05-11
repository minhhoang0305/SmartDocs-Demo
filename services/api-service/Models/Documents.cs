namespace api_service.Models
{
    public class Documents
    {
        public int Id {get; set;}
        public string FileName {get; set;} = string.Empty;
        public string FileUrl {get; set;} = string.Empty;
        public DateTime CreateAt {get; set;}
    }
}