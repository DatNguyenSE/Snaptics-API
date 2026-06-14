using AutoMapper;
using BLL.Dtos;
using BLL.Interfaces.IServices;
using DAL.IRepositories;
using System;
using System.Collections.Generic;
using System.Text;
using DAL.Enums;

namespace BLL.Service
{
    public class ItemInventoryService(IUnitOfWork _uow, IMapper mapper ) : IItemInventoryService
    {
        public async Task<IEnumerable<ItemInventoryDto>> GetAllAsync()
        {
            var itemInventories = await _uow.ItemInventoryRepository.GetAllAsync();
            return mapper.Map<IEnumerable<ItemInventoryDto>>(itemInventories);
        }

        public async Task<ItemInventoryDto> GetByIdAsync(int id)
        {
            var itemInventory = await _uow.ItemInventoryRepository.GetByIdAsync(id);
            return mapper.Map<ItemInventoryDto>(itemInventory);
        }

        public async Task<ItemInventoryDto> CreateAsync(ItemInventoryDto itemInventoryDto)
        {
            var entity = mapper.Map<DAL.Entities.ItemInventory>(itemInventoryDto);
            await _uow.ItemInventoryRepository.AddAsync(entity);
            await _uow.Complete();
            return mapper.Map<ItemInventoryDto>(entity);
        }

        public async Task<ItemInventoryDto> UpdateAsync(int itemInventoryId, ItemInventoryDto itemInventoryDto)
        {
            var existingEntity = await _uow.ItemInventoryRepository.GetByIdAsync(itemInventoryId);
            if (existingEntity == null)
            {
                throw new KeyNotFoundException("Item inventory not found");
            }
            mapper.Map(itemInventoryDto, existingEntity);
            _uow.ItemInventoryRepository.Update(existingEntity);
            await _uow.Complete();
            return mapper.Map<ItemInventoryDto>(existingEntity);
        }

        public async Task<ItemInventoryDto> DeleteAsync(int itemInventoryId)
        {
            var existingEntity = await _uow.ItemInventoryRepository.GetByIdAsync(itemInventoryId);
            if (existingEntity == null)
            {
                throw new KeyNotFoundException("Item inventory not found");
            }
            _uow.ItemInventoryRepository.Delete(existingEntity);
            await _uow.Complete();
            return mapper.Map<ItemInventoryDto>(existingEntity);
        }

        public async Task<IEnumerable<ItemInventoryDto>> GetItemsNeedReviewAsync(int days = 30)
        {
            var thresholdDate = DateTime.UtcNow.AddDays(-days);
            var items = await _uow.ItemInventoryRepository.FindAsync(x => !x.IsReviewed && x.CreatedAt <= thresholdDate);
            return mapper.Map<IEnumerable<ItemInventoryDto>>(items);
        }

        public async Task<ItemInventoryDto>ReviewItemAsync(int itemInventoryId, UsageStatusType usageStatus)
        {
            var item = await _uow.ItemInventoryRepository.GetByIdAsync(itemInventoryId);
            if (item == null)
            {
                throw new KeyNotFoundException("Item inventory not found");
            }
            item.UsageStatus = usageStatus;
            item.IsReviewed = true;
            item.LastReviewDate = DateTime.UtcNow;

            _uow.ItemInventoryRepository.Update(item);
            await _uow.Complete();
            return mapper.Map<ItemInventoryDto>(item);
        }
    }
}
