﻿using FinalDAW2.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinalDAW2.Models
{
    public class Post
    {
        [Key] 
        public int Id { get; set; }

        [Required(ErrorMessage = "Continutul Postarii este obligatoriu")]
        [StringLength(500, MinimumLength = 5, ErrorMessage = "Continutul Postarii trebuie să aibă între 5 și 500 de caractere.")]
        public string Continut { get; set; }


        public DateTime DataPostarii { get; set; }

        ///public string Image { get; set; }

        // Alte atribute relevante
        public string? UserId { get; set; }

        public int? GroupId { get; set; }

        public virtual ApplicationUser? User { get; set; }

        public virtual Group? Group  { get; set; }
        public virtual ICollection<Comment>? Comments { get; set; }


       

    }
}
