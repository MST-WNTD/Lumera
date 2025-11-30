namespace Lumera.Models.AdminViewModels
{
    public class UserManagementViewModel
    {
        public List<AdminUserViewModel> Users { get; set; } = new List<AdminUserViewModel>();
        public PaginationInfo PaginationInfo { get; set; } = new PaginationInfo();
    }

    public class PaginationInfo
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int StartIndex { get; set; }
        public int EndIndex { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }
}