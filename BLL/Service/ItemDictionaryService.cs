using AutoMapper;
using BLL.Dtos;
using BLL.Interfaces.IServices;
using DAL.IRepositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BLL.Service
{
    public class ItemDictionaryService(IUnitOfWork _uow, IMapper _mapper) : IItemDictionaryService
    {
        public async Task<IEnumerable<ItemDictionaryDto>> GetAllAsync()
        {
            var items = await _uow.ItemDictionaryRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<ItemDictionaryDto>>(items);
        }

        public async Task<ItemDictionaryDto> GetByIdAsync(int id)
        {
            var item = await _uow.ItemDictionaryRepository.GetByIdAsync(id);
            return _mapper.Map<ItemDictionaryDto>(item);
        }

        public async Task<ItemDictionaryDto> CreateAsync(ItemDictionaryDto itemDictionaryDto)
        {
            if (string.IsNullOrWhiteSpace(itemDictionaryDto.Keyword))
                throw new ArgumentException("Keyword is required");

            itemDictionaryDto.NormalizedKeyword = itemDictionaryDto.Keyword.Trim().ToLower();

            var isDuplicate = await _uow.ItemDictionaryRepository.AnyAsync(i => i.NormalizedKeyword == itemDictionaryDto.NormalizedKeyword);
            if (isDuplicate) throw new InvalidOperationException("Keyword already exists in dictionary");

            var entity = _mapper.Map<DAL.Entities.ItemDictionary>(itemDictionaryDto);
            await _uow.ItemDictionaryRepository.AddAsync(entity);
            await _uow.Complete();
            return _mapper.Map<ItemDictionaryDto>(entity);
        }

        public async Task<ItemDictionaryDto> UpdateAsync(int id, ItemDictionaryDto itemDictionaryDto)
        {
            if (string.IsNullOrWhiteSpace(itemDictionaryDto.Keyword))
                throw new ArgumentException("Keyword is required");

            itemDictionaryDto.NormalizedKeyword = itemDictionaryDto.Keyword.Trim().ToLower();

            var existingEntity = await _uow.ItemDictionaryRepository.GetByIdAsync(id);
            if (existingEntity == null)
            {
                throw new KeyNotFoundException("ItemDictionary not found");
            }

            if (existingEntity.NormalizedKeyword != itemDictionaryDto.NormalizedKeyword)
            {
                var isDuplicate = await _uow.ItemDictionaryRepository.AnyAsync(i => i.NormalizedKeyword == itemDictionaryDto.NormalizedKeyword);
                if (isDuplicate) throw new InvalidOperationException("Keyword already exists in dictionary");
            }

            _mapper.Map(itemDictionaryDto, existingEntity);
            _uow.ItemDictionaryRepository.Update(existingEntity);
            await _uow.Complete();
            return _mapper.Map<ItemDictionaryDto>(existingEntity);
        }

        public async Task<ItemDictionaryDto> DeleteAsync(int id)
        {
            var existingEntity = await _uow.ItemDictionaryRepository.GetByIdAsync(id);
            if (existingEntity == null)
            {
                throw new KeyNotFoundException("ItemDictionary not found");
            }
            _uow.ItemDictionaryRepository.Delete(existingEntity);
            await _uow.Complete();
            return _mapper.Map<ItemDictionaryDto>(existingEntity);
        }

        public async Task<int> CleanupAsync(int maxHitCount, int olderThanDays)
        {
            var thresholdDate = DateTime.UtcNow.AddDays(-olderThanDays);
            var itemsToDelete = await _uow.ItemDictionaryRepository.FindAsync(d => d.HitCount <= maxHitCount && d.CreatedAt <= thresholdDate);
            var itemsList = itemsToDelete.ToList();
            var count = 0;
            foreach (var item in itemsList)
            {
                _uow.ItemDictionaryRepository.Delete(item);
                count++;
            }
            if (count > 0)
            {
                await _uow.Complete();
            }
            return count;
        }
    }
}
