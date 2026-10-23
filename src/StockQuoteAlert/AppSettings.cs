namespace StockQuoteAlert;

public class AppSettings
{
    public EmailSettings Email { get; set; } = new();
    public SmtpSettings Smtp { get; set; } = new();
    public BrapiSettings Brapi { get; set; } = new();
    public MonitorSettings Monitor { get; set; } = new();

    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Email.To) || !Email.To.Contains('@'))
        {
            errors.Add("Email:To deve ser um e-mail válido.");
        }

        if (string.IsNullOrWhiteSpace(Smtp.Host))
        {
            errors.Add("Smtp:Host não pode estar vazio.");
        }

        if (Smtp.Port <= 0 || Smtp.Port > 65535)
        {
            errors.Add("Smtp:Port deve estar entre 1 e 65535.");
        }

        if (string.IsNullOrWhiteSpace(Smtp.Username))
        {
            errors.Add("Smtp:Username não pode estar vazio.");
        }

        if (string.IsNullOrWhiteSpace(Smtp.Password))
        {
            errors.Add("Smtp:Password não pode estar vazio.");
        }

        if (Monitor.IntervalSeconds < 5)
        {
            errors.Add("Monitor:IntervalSeconds deve ser de no mínimo 5 segundos.");
        }

        if (Monitor.HysteresisPercent < 0 || Monitor.HysteresisPercent > 10)
        {
            errors.Add("Monitor:HysteresisPercent deve estar entre 0 e 10.");
        }

        return errors;
    }
}

public class EmailSettings
{
    public string To { get; set; } = "";
}

public class SmtpSettings
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}

public class BrapiSettings
{
    public string Token { get; set; } = "";
}

public class MonitorSettings
{
    public int IntervalSeconds { get; set; } = 60;
    public decimal HysteresisPercent { get; set; } = 0.5m;
}