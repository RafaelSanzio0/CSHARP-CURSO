﻿using Api.Data.Context;
using Data.Implementations;
using Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Api.Data.Test
{
    public class CepCrudCompleto : BaseTest, IClassFixture<DbTeste>
    {
        private ServiceProvider _serviceProvide;

        public CepCrudCompleto(DbTeste dbTeste)
        {
            _serviceProvide = dbTeste.ServiceProvider;
        }

        [Fact(DisplayName = "CRUD de Cep")]
        [Trait("CRUD", "CepEntity")]
        public async Task E_Possivel_Realizar_CRUD_Cep()
        {
            using (var context = _serviceProvide.GetService<MyContext>())
            {
                MunicipioImplementation _repositorioMunicipio = new MunicipioImplementation(context);
                MunicipioEntity _entityMunicipio = new MunicipioEntity
                {
                    Nome = Faker.Address.City(),
                    CodIBGE = Faker.RandomNumber.Next(1000000, 9999999),
                    UfID = new Guid("e7e416de-477c-4fa3-a541-b5af5f35ccf6")
                };

                var _registroCriado = await _repositorioMunicipio.InsertAsync(_entityMunicipio);
                Assert.NotNull(_registroCriado);
                Assert.Equal(_entityMunicipio.Nome, _registroCriado.Nome);
                Assert.Equal(_entityMunicipio.CodIBGE, _registroCriado.CodIBGE);
                Assert.Equal(_entityMunicipio.UfID, _registroCriado.UfID);
                Assert.False(_registroCriado.Id == Guid.Empty);

                CepImplementation _repositorio = new CepImplementation(context);
                CepEntity _entityCep = new CepEntity
                {
                    Cep = "13.481-001",
                    Logradouro = Faker.Address.StreetName(),
                    Numero = "0 até 2000",
                    MunicipioID = _registroCriado.Id // associa o ID do municpio ao cep criado
                };

                var _registroCriadoCep = await _repositorio.InsertAsync(_entityCep);
                Assert.NotNull(_registroCriadoCep);
                Assert.Equal(_entityCep.Cep, _registroCriadoCep.Cep);
                Assert.Equal(_entityCep.Logradouro, _registroCriadoCep.Logradouro);
                Assert.Equal(_entityCep.Numero, _registroCriadoCep.Numero);
                Assert.Equal(_entityCep.MunicipioID, _registroCriadoCep.MunicipioID);
                Assert.False(_registroCriadoCep.Id == Guid.Empty);

                _entityCep.Logradouro = Faker.Address.StreetName();
                _entityCep.Id = _registroCriadoCep.Id;
                var _registroAtualizado = await _repositorio.UpdateAsync(_entityCep);
                Assert.NotNull(_registroAtualizado);
                Assert.Equal(_entityCep.Cep, _registroAtualizado.Cep);
                Assert.Equal(_entityCep.Logradouro, _registroAtualizado.Logradouro);
                Assert.Equal(_entityCep.MunicipioID, _registroAtualizado.MunicipioID);
                Assert.True(_registroCriadoCep.Id == _entityCep.Id);

                var _registroExiste = await _repositorio.ExistAsync(_registroAtualizado.Id);
                Assert.True(_registroExiste);

                var _registroSelecionado = await _repositorio.SelectAsync(_registroAtualizado.Id);
                Assert.NotNull(_registroSelecionado);
                Assert.Equal(_registroAtualizado.Cep, _registroSelecionado.Cep);
                Assert.Equal(_registroAtualizado.Logradouro, _registroSelecionado.Logradouro);
                Assert.Equal(_registroAtualizado.Numero, _registroSelecionado.Numero);
                Assert.Equal(_registroAtualizado.MunicipioID, _registroSelecionado.MunicipioID);

                _registroSelecionado = await _repositorio.SelectAsync(_registroAtualizado.Cep);
                Assert.NotNull(_registroSelecionado);
                Assert.Equal(_registroAtualizado.Cep, _registroSelecionado.Cep);
                Assert.Equal(_registroAtualizado.Logradouro, _registroSelecionado.Logradouro);
                Assert.Equal(_registroAtualizado.Numero, _registroSelecionado.Numero);
                Assert.Equal(_registroAtualizado.MunicipioID, _registroSelecionado.MunicipioID);
                Assert.NotNull(_registroSelecionado.MunicipioEntity);
                Assert.Equal(_entityMunicipio.Nome, _registroSelecionado.MunicipioEntity.Nome);
                Assert.NotNull(_registroSelecionado.MunicipioEntity.UfEntity);
                Assert.Equal("SP", _registroSelecionado.MunicipioEntity.UfEntity.Sigla);

                var _todosRegistros = await _repositorio.SelectAsync();
                Assert.NotNull(_todosRegistros);
                Assert.True(_todosRegistros.Count() > 0);

                var _removeu = await _repositorio.DeleteAsync(_registroSelecionado.Id);
                Assert.True(_removeu);

                _todosRegistros = await _repositorio.SelectAsync();
                Assert.NotNull(_todosRegistros);
                Assert.True(_todosRegistros.Count() == 0);
            }
        }

    }
}
