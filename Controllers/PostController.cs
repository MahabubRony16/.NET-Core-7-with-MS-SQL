using System.Data;
using System.Runtime.InteropServices;
using System.Security.Claims;
using Dapper;
using DotnetAPI.Data;
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

        [HttpGet("Posts/{userId}/{postId}/{searchParam}")]
        public IEnumerable<Post> GetAllPosts(int userId=0, int postId=0, string searchParam = "None")
        {
            string sql = @"EXEC TutorialAppSchema.spPosts_Get ";
            string stringParameters = "";
            DynamicParameters sqlParameters = new DynamicParameters();
            
            if(userId != 0)
            {
                stringParameters += ", @UserId=@UserIdParam";
                sqlParameters.Add("@UserIdParam", userId, DbType.Int32);
            }
            if(postId != 0)
            {
                stringParameters += ", @PostId=@PostIdParam";
                sqlParameters.Add("@PostIdParam", postId, DbType.Int32);
            }
            if(searchParam.ToLower() != "none")
            {
                stringParameters += ", @SearchValue=@SearchValueParam";
                sqlParameters.Add("@SearchValueParam", searchParam, DbType.String);
            }
            if (stringParameters.Length > 0)
            {
                sql += stringParameters.Substring(1);
            }
            System.Console.WriteLine(sql);
            return _dapper.LoadDataWithParameters<Post>(sql, sqlParameters);
        }
        
        [HttpGet("MyPosts")]
        public IEnumerable<Post> GetMyPosts()
        {
            string sql = @"EXEC TutorialAppSchema.spPosts_Get @UserId = @UserIdParam";
            // there User is coming from the Inherited class

            DynamicParameters sqlParameters = new DynamicParameters();
            sqlParameters.Add("@UserIdParam", this.User.FindFirst("userId")?.Value, DbType.Int32);

            return _dapper.LoadDataWithParameters<Post>(sql, sqlParameters);
        }


        [HttpPut("UpsertPost")]
        public IActionResult UpsertPost(Post postToUpsert)
        {
            string sql = @"EXEC TutorialAppSchema.spPosts_Upsert 
                @UserId=@UserIdParam
                , @PostTitle=@PostTitleParam
                , @PostContent=@PostContentParam";
                
            DynamicParameters sqlParameters = new DynamicParameters();
            sqlParameters.Add("@UserIdParam", this.User.FindFirst("userId")?.Value, DbType.Int32);
            sqlParameters.Add("@PostTitleParam", postToUpsert.PostTitle, DbType.String);
            sqlParameters.Add("@PostContentParam", postToUpsert.PostContent, DbType.String);

            if(postToUpsert.PostId > 0)
            {
                sql += ", @PostId=" + postToUpsert.PostId;
            }
            System.Console.WriteLine(sql);
            if (_dapper.ExecuteSqlWithParameters(sql, sqlParameters))
            {
                return Ok();
            }
            throw new Exception("Failed to upseart post!");
        }

        [HttpDelete("Post/{postId}")]
        public IActionResult DeletePost(int postId)
        {
            string sql = @"EXEC TutorialAppSchema.spPost_Delete @PostId=@PostIdParam
                        , @UserId=@UserIdParam";

            DynamicParameters sqlParameters = new DynamicParameters();
            sqlParameters.Add("@PostIdParam", postId, DbType.Int32);
            sqlParameters.Add("@UserIdParam", this.User.FindFirst("userId")?.Value, DbType.Int32);

            if (_dapper.ExecuteSqlWithParameters(sql, sqlParameters))
            {
                return Ok();
            }
            throw new Exception("Failed to delete post!");
        }
    }
}