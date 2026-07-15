using BLL.Interfaces.IServices;
using System;
using System.Collections.Generic;
using System.Text;
using DAL.IRepositories;

namespace BLL.Service
{
    public class IncomeJobService(IUnitOfWork _uow) : IIncomeJobService
    {
        public async Task ProcessRecurringIncomeAsync()
        {
            if (DateTime.UtcNow.Day != 1)
            {
                return;
            }

            var incomes = await _uow.IncomeSourceRepository.GetAllAsync();

            foreach (var income in incomes)
            {
                if (!income.IsRecurring)
                    continue;

                if (income.Type != DAL.Enums.IncomeType.Salary)
                    continue;

                var alreadyReceived =
                    await _uow.IncomeHistoryRepository
                        .HasReceivedThisMonthAsync(income.Id);

                if (alreadyReceived)
                    continue;

                var budget =
                    await _uow.BudgetRepository
                        .GetByIdAsync(income.BudgetId);

                if (budget == null)
                    continue;

                budget.CurrentAmount += income.Amount;

                _uow.BudgetRepository.Update(budget);

                await _uow.IncomeHistoryRepository.AddAsync(
                    new DAL.Entities.IncomeHistory
                    {
                        IncomeSourceId = income.Id,
                        Amount = income.Amount,
                        ReceivedDate = DateTime.UtcNow,
                        Note = "Auto monthly salary"
                    });

                await _uow.Complete();
            }
        }
    }
}
