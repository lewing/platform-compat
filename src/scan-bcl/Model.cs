using System;
using System.Collections.Generic;
using CsvHelper;

namespace scan_bcl
{
    internal sealed class DatabaseEntry
    {
        public DatabaseEntry(string DocId, string Namespace, string Type, string Member, int Nesting)
        {
            this.DocId = DocId;
            this.Namespace = Namespace;
            this.Type = Type;
            this.Member = Member;
            this.Nesting = Nesting;
        }

        public string DocId { get; set; }  
        public string Namespace { get; set; } 
        public string Type { get; set; } 
        public string Member { get; set; } 
        public int Nesting { get; set; } 
    }

    // public sealed class DatabaseMap : ClassMap<DatabaseEntry>
    // {
    //     public DatabaseMap()
    //     {
    //         Map(m => m.DocId);
    //         Map(m => m.Namespace);
    //         Map(m => m.Type);
    //         Map(m => m.Member);
    //         Map(m => m.Nesting);
    //     }
    // }

    internal sealed class Database 
    {
        private readonly Dictionary<string, DatabaseEntry> _entries = new Dictionary<string, DatabaseEntry>();
        
        public void Add(string DocId, string Namespace, string Type, string Member, int Nesting)
        {
            var entry = new DatabaseEntry(DocId, Namespace, Type, Member, Nesting);
            _entries.Add(DocId, entry);
        }

        public void Remove(string DocId)
        {
            _entries.Remove(DocId);
        }

        public IEnumerable<DatabaseEntry> Entries => _entries.Values;
    }
        
    
}