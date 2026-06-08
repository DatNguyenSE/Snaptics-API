using BLL.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BLL.Interfaces.IServices
{
    public interface IItemDictionaryService
    {
        Task<IEnumerable<ItemDictionaryDto>> GetAllAsync();
        Task<ItemDictionaryDto> GetByIdAsync(int id);
        Task<ItemDictionaryDto> CreateAsync(ItemDictionaryDto itemDictionaryDto);
        Task<ItemDictionaryDto> UpdateAsync(int id, ItemDictionaryDto itemDictionaryDto);
        Task<ItemDictionaryDto> DeleteAsync(int id);
    }
}
