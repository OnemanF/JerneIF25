using System.ComponentModel.DataAnnotations;

namespace api.Models;

public record PublishWinningNumbersDto(long GameId, int[] Numbers); // exactly 3