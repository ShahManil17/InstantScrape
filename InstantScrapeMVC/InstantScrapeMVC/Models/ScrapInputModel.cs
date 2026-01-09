namespace InstantScrapeMVC.Models
{
    public class ScrapInputModel
    {
        public string? Category { get; set; }
        public string? Place { get; set; }
        public int Start { get; set; } = 0;
    }

    public class BulkScrapInputModel
    {
        public string? Category { get; set; }
        public string? Place { get; set; }
        public int Size { get; set; } = 100;
    }
}
