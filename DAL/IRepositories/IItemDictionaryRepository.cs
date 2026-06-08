using System.Collections.Generic;
using System.Threading.Tasks;
using DAL.Entities;

namespace DAL.IRepositories
{
    public interface IItemDictionaryRepository : IGenericRepository<ItemDictionary>
    {
        /// <summary>
        /// Tìm category bằng cách so khớp keyword với NormalizedKeyword trong DB.
        /// Trả về null nếu không tìm thấy.
        /// </summary>
        Task<string?> FindCategoryByKeywordAsync(string keyword);

        /// <summary>
        /// Thêm nhiều item mới vào từ điển cùng lúc (batch insert).
        /// </summary>
        Task AddRangeAsync(IEnumerable<ItemDictionary> items);

        /// <summary>
        /// Lấy toàn bộ từ điển để load vào MemoryCache.
        /// </summary>
        Task<List<ItemDictionary>> GetAllDictionaryAsync();
    }
}
