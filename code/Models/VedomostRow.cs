
public class VedomostRow
{
    public string TabNumber { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Period { get; set; } = "";
    public decimal Accrued { get; set; }
    public decimal Withheld { get; set; }
    public decimal ToPay { get; set; }
}