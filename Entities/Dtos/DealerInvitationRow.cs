namespace Entities.Dtos
{
    /// <summary>
    /// Represents a single row from the Excel file for dealer invitation
    /// Used during parsing and validation
    /// </summary>
    public class DealerInvitationRow
    {
        public int RowNumber { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string DealerName { get; set; }
        public int? CodeCount { get; set; }      // Optional: row-specific code count
        public string PackageTier { get; set; }  // Optional: row-specific tier (S, M, L, XL)
    }
}
