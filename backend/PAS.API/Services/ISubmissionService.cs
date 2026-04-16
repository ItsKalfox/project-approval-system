using PAS.API.DTOs.Submission;

namespace PAS.API.Services;

public interface ISubmissionService
{
    /// <summary>
    /// Returns all active courseworks with open deadlines (submission points visible to a student).
    /// Includes whether the student has already submitted.
    /// </summary>
    Task<IEnumerable<SubmissionPointDto>> GetSubmissionPointsAsync(int studentUserId);

    /// <summary>
    /// Returns the student's submission for a specific coursework, or null if not found.
    /// </summary>
    Task<SubmissionResponseDto?> GetMySubmissionAsync(int studentUserId, int courseworkId);

    /// <summary>
    /// Returns all project submissions made by the student.
    /// Includes statuses, deadlines, and supervisor matching if available.
    /// </summary>
    Task<IEnumerable<SubmissionResponseDto>> GetStudentSubmissionsAsync(int studentUserId);

    /// <summary>
    /// Creates a new submission (Project + CourseworkProject link + Blob upload).
    /// </summary>
    Task<SubmissionResponseDto> CreateSubmissionAsync(
        int studentUserId, int courseworkId,
        CreateSubmissionDto dto, IFormFile file);

    /// <summary>
    /// Updates an existing submission before the deadline.
    /// </summary>
    Task<SubmissionResponseDto> UpdateSubmissionAsync(
        int studentUserId, int projectId,
        UpdateSubmissionDto dto, IFormFile? file);

    /// <summary>
    /// Soft-deletes a submission and removes the blob from Azure. Only before deadline.
    /// </summary>
    Task DeleteSubmissionAsync(int studentUserId, int projectId);

    /// <summary>
    /// Returns the PDF file stream for secure viewing/downloading.
    /// </summary>
    Task<(Stream FileStream, string ContentType, string FileName)> GetSubmissionFileAsync(
        int studentUserId, int projectId);
}
