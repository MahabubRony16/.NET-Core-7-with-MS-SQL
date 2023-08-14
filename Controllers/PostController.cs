using System.Security.Claims;
using DotnetAPI.Data;
using DotnetAPI.Dtos;
using DotnetAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace DotnetAPI.Controllers
{
    //This is from Rony-16
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class PostController: ControllerBase
    {
        private readonly DataContextDapper _dapper;
        public PostController(IConfiguration config)
        {
            _dapper = new DataContextDapper(config);
        }

        [HttpGet("Posts")]
        public IEnumerable<Post> GetPosts()
        {
            string sql = @"SELECT [PostId],
                                [UserId],
                                [PostTitle],
                                [PostContent],
                                [PostCreated],
                                [PostUpdated] 
                        FROM TutorialAppSchema.Posts";
            return _dapper.LoadData<Post>(sql);
        }

        [HttpGet("PostSingle/{postId}")]
        public IEnumerable<Post> GetPostSingle(int postId)
        {
            string sql = @"SELECT [PostId],
                                [UserId],
                                [PostTitle],
                                [PostContent],
                                [PostCreated],
                                [PostUpdated] 
                        FROM TutorialAppSchema.Posts
                        WHERE PostId = " + postId.ToString();
            return _dapper.LoadData<Post>(sql);
        }

        [HttpGet("PostByUser/{userId}")]
        public IEnumerable<Post> GetPostByUser(int userId)
        {
            string sql = @"SELECT [PostId],
                                [UserId],
                                [PostTitle],
                                [PostContent],
                                [PostCreated],
                                [PostUpdated] 
                        FROM TutorialAppSchema.Posts
                        WHERE UserId = " + userId.ToString();
            return _dapper.LoadData<Post>(sql);
        }

        [HttpGet("MyPosts")]
        public IEnumerable<Post> GetMyPosts()
        {
            // Claim? userIdClaim = User.FindFirst("userId");
            // string userId = "";
            // if (userIdClaim != null)
            // {
            //     userId = userIdClaim.Value;
            // }
            string sql = @"SELECT [PostId],
                                [UserId],
                                [PostTitle],
                                [PostContent],
                                [PostCreated],
                                [PostUpdated] 
                        FROM TutorialAppSchema.Posts
                        WHERE UserId = " + this.User.FindFirst("userId")?.Value;
            // there User is coming from the Inherited class
            return _dapper.LoadData<Post>(sql);
        }


        [HttpPost("Post")]
        public IActionResult AddPost(PostToAddDto postToAdd)
        {
            string sql = @"INSERT INTO TutorialAppSchema.Posts(
                        [UserId],
                        [PostTitle],
                        [PostContent],
                        [PostCreated],
                        [PostUpdated]) VALUES ('" + 
                        this.User.FindFirst("userId")?.Value + "','" +
                        postToAdd.PostTitle + "','" +
                        postToAdd.PostContent + "'," +
                        " GETDATE(), GETDATE() )";
            if (_dapper.ExecuteSql(sql))
            {
                return Ok();
            }
            throw new Exception("Failed to create new post!");

        }

        [HttpPut("Post")]
        public IActionResult EditPost(PostToEditDto postToEdit)
        {
            string sql = @"
            UPDATE TutorialAppSchema.Posts 
                SET PostContent = '" + postToEdit.PostContent + 
                "', PostTitle = '" + postToEdit.PostTitle + 
                "', PostUpdated = GETDATE() WHERE PostId = " + postToEdit.PostId.ToString() +
                "AND UserId = " + this.User.FindFirst("userId")?.Value;

            if (_dapper.ExecuteSql(sql))
            {
                return Ok();
            }
            throw new Exception("Failed to edit post!");

        }

        [HttpDelete("Post/{postId}")]
        public IActionResult DeletePost(int postId)
        {
            string sql = "DELETE FROM TutorialAppSchema.Posts WHERE PostId = " +
                        postId.ToString() + 
                        "AND UserId = " + this.User.FindFirst("userId")?.Value;
            if (_dapper.ExecuteSql(sql))
            {
                return Ok();
            }
            throw new Exception("Failed to delete post!");
        }
    }
}