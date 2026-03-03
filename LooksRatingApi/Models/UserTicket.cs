using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LooksRatingApi.Models
{
    public class UserTicket
    {
        public Guid Id { get; set; }

        /// <summary>
        /// Причина жалобы.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Когда произошло нарушение.
        /// </summary>
        public DateTime OccuredAt { get; set; }

        /// <summary>
        /// Пользователь, на которого пожаловались.
        /// </summary>
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
    }
}