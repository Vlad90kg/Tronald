namespace Tronald.Models;

public record GuardianResponse(GuardianInner Response);

public record GuardianInner(string Status, int Total, int Pages, int CurrentPage,
    List<GuardianArticle> Results);

public record GuardianArticle(string Id, string WebTitle, DateTime WebPublicationDate,
    string WebUrl, string SectionId);