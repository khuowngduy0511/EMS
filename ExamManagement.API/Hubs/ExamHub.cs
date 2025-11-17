using Microsoft.AspNetCore.SignalR;

namespace ExamManagement.API.Hubs;

public class ExamHub : Hub
{
    public async Task SendSubmissionUploaded(int submissionId, string studentName, List<string> violations)
    {
        await Clients.All.SendAsync("submissionUploaded", new
        {
            SubmissionId = submissionId,
            StudentName = studentName,
            Violations = violations
        });
    }

    public async Task SendGradeUpdated(int submissionId, decimal totalScore)
    {
        await Clients.All.SendAsync("gradeUpdated", new
        {
            SubmissionId = submissionId,
            TotalScore = totalScore
        });
    }

    public async Task SendViolationFlagged(int submissionId, string violationType)
    {
        await Clients.All.SendAsync("violationFlagged", new
        {
            SubmissionId = submissionId,
            ViolationType = violationType
        });
    }
}

