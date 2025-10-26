using Core.Entities;
using System;
using System.Collections.Generic;

namespace Entities.Dtos
{
    /// <summary>
    /// DTO for transferring sponsorship codes from main sponsor to dealer
    /// Used in POST /api/Sponsorship/dealer/transfer-codes
    /// </summary>
    public class DealerCodeTransferDto : IDto
    {
        /// <summary>
        /// ID of the dealer (must be existing sponsor user)
        /// </summary>
        public int DealerId { get; set; }

        /// <summary>
        /// ID of the purchase to transfer codes from
        /// </summary>
        public int PurchaseId { get; set; }

        /// <summary>
        /// Number of codes to transfer (must be <= available unused codes)
        /// </summary>
        public int CodeCount { get; set; }

        /// <summary>
        /// Optional note about the transfer
        /// </summary>
        public string TransferNote { get; set; }
    }

    /// <summary>
    /// Response DTO for code transfer operation
    /// </summary>
    public class DealerCodeTransferResponseDto : IDto
    {
        /// <summary>
        /// List of transferred sponsorship code IDs
        /// </summary>
        public List<int> TransferredCodeIds { get; set; }

        /// <summary>
        /// Number of codes successfully transferred
        /// </summary>
        public int TransferredCount { get; set; }

        /// <summary>
        /// Dealer ID who received the codes
        /// </summary>
        public int DealerId { get; set; }

        /// <summary>
        /// Dealer name
        /// </summary>
        public string DealerName { get; set; }

        /// <summary>
        /// Transfer timestamp
        /// </summary>
        public DateTime TransferredAt { get; set; }
    }
}
