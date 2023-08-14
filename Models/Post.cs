namespace DotnetAPI.Models
{
    public partial class Post
    {
        public string PostId {get; set;} = "";
        public string UserId {get; set;} = "";
        public string PostTitle {get; set;} = "";
        public string PostContent {get; set;} = "";
        public DateTime PostCreated {get; set;}
        public DateTime PostUpdated {get; set;}
    }
}