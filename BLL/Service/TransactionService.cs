using AutoMapper;
using BLL.Dtos;
using BLL.Interfaces.IServices;
using DAL.Entities;
using DAL.IRepositories;
using DAL.Enums;

namespace BLL.Service
{
    public class TransactionService(
        IUnitOfWork _uow,
        IMapper mapper
    ) : ITransactionService
    {
        public async Task<IEnumerable<TransactionDto>
        > GetAllAsync()
        {
            var transactions = await _uow.TransactionRepository.GetAllAsync();
            return mapper.Map<IEnumerable<TransactionDto>>(transactions);
        }

        public async Task<TransactionDto> GetByIdAsync(int transactionId)
        {
            var transaction = await _uow.TransactionRepository.GetByIdAsync(transactionId);
            return mapper.Map<TransactionDto>(transaction);
        }

        public async Task<TransactionDto> CreateAsync(TransactionDto transactionDto)
        {
            var entity = mapper.Map<DAL.Entities.Transaction>(transactionDto);
            if (entity == null)
            {
                throw new Exception("Failed to create transaction");
            }
            await _uow.TransactionRepository.AddAsync(entity);
            await _uow.Complete();
            return mapper.Map<TransactionDto>(entity);
        }

        public async Task<TransactionDto> CreateWithDetailsAsync(CreateTransactionWithDetailsDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto), "Payload (Hóa đơn) không được để trống.");
            if (dto.Items == null) dto.Items = new List<CreateTransactionDetailItemDto>();

            // 1. Gom tất cả CategoryName thành mảng duy nhất, loại bỏ null/empty
            var categoryNames = dto.Items
                .Where(i => i != null)
                .Select(i => i.Category)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c!)
                .Distinct()
                .ToList();

            var categoryDict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            if (categoryNames.Any())
            {
                // 2. Lấy các category đã có từ DB (chỉ query 1 lần)
                var existingCategories = await _uow.CategoryRepository
                    .FindAsync(c => categoryNames.Contains(c.Name!));
                
                foreach (var cat in existingCategories)
                {
                    categoryDict[cat.Name!] = cat.Id;
                }

                // 3. Tìm các category chưa có
                var existingNames = existingCategories.Select(c => c.Name).ToList();
                var missingNames = categoryNames.Except(existingNames, StringComparer.OrdinalIgnoreCase).ToList();

                // 4. Batch Insert các category mới
                if (missingNames.Any())
                {
                    var newCategories = missingNames.Select(name => new Category { Name = name }).ToList();
                    await _uow.CategoryRepository.AddRangeAsync(newCategories);
                    await _uow.Complete(); // Lưu để EF Core sinh ra ID cho các newCategories

                    // Nạp thêm ID mới vào Dictionary
                    foreach (var cat in newCategories)
                    {
                        categoryDict[cat.Name!] = cat.Id;
                    }
                }
            }

            // 5. Tạo entity Transaction
            var transaction = new DAL.Entities.Transaction
            {
                Name = dto.MerchantName ?? "Phân tích từ AI",
                UserId = dto.UserId,
                TotalAmount = dto.TotalAmount,
                TransactionDate = dto.TransactionDate,
                Note = dto.Note,
                Status = DAL.Enums.TransactionStatusType.Completed, // Mặc định
                IsAiEstimated = true,
                CreatedAt = DateTime.UtcNow,
                TransactionDetails = new List<TransactionDetail>()
            };

            // 6. Map Details sử dụng Dictionary O(1) để lấy CategoryId
            // Nếu item nào không có category, có thể gán ID của category "Unknown" 
            // Để an toàn, giả sử ta gán ID cho category mặc định hoặc lấy từ dictionary
            int? defaultUnknownId = categoryDict.ContainsKey("Unknown") ? categoryDict["Unknown"] : null;

            foreach (var item in dto.Items)
            {
                if (item == null) continue;

                int categoryId = 0;
                if (!string.IsNullOrWhiteSpace(item.Category) && categoryDict.TryGetValue(item.Category, out var id))
                {
                    categoryId = id;
                }
                else if (defaultUnknownId.HasValue)
                {
                    categoryId = defaultUnknownId.Value;
                }
                else
                {
                    // Fallback cực đoan: tạo Unknown nếu chưa có
                    var unknownCat = new Category { Name = "Unknown" };
                    await _uow.CategoryRepository.AddAsync(unknownCat);
                    await _uow.Complete();
                    defaultUnknownId = unknownCat.Id;
                    categoryId = unknownCat.Id;
                }

                transaction.TransactionDetails.Add(new TransactionDetail
                {
                    ItemName = item.ItemName,
                    Price = item.Price,
                    Quantity = item.Quantity,
                    CategoryId = categoryId
                });
            }

            // 7. Lưu Transaction + TransactionDetails
            await _uow.TransactionRepository.AddAsync(transaction);
            await _uow.Complete();


            // 8. Auto create ItemInventory cho category được tracking
            foreach (var detail in transaction.TransactionDetails)
            {
                var category = await _uow.CategoryRepository
                    .GetByIdAsync(detail.CategoryId);

                if (category != null && category.IsTrackableInventory)
                {
                    var inventory = new ItemInventory
                    {
                        UserId = transaction.UserId,
                        TransactionDetailId = detail.Id,
                        UsageStatus = UsageStatusType.Frequent,
                        IsReviewed = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _uow.ItemInventoryRepository
                        .AddAsync(inventory);
                }
            }

            // Save ItemInventory
            await _uow.Complete();

            return mapper.Map<TransactionDto>(transaction);
        }

        public async Task<TransactionDto> CreateFromBillAsync(string userId, BLL.Dtos.AiDto.BillReadResultDto billDto)
        {
            if (billDto == null) throw new ArgumentNullException(nameof(billDto));

            // Map BillReadResultDto sang CreateTransactionWithDetailsDto
            var dto = new CreateTransactionWithDetailsDto
            {
                UserId = userId,
                MerchantName = billDto.MerchantName ?? "Hóa đơn siêu thị",
                TransactionDate = billDto.TransactionDate ?? DateTime.UtcNow,
                TotalAmount = billDto.TotalAmount,
                Items = billDto.Items.Select(i => new CreateTransactionDetailItemDto
                {
                    ItemName = i.ItemName,
                    Price = i.Price,
                    Quantity = i.Quantity,
                    Category = i.Category
                }).ToList()
            };

            var transactionDto = await CreateWithDetailsAsync(dto);
            
            // Theo yêu cầu: read-bill thì IsAiEstimated = false
            var transaction = await _uow.TransactionRepository.GetByIdAsync(transactionDto.Id);
            if (transaction != null)
            {
                transaction.IsAiEstimated = false;
                _uow.TransactionRepository.Update(transaction);
                await _uow.Complete();
                transactionDto.IsAiEstimated = false;
            }

            return transactionDto;
        }

        public async Task<TransactionDto> CreateFromImageAnalyzeAsync(string userId, BLL.Dtos.AiDto.AnalyzeImageResponseDto imageDto)
        {
            if (imageDto == null) throw new ArgumentNullException(nameof(imageDto));

            // Map AnalyzeImageResponseDto sang CreateTransactionWithDetailsDto
            var dto = new CreateTransactionWithDetailsDto
            {
                UserId = userId,
                MerchantName = imageDto.ItemName, // Tên Transaction = itemName
                TransactionDate = DateTime.UtcNow,
                TotalAmount = imageDto.EstimatedPriceVND,
                Items = new List<CreateTransactionDetailItemDto>
                {
                    new CreateTransactionDetailItemDto
                    {
                        ItemName = imageDto.ItemName,
                        Price = imageDto.EstimatedPriceVND,
                        Quantity = imageDto.Quantity,
                        Category = imageDto.Category
                    }
                }
            };

            var transactionDto = await CreateWithDetailsAsync(dto);
            
            // Theo yêu cầu: analyze thì IsAiEstimated = true
            var transaction = await _uow.TransactionRepository.GetByIdAsync(transactionDto.Id);
            if (transaction != null)
            {
                transaction.IsAiEstimated = true;
                _uow.TransactionRepository.Update(transaction);
                await _uow.Complete();
                transactionDto.IsAiEstimated = true;
            }

            return transactionDto;
        }

        public async Task<TransactionDto> UpdateAsync(int transactionId, TransactionDto transactionDto)
        {
            var existingTransaction = await _uow.TransactionRepository.GetByIdAsync(transactionId);
            if (existingTransaction == null)
            {
                throw new Exception("Transaction not found");
            }

            //update data
            mapper.Map(transactionDto, existingTransaction);
            _uow.TransactionRepository.Update(existingTransaction);
            await _uow.Complete();
            return mapper.Map<TransactionDto>(existingTransaction);
        }

        public async Task<TransactionDto> DeleteAsync(int transactionId)
        {
            var transaction = await _uow.TransactionRepository.GetByIdAsync(transactionId);
            if (transaction == null)
            {
                throw new Exception("Transaction not found");
            }
            _uow.TransactionRepository.Delete(transaction);
            await _uow.Complete();
            return mapper.Map<TransactionDto>(transaction);
        }
    }
}