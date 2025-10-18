using System;
using Core.Entities;
using Core.Entities.Concrete;

namespace Entities.Concrete
{
    /// <summary>
    /// Farmer-initiated blocking of sponsors for messaging
    /// Allows farmers to prevent unwanted messages from specific sponsors
    /// </summary>
    public class FarmerSponsorBlock : IEntity
    {
        /// <summary>
        /// Primary key
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Farmer user ID who is blocking
        /// </summary>
        public int FarmerId { get; set; }

        /// <summary>
        /// Sponsor user ID being blocked
        /// </summary>
        public int SponsorId { get; set; }

        /// <summary>
        /// Is the sponsor blocked (cannot send messages)
        /// </summary>
        public bool IsBlocked { get; set; }

        /// <summary>
        /// Is the sponsor muted (can send but farmer doesn't get notifications)
        /// </summary>
        public bool IsMuted { get; set; }

        /// <summary>
        /// When the block/mute was created
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Optional reason for blocking (e.g., "Spam", "Inappropriate", "No longer needed")
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// Navigation property to farmer
        /// </summary>
        public virtual User Farmer { get; set; }

        /// <summary>
        /// Navigation property to sponsor
        /// </summary>
        public virtual User Sponsor { get; set; }
    }
}
