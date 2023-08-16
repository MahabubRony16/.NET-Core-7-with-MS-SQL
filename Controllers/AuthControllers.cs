using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using DotnetAPI.Data;
using DotnetAPI.Helpers;
using DotnetWebApi.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.IdentityModel.Tokens;

namespace DotnetApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly DataContextDapper _dapper;
        private readonly AuthHelper _authHelper;
        public AuthController(IConfiguration config)
        {
            _dapper = new DataContextDapper(config);
            _authHelper = new AuthHelper(config);
        }

        [HttpPut("ResetPassword")]
        public IActionResult ResetPassword(UserForLoginDto userForSetPassword)
        {
            if(_authHelper.SetPassword(userForSetPassword))
            {
                return Ok();
            }
            throw new Exception("Failed to update password");
        }

        [AllowAnonymous]
        [HttpPost("Register")]
        public IActionResult Register(UserForRegistrationDto userForRegistration)
        {
            if (userForRegistration.Password == userForRegistration.PasswordConfirm)
            {
                string sqlCheckUserExists = @"SELECT Email FROM TutorialAppSchema.Auth 
                                                WHERE Email = '" + userForRegistration.Email + "'";
                IEnumerable<string> existingUsers = _dapper.LoadData<string>(sqlCheckUserExists);
                if (existingUsers.Count() == 0)
                {
                    UserForLoginDto userForSetPassword = new UserForLoginDto(){
                        Email = userForRegistration.Email,
                        Password = userForRegistration.Password
                    };
                    if(_authHelper.SetPassword(userForSetPassword))
                    {
                        string sqlAddUser = @"EXEC TutorialAppSchema.spUser_Upsert
                                        @FirstName = '" + userForRegistration.FirstName + 
                                    "', @LastName = '" + userForRegistration.LastName +
                                    "', @Email = '" + userForRegistration.Email + 
                                    "', @Gender = '" + userForRegistration.Gender + 
                                    "', @Active = '" + 1 + 
                                    "', @JobTitle = '" + userForRegistration.JobTitle + 
                                    "', @Department = '" + userForRegistration.Department + 
                                    "', @Salary = " + userForRegistration.Salary;

                        if(_dapper.ExecuteSql(sqlAddUser))
                        {
                            return Ok();
                        }
                        throw new Exception("Failed to add user");
                    }
                    throw new Exception("Failed to register user!");

                }
                throw new Exception("User with this email already exists!");
            }
            throw new Exception("Passwords do not match!");
        }

        [AllowAnonymous]
        [HttpPost("Login")]
        public IActionResult Login(UserForLoginDto userForLogin)
        {
            string sqlForHashAndSalt = @"EXEC TutorialAppSchema.spLoginConfirmation_Get 
                                            @Email = @EmailParam";

           
            // List<SqlParameter> sqlParameters = new List<SqlParameter>();
            // SqlParameter emailParameter = new SqlParameter("@EmailParam", SqlDbType.NVarChar);
            // emailParameter.Value = userForLogin.Email;
            // sqlParameters.Add(emailParameter);

            DynamicParameters sqlParameters = new DynamicParameters();
            sqlParameters.Add("@EmailParam", userForLogin.Email, DbType.String);

            UserForLoginConfirmationDto userForConfirmation = _dapper
            .LoadDataSingleWithParameters<UserForLoginConfirmationDto>(sqlForHashAndSalt, sqlParameters);
            
            byte[] passwordHash = _authHelper.GetPasswordHash(userForLogin.Password, userForConfirmation.PasswordSalt);

            // if(passwordHash == userForLoginConfirmation.PasswordHash) //Won't work, because it will compare the pointers of these objects

            for(int index = 0; index < passwordHash.Length; index++)
            {
                if(passwordHash[index] != userForConfirmation.PasswordHash[index])
                {
                    return StatusCode(401, "Incorrect Password!");
                }
            }

            string userIdSql = @"SELECT [UserId]
                                FROM [TutorialAppSchema].[Users]
                                WHERE Email = '" + userForLogin.Email + "'";
            int userId = _dapper.LoadDataSingle<int>(userIdSql);
            // Console.WriteLine(userId);
            return Ok(new Dictionary<string, string> 
            {
                {"token", _authHelper.CreateToken(userId)}
            });
        }

        [HttpGet("RefreshToken")]
        public string RefreshToken()
        {
            string UserIdSql = @"SELECT UserId FROM TutorialAppSchema.Users 
                                    WHERE UserId = '" + this.User.FindFirst("userId")?.Value + "'";
            // there User is coming from the Inherited class
            int userId = _dapper.LoadDataSingle<int>(UserIdSql);
            return _authHelper.CreateToken(userId);
        }
        
    }
}
