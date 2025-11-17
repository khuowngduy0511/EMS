namespace ExamManagement.Common;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Moderator = "Moderator";
    public const string Examiner = "Examiner";
    
    public static readonly string[] AllRoles = { Admin, Manager, Moderator, Examiner };
}

