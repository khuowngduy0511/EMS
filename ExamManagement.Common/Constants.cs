namespace ExamManagement.Common;

public static class Constants
{
    public const int MaxFileSize = 50 * 1024 * 1024; // 50 MB
    public const string AllowedFileExtensions = ".rar,.zip,.doc,.docx";
    
    public const string JwtSecretKey = "YourSuperSecretKeyForJWTTokenGeneration-AtLeast32Characters!";
    public const string JwtIssuer = "ExamManagement";
    public const string JwtAudience = "ExamManagementUsers";
    public const int JwtExpirationMinutes = 60;
    public const int RefreshTokenExpirationDays = 7;
    
    public static class ConnectionStrings
    {
        public const string AuthDB = "Server=localhost;Database=AuthDB;Trusted_Connection=True;TrustServerCertificate=True;";
        public const string SubjectDB = "Server=localhost;Database=SubjectDB;Trusted_Connection=True;TrustServerCertificate=True;";
        public const string SemesterDB = "Server=localhost;Database=SemesterDB;Trusted_Connection=True;TrustServerCertificate=True;";
        public const string ExamDB = "Server=localhost;Database=ExamDB;Trusted_Connection=True;TrustServerCertificate=True;";
        public const string RubricDB = "Server=localhost;Database=RubricDB;Trusted_Connection=True;TrustServerCertificate=True;";
        public const string ExaminerDB = "Server=localhost;Database=ExaminerDB;Trusted_Connection=True;TrustServerCertificate=True;";
        public const string SubmissionDB = "Server=localhost;Database=SubmissionDB;Trusted_Connection=True;TrustServerCertificate=True;";
        public const string ViolationDB = "Server=localhost;Database=ViolationDB;Trusted_Connection=True;TrustServerCertificate=True;";
        public const string ReportDB = "Server=localhost;Database=ReportDB;Trusted_Connection=True;TrustServerCertificate=True;";
    }
}

