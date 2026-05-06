using System.Text.RegularExpressions;
using Serilog;

namespace regvalidation
{
    internal class Program
    {
        private static readonly List<string> UnavailableLogins = new List<string> { "admin", "moderator", "user123", "root" };

        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("registration_log.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            // Пример
            string login = "MyLogin_123";
            string password = "Пароль1!";
            string confirmPassword = "Пароль1!";

            var (isSuccess, message) = RegisterUser(login, password, confirmPassword);

            Console.WriteLine($"Результат: {isSuccess}");
            Console.WriteLine($"Сообщение: {message}");

            Log.CloseAndFlush();
        }

        public static (bool Result, string Message) RegisterUser(string login, string password, string confirm)
        {
            string maskedPassword = MaskValue(password);
            string maskedConfirm = MaskValue(confirm);
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            try
            {
                if (password != confirm)
                    throw new Exception("Пароль и подтверждение пароля не совпадают.");

                ValidateLogin(login);

                // 3. Проверка на существующий логин
                if (UnavailableLogins.Contains(login.ToLower()))
                    throw new Exception("Данный логин недоступен.");

                // 4. Валидация пароля
                ValidatePassword(password);

                // Успех
                Log.Information("{Timestamp} | Login: {Login} | Pass: {P} | Conf: {C} | Успешная регистрация",
                    timestamp, login, maskedPassword, maskedConfirm);

                return (true, "");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{Timestamp} | Login: {Login} | Pass: {P} | Conf: {C} | Ошибка: {Error}",
                    timestamp, login, maskedPassword, maskedConfirm, ex.Message);

                return (false, ex.Message);
            }
        }

        private static void ValidateLogin(string login)
        {
            if (string.IsNullOrWhiteSpace(login)) throw new Exception("Логин не может быть пустым.");

            // Формат телефона +x-xxx-xxx-xxxx
            bool isPhone = Regex.IsMatch(login, @"^\+\d-\d{3}-\d{3}-\d{4}$");
            // Формат почты [текст без пробелов и @]@[текст без пробелов].[текст без пробелов]
            bool isEmail = Regex.IsMatch(login, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            // Формат обычной строки (мин 5, латиница, цифры, _)
            bool isSimple = Regex.IsMatch(login, @"^[a-zA-Z0-9_]{5,}$");

            if (!isPhone && !isEmail && !isSimple)
            {
                if (login.Contains("@")) throw new Exception("Неверный формат электронной почты.");
                if (login.StartsWith("+")) throw new Exception("Неверный формат телефона (ожидается +x-xxx-xxx-xxxx).");
                if (login.Length < 5) throw new Exception("Логин-строка должен содержать минимум 5 символов.");
                throw new Exception("Логин содержит недопустимые символы (разрешена только латиница, цифры и '_').");
            }
        }

        private static void ValidatePassword(string pass)
        {
            if (pass.Length < 7) throw new Exception("Пароль слишком короткий (минимум 7 символов).");
            if (!Regex.IsMatch(pass, @"[а-яё]")) throw new Exception("Пароль должен содержать строчную кириллицу.");
            if (!Regex.IsMatch(pass, @"[А-ЯЁ]")) throw new Exception("Пароль должен содержать заглавную кириллицу.");
            if (!Regex.IsMatch(pass, @"\d")) throw new Exception("Пароль должен содержать хотя бы одну цифру.");
            if (!Regex.IsMatch(pass, @"[^\w\s]")) throw new Exception("Пароль должен содержать хотя бы один спецсимвол.");
            if (Regex.IsMatch(pass, @"[a-zA-Z]")) throw new Exception("В пароле запрещена латиница (только кириллица).");
        }

        // Маскирование: заменяет символы на *
        private static string MaskValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return new string('*', value.Length);
        }
    }
}
