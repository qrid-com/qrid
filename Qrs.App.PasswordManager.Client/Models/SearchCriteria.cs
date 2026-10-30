using System;
using System.Collections.Generic;

namespace Qrs.App.PasswordManager.Client.Models
{
    public enum SortBy
    {
        CreatedAsc,
        CreatedDesc,
        NameAsc,
        NameDesc,
        TypeAsc,
        TypeDesc
    }
    public class SearchCriteria
    {

        public string Keywords { get; set; } = String.Empty;
        public List<string> Tags = new List<string>();

        public SortBy SortBy = SortBy.CreatedDesc;

    }
}
