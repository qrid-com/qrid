using System.Text.RegularExpressions;

namespace Qrs.App.PasswordManager.Client.Models
{

    public class SearchedItem
    {
        public ResourceItem ResourceItem;

        //public MatchCollection ResourceTypeMatches;
        public MatchCollection ResourceNameMatches;
        public MatchCollection UsernameMatches;
        public MatchCollection ResourceNotesMatches;
        
        public bool IsMatch => ResourceNameMatches?.Count > 0
            //|| ResourceTypeMatches?.Count > 0
            || UsernameMatches?.Count > 0
            || ResourceNotesMatches?.Count > 0
            ;


    }

}
