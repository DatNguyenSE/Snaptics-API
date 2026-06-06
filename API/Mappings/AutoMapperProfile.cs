using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using BLL.Dtos;
using DAL.Entities;

namespace API.Mappings
{
    // AutoMapper profile to define mappings between entities and DTOs
    // Avoid the risk of displaying sensitive fields from the database.
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<Category, CategoryDto>().ReverseMap(); 
            CreateMap<Transaction, TransactionDto>().ReverseMap();
            CreateMap<TransactionDetail, TransactionDetailDto>().ReverseMap();
            CreateMap<ItemInventory, ItemInventoryDto>().ReverseMap();
        }
    }
}