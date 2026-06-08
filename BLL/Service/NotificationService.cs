using AutoMapper;
using BLL.Dtos;
using BLL.Interfaces.IServices;
using DAL.IRepositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BLL.Service
{
    public class NotificationService(IUnitOfWork _uow, IMapper _mapper) : INotificationService
    {
        public async Task<IEnumerable<NotificationDto>> GetAllAsync()
        {
            var notifications = await _uow.NotificationRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<NotificationDto>>(notifications);
        }

        public async Task<NotificationDto> GetByIdAsync(int id)
        {
            var notification = await _uow.NotificationRepository.GetByIdAsync(id);
            return _mapper.Map<NotificationDto>(notification);
        }

        public async Task<NotificationDto> CreateAsync(NotificationDto notificationDto)
        {
            var entity = _mapper.Map<DAL.Entities.Notification>(notificationDto);
            await _uow.NotificationRepository.AddAsync(entity);
            await _uow.Complete();
            return _mapper.Map<NotificationDto>(entity);
        }

        public async Task<NotificationDto> UpdateAsync(int id, NotificationDto notificationDto)
        {
            var existingEntity = await _uow.NotificationRepository.GetByIdAsync(id);
            if (existingEntity == null)
            {
                throw new KeyNotFoundException("Notification not found");
            }
            _mapper.Map(notificationDto, existingEntity);
            _uow.NotificationRepository.Update(existingEntity);
            await _uow.Complete();
            return _mapper.Map<NotificationDto>(existingEntity);
        }

        public async Task<NotificationDto> DeleteAsync(int id)
        {
            var existingEntity = await _uow.NotificationRepository.GetByIdAsync(id);
            if (existingEntity == null)
            {
                throw new KeyNotFoundException("Notification not found");
            }
            _uow.NotificationRepository.Delete(existingEntity);
            await _uow.Complete();
            return _mapper.Map<NotificationDto>(existingEntity);
        }
    }
}
