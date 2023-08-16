# .NET-Core-7-with-MS-SQL


Users and Passwords
mahabub.rony@gmail.com
mahabub



Stored Procedure commands -- 

## User ##
EXEC TutorialAppSchema.spUsers_Get 
@UserId=3, 
@Active = 1

EXEC TutorialAppSchema.spUser_Upsert 
@FirstName,
@LastName,
@Email,
@Gender,
@JobTitle,
@Department,
@Salary,
@Active,
@UserId

EXEC TutorialAppSchema.spUser_Delete 
@UserId


## Post ##
EXEC TutorialAppSchema.spPosts_Get 
@UserId, 
@SearchValue, 
@PostId

EXEC TutorialAppSchema.spPosts_Upsert 
@UserId,
@PostTitle,
@PostContent,
@PostId

EXEC TutorialAppSchema.spPost_Delete
@PostId,
@UserId

## Login_Registration ##
EXEC TutorialAppSchema.spLoginConfirmation_Get
@Email

EXEC TutorialAppSchema.spRegistration_Upsert
@Email,
@PasswordHash,
@PasswordSalt