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
        IMapper mapper,
        IItemDictionaryService _itemDictionaryService
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

        //hàm này sẽ tạo Transaction + TransactionDetails + Category (nếu chưa có) + ItemInventory (nếu category được tracking)
        public async Task<TransactionDto> CreateWithDetailsAsync(CreateTransactionWithDetailsDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto), "Payload (Hóa đơn) không được để trống.");
            if (dto.Items == null) dto.Items = new List<CreateTransactionDetailItemDto>();

            // 1. Gom tất cả CategoryName thành mảng duy nhất, loại bỏ null/empty và giá trị "string"
            var categoryNames = dto.Items
                .Where(i => i != null)
                .Select(i => i.Category)
                .Where(c => !string.IsNullOrWhiteSpace(c) && !c.Trim().Equals("string", StringComparison.OrdinalIgnoreCase))
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
                ImageKey = dto.ImageKey,
                TotalAmount = dto.TotalAmount,
                TransactionDate = dto.TransactionDate,
                Note = dto.Note,
                Status = DAL.Enums.TransactionStatusType.Completed, // Mặc định
                IsAiEstimated = true,
                CreatedAt = DateTime.UtcNow,
                TransactionDetails = new List<TransactionDetail>()
            };

            // 6. Map Details sử dụng Dictionary O(1) để lấy CategoryId
            // Reject any item that does not have a valid category


            foreach (var item in dto.Items)
            {
                if (item == null) continue;

                int categoryId = 0;
                bool isValidCategory = !string.IsNullOrWhiteSpace(item.Category) && !item.Category.Trim().Equals("string", StringComparison.OrdinalIgnoreCase);

                if (isValidCategory && categoryDict.TryGetValue(item.Category, out var id))
                {
                    categoryId = id;
                }
                else
                {
                    throw new ArgumentException($"Sản phẩm '{item.ItemName}' có danh mục không hợp lệ hoặc chưa được phân loại. Vui lòng phân loại tất cả các sản phẩm.");
                }

                transaction.TransactionDetails.Add(new TransactionDetail
                {
                    ItemName = item.ItemName,
                    Price = item.Price,
                    Quantity = item.Quantity,
                    CategoryId = categoryId,
                    EstimatedCalories = item.EstimatedCalories,
                    Unit = item.Unit
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
            var calculatedTotal = billDto.Items.Sum(i => i.Price * i.Quantity);
            var finalTotal = calculatedTotal > 0 ? calculatedTotal : billDto.TotalAmount;

            //dto(Trans + TransDetail) -> record(Trans + TransDetail + Category(if new) + ItemIventory(if Tracking)
            var dto = new CreateTransactionWithDetailsDto
            {
                UserId = userId,
                MerchantName = billDto.MerchantName ?? "Hóa đơn siêu thị",
                ImageKey = billDto.BillImageKey,
                TransactionDate = billDto.TransactionDate ?? DateTime.UtcNow,
                TotalAmount = finalTotal,
                // insert transation-details from parameter billDto.Items
                Items = billDto.Items.Select(i => new CreateTransactionDetailItemDto
                {
                    ItemName = i.ItemName,
                    Price = i.Price,
                    Quantity = i.Quantity,
                    Category = i.Category,
                    Unit = i.Unit
                }).ToList()
            };
            // Gọi hàm CreateWithDetailsAsync để tạo Transaction + Details + Category + ItemInventory
            var transactionDto = await CreateWithDetailsAsync(dto);
            
            // read-bill:  IsAiEstimated = false
            // kiểm tra xem đã tạo transaction từ hàm CreateWithDetailsAsync chưa 
            var transaction = await _uow.TransactionRepository.GetByIdAsync(transactionDto.Id);
            if (transaction != null)
            {
                transaction.IsAiEstimated = false;
                _uow.TransactionRepository.Update(transaction);
                await _uow.Complete();
                transactionDto.IsAiEstimated = false;
            }

            // Gọi hàm học hỏi từ user feedback
            try
            {
                await _itemDictionaryService.LearnFromUserFeedbackAsync(billDto.Items);
            }
            catch
            {
                // Không block luồng chính lưu hóa đơn
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
                MerchantName = imageDto.ItemName, 
                ImageKey = imageDto.ImageKey,
                TransactionDate = DateTime.UtcNow,
                TotalAmount = imageDto.EstimatedPriceVND,
                Items = new List<CreateTransactionDetailItemDto>
                {
                    new CreateTransactionDetailItemDto
                    {
                        ItemName = imageDto.ItemName,
                        Price = imageDto.EstimatedPriceVND,
                        Quantity = imageDto.Quantity,
                        Category = imageDto.Category,
                        EstimatedCalories = imageDto.EstimatedCalories,
                        Unit = string.IsNullOrWhiteSpace(imageDto.Unit) ? "cái" : imageDto.Unit
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

            // Gọi hàm học hỏi từ user feedback
            try
            {
                var feedbackItems = new List<BLL.Dtos.AiDto.BillItemDto>
                {
                    new BLL.Dtos.AiDto.BillItemDto
                    {
                        ItemName = imageDto.ItemName,
                        Category = imageDto.Category,
                        Quantity = imageDto.Quantity,
                        Price = imageDto.EstimatedPriceVND
                    }
                };
                await _itemDictionaryService.LearnFromUserFeedbackAsync(feedbackItems);
            }
            catch
            {
                // Không block luồng chính lưu hóa đơn
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

        public async Task<IEnumerable<TransactionDto>> GetUnconfirmedTransactionsByDateAsync(DateTime date)
        {
            var transactions = await _uow.TransactionRepository.GetAllAsync();
            var unconfirmed = transactions
                .Where(t => t.IsAiEstimated == false
                            && t.CreatedAt >= date.Date
                            && !t.IsDeleted)
                .ToList();
            return mapper.Map<IEnumerable<TransactionDto>>(unconfirmed);
        }

        public async Task<IEnumerable<TransactionDto>> GetByUserIdAsync(string userId)
        {
            // 1. Gọi Repo lấy data từ DB
            var transactions = await _uow.TransactionRepository.GetByUserIdAsync(userId);
    
            // 2. Map sang DTO trả về
            return mapper.Map<IEnumerable<TransactionDto>>(transactions);
        }

    }
}