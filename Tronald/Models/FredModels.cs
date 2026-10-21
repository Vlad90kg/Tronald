namespace Tronald.Models;

public record FredObservationsResponse(List<FredObservation> Observations);
public record FredObservation(string Date, string Value);