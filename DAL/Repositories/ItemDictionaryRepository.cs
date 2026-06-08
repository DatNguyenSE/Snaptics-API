using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DAL.Data;
using DAL.Entities;
using DAL.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class ItemDictionaryRepository : GenericRepository<ItemDictionary>, IItemDictionaryRepository
    {
        public ItemDictionaryRepository(AppDbContext context) : base(context) { }

        /// <summary>
        /// Chuẩn hóa keyword về lowercase rồi tìm trong DB.
        /// Dùng Contains để hỗ trợ khớp một phần (vd: "sữa chua" khớp "Sữa chua Vinamilk").
        /// </summary>
        public async Task<string?> FindCategoryByKeywordAsync(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return null;

            var normalized = keyword.Trim().ToLowerInvariant();

            // Tìm các entry mà NormalizedKeyword là substring của normalized keyword đầu vào
            // Hoặc ngược lại: normalized keyword đầu vào chứa NormalizedKeyword đã lưu
            var match = await _dbSet
                .AsNoTracking()
                .Where(d => normalized.Contains(d.NormalizedKeyword) || d.NormalizedKeyword.Contains(normalized))
                .OrderByDescending(d => d.NormalizedKeyword.Length) // Ưu tiên match dài hơn (chính xác hơn)
                .FirstOrDefaultAsync();

            return match?.Category;
        }

        /// <summary>
        /// Batch insert nhiều item mới vào từ điển cùng lúc.
        /// </summary>
        public async Task AddRangeAsync(IEnumerable<ItemDictionary> items)
        {
            await _dbSet.AddRangeAsync(items);
        }

        /// <summary>
        /// Lấy toàn bộ từ điển để load vào MemoryCache (dùng AsNoTracking cho hiệu năng).
        /// </summary>
        public async Task<List<ItemDictionary>> GetAllDictionaryAsync()
        {
            return await _dbSet.AsNoTracking().ToListAsync();
        }
    }
}
