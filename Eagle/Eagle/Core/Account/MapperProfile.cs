﻿using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Eagle.DbTables;
using Eagle.DomainModels;

namespace Eagle.Core.Account
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            CreateMap<DmAccount, UserEntity>().ReverseMap();
        }
    }
}