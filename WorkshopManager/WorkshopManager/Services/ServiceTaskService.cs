using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkshopManager.Data;
using WorkshopManager.DTOs;
using WorkshopManager.Mappers;
using WorkshopManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WorkshopManager.Services
{
    public class ServiceTaskService : IServiceTaskService
    {
        private readonly ApplicationDbContext _context;
        private readonly ServiceTaskMapper _mapper;
        private readonly ILogger<ServiceTaskService> _logger;

        public ServiceTaskService(ApplicationDbContext context, ILogger<ServiceTaskService> logger)
        {
            _context = context;
            _mapper = new ServiceTaskMapper();
            _logger = logger;
        }

        public async Task<List<ServiceTaskDto>> GetTasksByOrderIdAsync(int orderId)
        {
            try
            {
                _logger.LogInformation("Rozpoczęto pobieranie zadań dla zlecenia ID: {OrderId}", orderId);

                var tasks = await _context.ServiceTasks
                    .Where(t => t.ServiceOrderId == orderId)
                    .Include(t => t.ServiceOrder)
                    .ToListAsync();

                var result = tasks.Select(t => _mapper.ToDto(t)).ToList();

                _logger.LogInformation("Pobrano {Count} zadań dla zlecenia ID: {OrderId}", result.Count, orderId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania zadań dla zlecenia ID: {OrderId}", orderId);
                throw;
            }
        }

        public async Task<ServiceTaskDto> GetTaskByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Rozpoczęto pobieranie zadania ID: {TaskId}", id);

                var task = await _context.ServiceTasks
                    .Include(t => t.ServiceOrder)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (task == null)
                {
                    _logger.LogWarning("Nie znaleziono zadania o ID: {TaskId}", id);
                    throw new InvalidOperationException($"Zadanie o ID {id} nie zostało znalezione");
                }

                var result = _mapper.ToDto(task);
                _logger.LogInformation("Pomyślnie pobrano zadanie ID: {TaskId}, Opis: '{Description}', Zlecenie: {OrderId}",
                    id, task.Description, task.ServiceOrderId);

                return result;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nieoczekiwany błąd podczas pobierania zadania ID: {TaskId}", id);
                throw;
            }
        }

        public async Task<ServiceTaskDto> CreateTaskAsync(ServiceTaskCreateDto taskDto)
        {
            try
            {
                _logger.LogInformation("Rozpoczęto tworzenie nowego zadania dla zlecenia ID: {OrderId}, Opis: '{Description}', Koszt: {LaborCost:C}",
                    taskDto.ServiceOrderId, taskDto.Description, taskDto.LaborCost);

                // Sprawdzenie czy zlecenie istnieje
                var orderExists = await _context.ServiceOrders.AnyAsync(o => o.Id == taskDto.ServiceOrderId);
                if (!orderExists)
                {
                    _logger.LogWarning("Próba utworzenia zadania dla nieistniejącego zlecenia ID: {OrderId}", taskDto.ServiceOrderId);
                    throw new InvalidOperationException($"Zlecenie o ID {taskDto.ServiceOrderId} nie istnieje");
                }

                var task = _mapper.FromDto(taskDto);
                _context.ServiceTasks.Add(task);
                await _context.SaveChangesAsync();

                var result = _mapper.ToDto(task);
                _logger.LogInformation("Pomyślnie utworzono zadanie ID: {TaskId} dla zlecenia ID: {OrderId}, Koszt: {LaborCost:C}",
                    task.Id, taskDto.ServiceOrderId, taskDto.LaborCost);

                return result;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Błąd bazy danych podczas tworzenia zadania dla zlecenia ID: {OrderId}", taskDto.ServiceOrderId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nieoczekiwany błąd podczas tworzenia zadania dla zlecenia ID: {OrderId}", taskDto.ServiceOrderId);
                throw;
            }
        }

        public async Task<ServiceTaskDto> UpdateTaskAsync(int id, ServiceTaskUpdateDto taskDto)
        {
            try
            {
                _logger.LogInformation("Rozpoczęto aktualizację zadania ID: {TaskId}", id);

                var existingTask = await _context.ServiceTasks.FindAsync(id);
                if (existingTask == null)
                {
                    _logger.LogWarning("Nie znaleziono zadania o ID: {TaskId} do aktualizacji", id);
                    throw new InvalidOperationException($"Zadanie o ID {id} nie zostało znalezione");
                }

                var oldDescription = existingTask.Description;
                var oldLaborCost = existingTask.LaborCost;

                _mapper.UpdateEntity(taskDto, existingTask);
                await _context.SaveChangesAsync();

                var result = _mapper.ToDto(existingTask);
                _logger.LogInformation("Pomyślnie zaktualizowano zadanie ID: {TaskId}. " +
                    "Opis: '{OldDescription}' -> '{NewDescription}', " +
                    "Koszt: {OldCost:C} -> {NewCost:C}",
                    id, oldDescription, existingTask.Description, oldLaborCost, existingTask.LaborCost);

                return result;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Błąd bazy danych podczas aktualizacji zadania ID: {TaskId}", id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nieoczekiwany błąd podczas aktualizacji zadania ID: {TaskId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteTaskAsync(int id)
        {
            try
            {
                _logger.LogInformation("Rozpoczęto usuwanie zadania ID: {TaskId}", id);

                var task = await _context.ServiceTasks.FindAsync(id);
                if (task == null)
                {
                    _logger.LogWarning("Nie znaleziono zadania o ID: {TaskId} do usunięcia", id);
                    return false;
                }

                var taskInfo = $"Opis: '{task.Description}', Zlecenie: {task.ServiceOrderId}, Koszt: {task.LaborCost:C}";
                _context.ServiceTasks.Remove(task);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Pomyślnie usunięto zadanie ID: {TaskId} ({TaskInfo})", id, taskInfo);
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Błąd bazy danych podczas usuwania zadania ID: {TaskId}. " +
                    "Możliwe że zadanie ma powiązane części", id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nieoczekiwany błąd podczas usuwania zadania ID: {TaskId}", id);
                throw;
            }
        }

        public async Task<ServiceTaskDto> MarkTaskAsCompletedAsync(int id)
        {
            try
            {
                _logger.LogInformation("Rozpoczęto oznaczanie zadania ID: {TaskId} jako ukończone", id);

                var task = await _context.ServiceTasks.FindAsync(id);
                if (task == null)
                {
                    _logger.LogWarning("Nie znaleziono zadania o ID: {TaskId} do oznaczenia jako ukończone", id);
                    throw new InvalidOperationException($"Zadanie o ID {id} nie zostało znalezione");
                }

                var oldDescription = task.Description;

                // Sprawdzenie czy zadanie nie jest już oznaczone jako ukończone
                if (task.Description.Contains("(Completed)"))
                {
                    _logger.LogWarning("Zadanie ID: {TaskId} jest już oznaczone jako ukończone", id);
                    return _mapper.ToDto(task);
                }

                task.Description += " (Completed)";
                await _context.SaveChangesAsync();

                var result = _mapper.ToDto(task);
                _logger.LogInformation("✅ Zadanie ID: {TaskId} zostało oznaczone jako UKOŃCZONE. " +
                    "Opis zmieniony z '{OldDescription}' na '{NewDescription}'",
                    id, oldDescription, task.Description);

                return result;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Błąd bazy danych podczas oznaczania zadania ID: {TaskId} jako ukończone", id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nieoczekiwany błąd podczas oznaczania zadania ID: {TaskId} jako ukończone", id);
                throw;
            }
        }
    }
}