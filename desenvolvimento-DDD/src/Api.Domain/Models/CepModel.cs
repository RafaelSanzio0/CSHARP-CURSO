using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Models
{
    public class CepModel : BaseModel
    {
        private string _cep;

        public string Cep
        {
            get { return _cep; }
            set { _cep = value; }
        }

        private string _logradouro;

        public string Logradouro
        {
            get { return _logradouro; }
            set { _logradouro = value; }
        }

        private string _numero; // campo nao obrigatorio no DTO entao iremos criar uma validacao aqui

        public string Numero
        {
            get { return _numero; }
            set { _numero = string.IsNullOrEmpty(value) ? "S/N" : value; }
        }

        private Guid _municipioId;

        public Guid MunicipioID
        {
            get { return _municipioId; }
            set { _municipioId = value; }
        }

    }
}
