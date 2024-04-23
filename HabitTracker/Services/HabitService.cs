﻿using HabitTracker.Data;
using HabitTracker.Data.Entities;
using HabitTracker.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HabitTracker.Services;

public class HabitService(ApplicationDbContext context, AuthenticationStateProvider authenticationStateProvider) : IHabit
{
    private async Task<string?> GetUserIdAsync()
    {
        var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        string? userId = null;

        Console.WriteLine(user.Identity?.IsAuthenticated);
        Console.WriteLine(user.Identity?.Name);

        if (user.Identity is not null && user.Identity.IsAuthenticated)
        {
            userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        return userId;
    }

    public async Task CreateHabitAsync(Habit habit)
    {
        var userId = await GetUserIdAsync();

        if (string.IsNullOrEmpty(userId))
        {
            throw new Exception("User not found");
        }

        habit.UserId = userId;

        await context.Habits.AddAsync(habit);
        await context.SaveChangesAsync();
    }

    public async Task DeleteHabitAsync(Habit habit)
    {
        var userId = await GetUserIdAsync();

        if (string.IsNullOrEmpty(userId))
        {
            throw new Exception("User not found");
        }

        if (habit.UserId != userId)
        {
            throw new Exception("Habit not found");
        }

        var entry = await context.Habits.FindAsync(habit.Id);
        if (entry != null)
        {
            context.Habits.Remove(entry);
            await context.SaveChangesAsync();
        }

        throw new Exception("Habit not found");
    }

    public async Task<Habit> GetHabitByIdAsync(Guid id)
    {
        var userId = await GetUserIdAsync();
        if (string.IsNullOrEmpty(userId)) {
            throw new Exception("User not found");
        }

        return await context.Habits
            .Include(h => h.Color)
            .Include(h => h.Frequencies)
            .Where(h => h.UserId == userId)
            .FirstOrDefaultAsync(h => h.Id == id) ?? throw new Exception("Habit not found");

    }

    public async Task<List<Habit>> GetHabitsAsync()
    {
        var userId = await GetUserIdAsync();

        if (string.IsNullOrEmpty(userId))
        {
            throw new Exception("User not found");
        }

        return await context.Habits
            .Include(h => h.Color)
            .Include(h => h.Frequencies)
            .Where(h => h.UserId == userId)
            .ToListAsync();
    }

    public async Task<List<Habit>> GetUncompletedHabitsAsync()
    {
        var userId = await GetUserIdAsync();

        if (string.IsNullOrEmpty(userId))
        {
            throw new Exception("User not found");
        }

        return await context.Habits
            .Include(h => h.Color)
            .Include(h => h.Frequencies)
            .Where(h => h.UserId == userId)
            .Where(h => !h.IsCompleted)
            .ToListAsync();
    }

    public async Task UpdateHabitAsync(Habit habit)
    {
        var userId = await GetUserIdAsync();
        if (string.IsNullOrEmpty(userId)) {
            throw new Exception("User not found");
        }

        if (habit.UserId != userId) {
            throw new Exception("Habit not found");
        }

        if (!habit.IsCompleted) {
            habit.EndDate = DateTime.Now;
        }

        habit.UserId = userId;

        var entry = context.Habits.Entry(habit);
        if (entry.State == EntityState.Detached)
        {
            context.Habits.Attach(habit);
            entry.State = EntityState.Modified;
        }

        await context.SaveChangesAsync();
    }
}
