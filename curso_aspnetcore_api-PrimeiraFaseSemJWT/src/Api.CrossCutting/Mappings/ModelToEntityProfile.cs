﻿using Api.Domain.Entities;
using AutoMapper;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace CrossCutting.Mappings
{
    class ModelToEntityProfile : Profile
    {
        public ModelToEntityProfile()
        {
            CreateMap<UserModel, UserEntity>()
                  .ReverseMap();
        }
    }
}

// pegar da service para data