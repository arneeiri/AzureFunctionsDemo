namespace Contracts
{
    public class PublishImageCommand
    {
        public string DirectoryName { get; set; }
        public string ImageName { get; set; }
        public int ImageNumber { get; set; }
    }
}