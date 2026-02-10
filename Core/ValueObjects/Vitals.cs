namespace Core.Entities;

public class Vitals
{
    public decimal? Temperature { get; private set; }
    public int? Systolic { get; private set; }
    public int? Diastolic { get; private set; }

    protected Vitals() { } // EF

    public Vitals(decimal? temperature, int? systolic, int? diastolic)
    {
        Temperature = temperature;
        Systolic = systolic;
        Diastolic = diastolic;
    }
}
