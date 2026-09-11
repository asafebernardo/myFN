namespace MyFn.Domain.Entities;

public class AppSetting
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public decimal DefaultSalary { get; set; }
    public string CurrencyCode { get; set; } = "BRL";
    public int FinancialMonthStartDay { get; set; } = 1;
    public decimal HealthyCommitmentMaxPercent { get; set; } = 70m;
    public decimal WarningCommitmentMaxPercent { get; set; } = 90m;
    public bool ShowDashboardCharts { get; set; } = true;

    public User? User { get; set; }
}
