using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using GWK = Google.Protobuf.WellKnownTypes;

namespace Qrs.App.PasswordManager.Client.Models
{

    public class SearchedList
    {
        private static GWK.Timestamp MinTimestamp = GWK.Timestamp.FromDateTime(DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc));
        
        public Dictionary<string, ResourceItem> SourceItems;
        public List<ResourceInfo> SourceAsResourceInfos => SourceItems.Values.Select(x => x.ResourceInfo).ToList();

        public SearchCriteria Criteria;
        public List<SearchedItem> Items; // results - items to display - filtered and sorted 

        private Comparison<SearchedItem> CurrentSort; // current sort comparison delegate
        

        public SearchedList()
        {
            SourceItems = new Dictionary<string, ResourceItem>();
            Criteria = new SearchCriteria();
            Items = new List<SearchedItem>();
            CurrentSort = SortByCreateDateDesc;
        }

        public static int SortByResourceNameAsc(SearchedItem a, SearchedItem b)
        {
            var aa = a.ResourceItem?.ResourceInfo?.ResourceData?.ResourceName ?? String.Empty;
            var bb = b.ResourceItem?.ResourceInfo?.ResourceData?.ResourceName ?? String.Empty;
            var rr = aa.CompareTo(bb);
            if (rr != 0)
                return rr;
            else
            {
                var aaa = a.ResourceItem?.ResourceInfo?.ResourceData?.Username ?? string.Empty;
                var bbb = b.ResourceItem?.ResourceInfo?.ResourceData?.Username ?? string.Empty;
                return aaa.CompareTo(bbb);
            }
        }

        public static int SortByResourceNameDesc(SearchedItem a, SearchedItem b)
        {
            return -1 * SortByResourceNameAsc(a, b);
        }

        public static int SortByCreateDateAsc(SearchedItem a, SearchedItem b)
        {
            var aa = a.ResourceItem?.ResourceInfo?.ResourceData?.CreateDate ?? DateTime.MinValue;
            var bb = b.ResourceItem?.ResourceInfo?.ResourceData?.CreateDate ?? DateTime.MinValue;
            var rr = aa.CompareTo(bb);
            if (rr != 0)
                return rr;
            else
            {
                // create dates are truncated to minutes, so ties are common - keep the order predictable
                return SortByResourceNameAsc(a, b);
            }
        }

        public static int SortByCreateDateDesc(SearchedItem a, SearchedItem b)
        {
            return -1 * SortByCreateDateAsc(a, b);
        }

        public static int SortByResourceTypeAsc(SearchedItem a, SearchedItem b)
        {
            var aSender = a.ResourceItem?.ResourceInfo?.ResourceData?.ResourceType ?? String.Empty;
            var bSender = b.ResourceItem?.ResourceInfo?.ResourceData?.ResourceType ?? String.Empty;
            if (aSender != bSender)
                return aSender.CompareTo(bSender);
            else
            {
                var aa = a.ResourceItem?.ResourceInfo?.ResourceData?.ResourceName ?? string.Empty;
                var bb = b.ResourceItem?.ResourceInfo?.ResourceData?.ResourceName ?? string.Empty;
                return aa.CompareTo(bb);
            }
        }

        public static int SortByResourceTypeDesc(SearchedItem a, SearchedItem b)
        {
            return -1 * SortByResourceTypeAsc(a, b);
        }

        public void SearchAndSort()
        {
            Search();
            Sort();
        }


        public void Search()
        {
            var searchRegex = new Regex(Criteria.Keywords ?? String.Empty, RegexOptions.IgnoreCase);
            Items.Clear();

            foreach (var mi in SourceItems)
            {
                var rd = mi.Value.ResourceInfo?.ResourceData;
                if (rd == null) continue;

                var sri = new SearchedItem();
                sri.ResourceItem = mi.Value;
                sri.UsernameMatches = searchRegex.Matches(rd.Username);
                sri.ResourceNotesMatches = searchRegex.Matches(rd.ResourceNotes);
                sri.ResourceNameMatches = searchRegex.Matches(rd.ResourceName);
                //sri.ResourceTypeMatches = searchRegex.Matches(rd.ResourceType);

                if (sri.IsMatch)
                {
                    mi.Value.SearchResultItem = sri;
                    Items.Add(sri);
                }
                else
                    mi.Value.SearchResultItem = null;
            }

        }


        public void Sort()
        {
            switch (Criteria.SortBy)
            {
                case SortBy.CreatedAsc: { CurrentSort = SortByCreateDateAsc; break; }
                case SortBy.CreatedDesc: { CurrentSort = SortByCreateDateDesc; break; }
                case SortBy.NameAsc: { CurrentSort = SortByResourceNameAsc; break; }
                case SortBy.NameDesc: { CurrentSort = SortByResourceNameDesc; break; }
                case SortBy.TypeAsc: { CurrentSort = SortByResourceTypeAsc; break; }
                case SortBy.TypeDesc: { CurrentSort = SortByResourceTypeDesc; break; }
                default: { throw new ApplicationException($"Unsupported value in Criteria.SortBy={Criteria.SortBy}"); }
            }
            Items.Sort(CurrentSort);
        }

        public int AddOrdered(SearchedItem si)
        {
            // TODO - populate Matches in SearchedItem b/c they will be used to HIGHLIGHT matches on UI in the future
            // also, what if newly added item is NOT matching current filter's Criteria? Right now, we are adding it anyways
            // once we populate Matches, we could also check IsMatch to decided do we need to add to Items at all
            var idx = Items.FindIndex(x => CurrentSort(si, x) <= 0);
            if (idx >= 0)
            {
                Items.Insert(idx, si);
                return idx;
            }
            else
            {
                Items.Add(si);
                return Items.Count - 1;
            }
        }

        public int AddResourceInfo(ResourceInfo rin, bool ordered = false)
        {
            var rit = new ResourceItem();
            rit.ResourceInfo = rin;
            
            return AddResourceItem(rit);
        }

        public bool ExistsId(string id)
        {
            return SourceItems.ContainsKey(id);
        }

        public SearchedItem NextSearchedItem(string id)
        {
            var x = Items.FindIndex(si => si.ResourceItem?.ResourceInfo?.Id == id);
            if ( x <0 ) return null;
            x++;
            if (x >= Items.Count) return null;
            return Items[x];
        }

        public SearchedItem NextOrPreviousSearchedItem(string id)
        {
            var x = Items.FindIndex(si => si.ResourceItem?.ResourceInfo?.Id == id);
            if (x < 0) return null;
            // move to next
            x++;
            if (x >= Items.Count)
            {
                // move to previous (2 items back from current "next" item)
                x -= 2;
                if (x < 0) return null;
            }
            return Items[x];
        }


        public int AddResourceItem(ResourceItem rit, bool ordered = false)
        {
            if (ExistsId(rit.ResourceInfo.Id)) { return -1; }

            var si = new SearchedItem();
            si.ResourceItem = rit;

            SourceItems.Add(rit.ResourceInfo.Id, si.ResourceItem);
            if (ordered)
            {
                return AddOrdered(si);
            }
            else
            {
                Items.Add(si);
                return Items.Count - 1;
            }
        }

        public int SaveResourceItem(ResourceItem rit)
        {
            // 1) SOURCE ITEMS - add if not already there
            if (!ExistsId(rit.ResourceInfo.Id)) 
            { 
                SourceItems.Add(rit.ResourceInfo.Id, rit);
            }

            // 2) ITEMS - Add it WITHOUT applying filtering (Search), but do apply current Sort Order

            // search SI for this Id
            var index = Items.FindIndex(si => si.ResourceItem?.ResourceInfo?.Id == rit.ResourceInfo.Id);
            if (index >= 0)
            {
                var si = Items[index];
                Items.RemoveAt(index);
                return AddOrdered(si);
            }
            else
            {
                var si = new SearchedItem();
                si.ResourceItem = rit;
                return AddOrdered(si); 
            }
        }


        public void Delete(string id)
        { 
            if (!SourceItems.ContainsKey(id)) 
                return;

            Items.RemoveAll(si => si.ResourceItem?.ResourceInfo?.Id == id);
            SourceItems.Remove(id);
        }

    }
}
