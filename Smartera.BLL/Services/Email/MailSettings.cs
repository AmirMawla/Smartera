namespace Smartera.BLL.Services.Email
{
    public class MailSettings
    {
        public string Mail { get; set; } = string.Empty;
        public string DisplayName { get; set; } = "SMARTERA Electronics";
        public string Password { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
    }
}
