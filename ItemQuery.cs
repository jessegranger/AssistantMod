using System.Collections.Generic;
using System.Linq;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements.InventoryElements;
using static Assistant.Globals;

namespace Assistant
{
    public class ItemQuery
    {
        private class ItemModQuery
        {
            public string ModName;
            public bool AcceptAnyValue = true;
            public int AcceptMinValue = int.MinValue;
            public int AcceptMaxValue = int.MaxValue;
            public bool Matches(NormalInventoryItem item) => CountMatches(item) > 0;
            public int CountMatches(NormalInventoryItem item)
            {
                int count = 0;
                var mods = item.Item.GetComponent<Mods>();
                if (mods == null) return 0;
                string prefix = ModName;
                bool matchPrefix = false;
                if( ModName.EndsWith("*") )
                {
                    matchPrefix = true;
                    prefix = ModName.Substring(0, ModName.Length - 1);
                }

                foreach(var mod in mods.ItemMods)
                {
                    if( (matchPrefix && mod.Name.StartsWith(prefix)) || mod.Name.Equals(ModName) )
                    {
                        if( AcceptAnyValue || (mod.Value1 >= AcceptMinValue && mod.Value1 <= AcceptMaxValue))
                        {
                            count += 1;
                        }
                    }
                }
                return count;
            }
            public ItemModQuery(string modName) => ModName = modName;
        }
        private abstract class ItemQueryGroup
        {
            public abstract bool Matches(NormalInventoryItem item);
            public virtual int CountMatches(NormalInventoryItem item) => Matches(item) ? 1 : 0;
        }
        private class ItemQueryGroup_AllMatch : ItemQueryGroup
        {
            private List<ItemModQuery> matches;
            public ItemQueryGroup_AllMatch(params ItemModQuery[] query) => matches = new List<ItemModQuery>(query);
            public override bool Matches(NormalInventoryItem item) => matches.All((query) => query.Matches(item));
            public override int CountMatches(NormalInventoryItem item)
            {
                return matches.Select((query) => query.CountMatches(item)).Sum();
            }
        
        }
        private class ItemQueryGroup_CountMatch : ItemQueryGroup
        {

            public ItemQueryGroup_CountMatch(int count, ItemModQuery[] q)
            {
                matchCount = count;
                matches = new List<ItemModQuery>(q);
            }

            private int matchCount;
            private List<ItemModQuery> matches;

            public override bool Matches(NormalInventoryItem item) => matches.Where((query) => query.Matches(item)).Count() >= matchCount;
        }
        private class ItemQueryGroup_NotMatch : ItemQueryGroup
        {
            private List<ItemModQuery> matches = new List<ItemModQuery>();
            public override bool Matches(NormalInventoryItem item) => matches.All((query) => !query.Matches(item));
        }

        private List<ItemQueryGroup> groups = new List<ItemQueryGroup>();
        public bool Matches(NormalInventoryItem item)
        {
            return groups.All((group) => group.Matches(item));
        }
        public int CountMatches(NormalInventoryItem item)
        {
            return groups.Select((group) => group.CountMatches(item)).Sum();
        }
        private void MatchAll(params ItemModQuery[] query)
        {
            groups.Add(new ItemQueryGroup_AllMatch(query));
        }
        public void MatchAll(params string[] query)
        {
            ItemModQuery[] q = new ItemModQuery[query.Length];
            for (int i = 0; i < q.Length; i++)
            {
                q[i] = new ItemModQuery(query[i]);
            }
            MatchAll(q);
        }
        public void MatchCount(int count, params string[] query)
        {
            ItemModQuery[] q = new ItemModQuery[query.Length];
            for (int i = 0; i < q.Length; i++)
            {
                q[i] = new ItemModQuery(query[i]);
            }
            groups.Add(new ItemQueryGroup_CountMatch(count, q));
        }
    }
}
