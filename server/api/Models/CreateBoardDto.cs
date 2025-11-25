using System.ComponentModel.DataAnnotations;

namespace api.Models;

public record CreateBoardDto(long PlayerId, int[] Numbers, long GameId);