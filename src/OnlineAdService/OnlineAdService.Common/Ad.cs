using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace OnlineAdService.Common
{
    public class Ad
    {
        public int AdId { get; set; }

        [StringLength(100)]
        public string Title { get; set; }

        public int Price { get; set; }

        [StringLength(1000)]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        [StringLength(2083)]
        [DisplayName("Full-size Image")]
        public string ImageUrl { get; set; }

        [StringLength(2083)]
        [DisplayName("Thumbnail")]
        public string ThumbnailUrl { get; set; }

        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime PostedDate { get; set; }

        public Category? Category { get; set; }

        [StringLength(12)]
        public string Phone { get; set; }
    }
}
