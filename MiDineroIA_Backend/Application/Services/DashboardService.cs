using MiDineroIA_Backend.Application.DTOs;
using MiDineroIA_Backend.Application.Interfaces;
using MiDineroIA_Backend.Domain.Entities.Views;
using MiDineroIA_Backend.Domain.Interfaces;

namespace MiDineroIA_Backend.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IDashboardRepository _dashboardRepository;

    public DashboardService(IDashboardRepository dashboardRepository)
    {
        _dashboardRepository = dashboardRepository;
    }



    // ESTE METODO ES EL CORAZ�N DEL SERVICIO, AQU� SE OBTIENEN LOS DATOS DE LA BASE DE DATOS Y SE TRANSFORMAN EN LOS DTOs QUE EL FRONTEND NECESITA PARA MOSTRAR EL DASHBOARD
    public async Task<DashboardResponseDto> GetDashboardAsync(int userId, int year, int month)
    {
        var summaryData = await _dashboardRepository.GetMonthlySummariesAsync(userId, year, month);
        var categoryDetails = await _dashboardRepository.GetCategoryDetailsAsync(userId, year, month);
        var distributionData = await _dashboardRepository.GetExpenseDistributionsAsync(userId, year, month);

        return new DashboardResponseDto
        {
            Summary = BuildSummary(summaryData),
            IncomeDetail = BuildIncomeDetail(categoryDetails),
            ExpenseGroups = BuildExpenseGroups(categoryDetails),
            ExpenseDistribution = BuildExpenseDistribution(distributionData)
        };
    }


    // M�todos privados para transformar los datos crudos de la base de datos en los DTOs que el frontend necesita
    private static SummaryDto BuildSummary(List<MonthlySummaryView> summaryData)
    {
        if (summaryData.Count == 0)
        {
            return new SummaryDto
            {
                TotalIncome = 0,
                TotalExpenses = 0,
                Balance = 0
            };
        }

        var totalIncome = summaryData
            .Where(s => s.TransactionType == "INGRESO")
            .Sum(s => s.TotalReal);

        var totalExpenses = summaryData
            .Where(s => s.TransactionType == "EGRESO")
            .Sum(s => s.TotalReal);

        return new SummaryDto
        {
            TotalIncome = totalIncome,
            TotalExpenses = totalExpenses,
            Balance = totalIncome - totalExpenses
        };
    }


    // Construye la lista de detalles de ingresos, filtrando solo las categor�as que son ingresos
    private static List<CategoryBudgetRealDto> BuildIncomeDetail(List<CategoryDetailView> details)
    {
        return details
            .Where(d => d.TransactionType == "INGRESO")
            .Select(d => new CategoryBudgetRealDto
            {
                CategoryId = d.CategoryId,
                Category = d.CategoryName,
                Budget = d.BudgetAmount,
                Real = d.RealAmount
            })
            .ToList();
    }


    // Agrupa los detalles de gastos por grupo, creando una estructura que el frontend puede usar para mostrar los grupos de gastos y sus categor�as
    private static List<ExpenseGroupDto> BuildExpenseGroups(List<CategoryDetailView> details)
    {
        return details
            .Where(d => d.TransactionType == "EGRESO")
            .GroupBy(d => d.GroupName)
            .Select(g => new ExpenseGroupDto
            {
                GroupName = g.Key,
                Categories = g.Select(c => new CategoryBudgetRealDto
                {
                    CategoryId = c.CategoryId,
                    Category = c.CategoryName,
                    Budget = c.BudgetAmount,
                    Real = c.RealAmount
                }).ToList()
            })
            .ToList();
    }


    // Construye la lista de distribuci�n de gastos, transformando los datos crudos en el formato que el frontend necesita para mostrar la distribuci�n de gastos por grupo
    private static List<ExpenseDistributionDto> BuildExpenseDistribution(List<ExpenseDistributionView> data)
    {
        return data.Select(e => new ExpenseDistributionDto
        {
            Group = e.GroupName,
            Total = e.TotalAmount,
            Percentage = e.Percentage
        }).ToList();
    }

}
