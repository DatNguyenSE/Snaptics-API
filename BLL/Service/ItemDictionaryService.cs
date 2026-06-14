using AutoMapper;
using BLL.Dtos;
using BLL.Interfaces.IServices;
using DAL.IRepositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using BLL.Helpers;
using DAL.Entities;

namespace BLL.Service
{
    public class ItemDictionaryService(IUnitOfWork _uow, IMapper _mapper, IMemoryCache _cache) : IItemDictionaryService
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

        private const string DictionaryCacheKey = "ItemDictionary_All";

        public async Task LearnFromUserFeedbackAsync(List<BLL.Dtos.AiDto.BillItemDto> confirmedItems)
        {
            if (confirmedItems == null || !confirmedItems.Any()) return;

            // Load từ điển hiện tại từ database
            var dictionary = await _uow.ItemDictionaryRepository.GetAllDictionaryAsync();
            var orderedDictionary = dictionary.OrderByDescending(d => d.NormalizedKeyword.Length).ToList();
            var isDbChanged = false;
            var newEntries = new List<ItemDictionary>();

            foreach (var item in confirmedItems)
            {
                var normalizedName = TextNormalizationHelper.NormalizeItemName(item.ItemName);
                if (string.IsNullOrWhiteSpace(normalizedName)) continue;

                ItemDictionary? matchedEntry = null;

                // TIER 1: So khớp nguyên từ (Word Boundary)
                foreach (var dictItem in orderedDictionary)
                {
                    if (string.IsNullOrWhiteSpace(dictItem.NormalizedKeyword)) continue;

                    string pattern = @"\b" + System.Text.RegularExpressions.Regex.Escape(dictItem.NormalizedKeyword) + @"\b";
                    if (System.Text.RegularExpressions.Regex.IsMatch(normalizedName, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                    {
                        matchedEntry = dictItem;
                        break;
                    }
                }

                // TIER 2: So khớp mờ (Fuzzy Match) sử dụng Levenshtein nếu Tier 1 thất bại
                if (matchedEntry == null)
                {
                    double maxSimilarity = 0.0;
                    foreach (var dictItem in orderedDictionary)
                    {
                        if (string.IsNullOrWhiteSpace(dictItem.NormalizedKeyword)) continue;

                        double similarity = TextNormalizationHelper.CalculateSimilarity(normalizedName, dictItem.NormalizedKeyword);

                        // Ngưỡng chấp nhận (ví dụ >= 80% tương đồng)
                        if (similarity >= 0.8 && similarity > maxSimilarity)
                        {
                            maxSimilarity = similarity;
                            matchedEntry = dictItem;
                        }
                    }
                }

                // Cập nhật hoặc thêm mới dựa trên kết quả so khớp
                if (matchedEntry != null)
                {
                    // Nếu Category trong DB khác với Category được User xác nhận
                    if (matchedEntry.Category != item.Category)
                    {
                        matchedEntry.Category = item.Category ?? "Unknown";
                        matchedEntry.HitCount++;
                        _uow.ItemDictionaryRepository.Update(matchedEntry);
                        isDbChanged = true;
                    }
                    else
                    {
                        matchedEntry.HitCount++;
                        _uow.ItemDictionaryRepository.Update(matchedEntry);
                        isDbChanged = true;
                    }
                }
                else
                {
                    // Nếu đây là món hoàn toàn mới và category không phải là Unknown
                    if (item.Category != "Unknown" && !string.IsNullOrWhiteSpace(item.Category))
                    {
                        var newEntry = new ItemDictionary
                        {
                            Keyword = item.ItemName,
                            NormalizedKeyword = normalizedName,
                            Category = item.Category,
                            HitCount = 1,
                            CreatedAt = DateTime.UtcNow
                        };
                        newEntries.Add(newEntry);
                        isDbChanged = true;
                    }
                }
            }

            // Lưu thay đổi vào DB và xóa cache
            if (newEntries.Any())
            {
                await _uow.ItemDictionaryRepository.AddRangeAsync(newEntries);
            }

            if (isDbChanged)
            {
                await _uow.Complete();
                _cache.Remove(DictionaryCacheKey); // Invalidate Cache
            }
        }
    }
}
