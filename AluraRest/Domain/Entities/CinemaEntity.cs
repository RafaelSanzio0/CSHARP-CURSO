using Domain.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json.Serialization;

namespace Domain.Entities
{
    public class CinemaEntity : BaseEntity
    {
        [Required]
        public string Nome { get; set; }

        [Required]
        public EnderecoEntity Endereco { get; set; }

        [Required]
        public int EnderecoId { get; set; }

        public GerenteEntity Gerente { get; set; }

        public int GerenteId { get; set; }

        [JsonIgnore]
        public List<SessaoEntity> Sessoes { get; set; }
    }
}
