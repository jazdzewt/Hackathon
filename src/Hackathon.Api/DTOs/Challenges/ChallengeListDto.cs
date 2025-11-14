namespace Hackathon.Api.DTOs.Challenges;

public record ChallengeListDto(
    int Id,
    string Name,
    string ShortDescription,
    DateTime StartDate,
    DateTime? EndDate,
    bool IsActive
);
