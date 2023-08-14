using System.Data;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using DotnetAPI.Data;
using DotnetWebApi.Dtos;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace DotnetWebApi.Controllers
{
    public class AuthController : ControllerBase
    {
        private readonly DataContextDapper _dapper;
        private readonly IConfiguration _config;
        public AuthController(IConfiguration config)
        {
            _dapper = new DataContextDapper(config);
            _config = config;
        }

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
                    byte[] passwordSalt = new byte[128 / 8];
                    using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
                    {
                        rng.GetNonZeroBytes(passwordSalt);
                    }

                    byte[] passwordHash = GetPasswordHash(userForRegistration.Password, passwordSalt);
                    
                    string sqlAddAuth = @"
                                        INSERT INTO TutorialAppSchema.Auth ([Email],
                                        [PasswordHash],
                                        [PasswordSalt]) VALUES ('" + userForRegistration.Email + 
                                        "', @PasswordHash, @PasswordSalt)";

                    List<SqlParameter> sqlParameters = new List<SqlParameter>();

                    SqlParameter passwordSaltParameter = new SqlParameter("@PasswordSalt", SqlDbType.VarBinary);
                    passwordSaltParameter.Value = passwordSalt;

                    SqlParameter passwordHashParameter = new SqlParameter("@PasswordHash", SqlDbType.VarBinary);
                    passwordHashParameter.Value = passwordHash;

                    sqlParameters.Add(passwordSaltParameter);
                    sqlParameters.Add(passwordHashParameter);

                    if(_dapper.ExecuteSqlWithParameters(sqlAddAuth, sqlParameters))
                    {
                        string sqlAddUser = @"INSERT INTO TutorialAppSchema.Users(
                                                [FirstName],
                                                [LastName],
                                                [Email],
                                                [Gender],
                                                [Active]
                                            ) VALUES (" +
                                                "'" + userForRegistration.FirstName + 
                                                "', '" + userForRegistration.LastName +
                                                "', '" + userForRegistration.Email + 
                                                "', '" + userForRegistration.Gender + 
                                                "', '" + "1" + 
                                            "')";
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

        [HttpPost("Login")]
        public IActionResult Login(UserForLoginDto userForLogin)
        {
            string sqlForHashAndSalt = @"
                                        SELECT 
                                        [PasswordHash],
                                        [PasswordSalt] 
                                        FROM TutorialAppSchema.Auth 
                                        WHERE Email = '" + userForLogin.Email + "'";

            UserForLoginConfirmationDto userForLoginConfirmation = _dapper.LoadDataSingle<UserForLoginConfirmationDto>(sqlForHashAndSalt);
            
            byte[] passwordHash = GetPasswordHash(userForLogin.Password, userForLoginConfirmation.PasswordSalt);

            // if(passwordHash == userForLoginConfirmation.PasswordHash) //Won't work, because it will compare the pointers of these objects

            for(int index = 0; index < passwordHash.Length; index++)
            {
                if(passwordHash[index] != userForLoginConfirmation.PasswordHash[index])
                {
                    return StatusCode(401, "Incorrect Password!");
                }
            }
            return Ok();
        }

        private byte[] GetPasswordHash(string password, byte[] passwordSalt)
        {
            string passwordSaltPlusString = _config.GetSection("Appsettings:PasswordKey").Value + 
            Convert.ToBase64String(passwordSalt);
            byte[] passwordHash = KeyDerivation.Pbkdf2(
                password: password,
                salt: Encoding.ASCII.GetBytes(passwordSaltPlusString),
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 1000000,
                numBytesRequested: 256 / 8
            );
            return passwordHash;

        }
    }
}