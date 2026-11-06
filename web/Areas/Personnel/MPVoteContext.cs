using Microsoft.EntityFrameworkCore;

namespace Viper.Areas.Personnel;

/// <summary>
/// The MPVote database, read by EIS only for the MyInfoVault boards and focus areas its legacy
/// procedures (usp_get_boards, usp_get_researchFocus, usp_get_specialtyFocus) return. It maps no
/// tables; no Merit/Promotion code is ported.
/// </summary>
public class MPVoteContext : DbContext
{
    public MPVoteContext(DbContextOptions<MPVoteContext> options) : base(options)
    {
    }
}
