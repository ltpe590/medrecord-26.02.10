namespace WPF.Configuration
{
    public static class UserSettingsManager
    {
        public static string GetDefaultUsername()
        {
            try
            {
                return UserSettings.Default.DefaultUsername;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting username: {ex.Message}");
                return "test"; // fallback
            }
        }

        public static string GetDefaultPassword()
        {
            try
            {
                return UserSettings.Default.DefaultPassword;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting password: {ex.Message}");
                return "Test@123"; // fallback
            }
        }

        public static void SaveCredentials(string username, string password)
        {
            try
            {
                UserSettings.Default.DefaultUsername = username;
                UserSettings.Default.DefaultPassword = password;
                UserSettings.Default.SettingsSaved = true;
                UserSettings.Default.Save();

                Console.WriteLine($"Credentials saved: {username}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        public static bool HasSavedSettings()
        {
            try
            {
                return UserSettings.Default.SettingsSaved;
            }
            catch
            {
                return false;
            }
        }

        public static void ResetSettings()
        {
            try
            {
                UserSettings.Default.Reset();
                UserSettings.Default.Save();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error resetting settings: {ex.Message}");
            }
        }
    }
}