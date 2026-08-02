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
            CreateMap<ItemInventory, ItemInventoryDto>()
                .ForMember(dest => dest.ItemName, opt => opt.MapFrom(src => src.TransactionDetail != null ? src.TransactionDetail.ItemName : null))
                .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.TransactionDetail != null ? src.TransactionDetail.Price * src.TransactionDetail.Quantity : (decimal?)null))
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => (src.TransactionDetail != null && src.TransactionDetail.Category != null) ? src.TransactionDetail.Category.Name : null))
                .ForMember(dest => dest.PurchaseDate, opt => opt.MapFrom(src => (src.TransactionDetail != null && src.TransactionDetail.Transaction != null) ? src.TransactionDetail.Transaction.TransactionDate : (DateTime?)null))
                .ReverseMap();
            CreateMap<Budget, BudgetDto>().ReverseMap();
            CreateMap<Notification, NotificationDto>().ReverseMap();
            CreateMap<ItemDictionary, ItemDictionaryDto>().ReverseMap();
            CreateMap<IncomeSource, IncomeSourceDto>().ReverseMap();
            CreateMap<BudgetIncomeSource, BudgetIncomeSourceDto>().ReverseMap();
            CreateMap<DAL.Entities.SupportTicket, BLL.Dtos.Support.SupportTicketDto>().ReverseMap();
            CreateMap<DAL.Entities.SupportMessage, BLL.Dtos.Support.SupportMessageDto>().ReverseMap();
            CreateMap<DAL.Entities.SupportAttachment, BLL.Dtos.Support.SupportAttachmentDto>().ReverseMap();
        }
    }
}