namespace SchoolManager.Identity.Application
{
    internal static class MenuItemRules
    {
        /// <summary>Empty (group without a page) or an app path like "/users/employees".</summary>
        public static bool IsValidRoute(string? route)
        {
            if (string.IsNullOrWhiteSpace(route))
                return true;

            var r = route.Trim();
            return r.StartsWith('/') && !r.StartsWith("//") && !r.Contains(' ') && !r.Contains("://");
        }
    }
}
