namespace DotnetAPI.Dtos
{
    public partial class PostToAddDto
    {
        public string PostId {get; set;} = "";
        public string UserId {get; set;} = "";
        public string PostTitle {get; set;} = "";
        public string PostContent {get; set;} = "";
    }
}